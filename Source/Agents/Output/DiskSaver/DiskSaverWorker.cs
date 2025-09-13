﻿/******************************************************************************/
/* This source, or parts thereof, may be used in any software as long the     */
/* license of NostalgicPlayer is keep. See the LICENSE file for more          */
/* information.                                                               */
/******************************************************************************/
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Polycode.NostalgicPlayer.Agent.Output.DiskSaver.Settings;
using Polycode.NostalgicPlayer.Kit.Bases;
using Polycode.NostalgicPlayer.Kit.Containers;
using Polycode.NostalgicPlayer.Kit.Containers.Flags;
using Polycode.NostalgicPlayer.Kit.Gui.Controls;
using Polycode.NostalgicPlayer.Kit.Helpers;
using Polycode.NostalgicPlayer.Kit.Interfaces;
using Polycode.NostalgicPlayer.Kit.Streams;

namespace Polycode.NostalgicPlayer.Agent.Output.DiskSaver
{
	/// <summary>
	/// Main worker class
	/// </summary>
	internal class DiskSaverWorker : OutputAgentBase, IAgentSettingsRegistrar
	{
		private const int MixerBufferSize = 65536;

		private readonly AgentInfo[] outputAgents;
		private readonly AgentInfo[] sampleConverterAgents;

		private DiskSaverSettings settings;

		private ISampleSaverAgent converterInUse;
		private IOutputAgent outputAgentInUse;

		private Lock streamLock;
		private SoundStream stream;
		private Stream fileStream;

		private byte[] mixerBuffer;
		private int mixerBufferSizeInFrames;
		private int[] converterBuffer;

		private ManualResetEvent shutdownEvent;
		private ManualResetEvent pauseEvent;
		private Thread saverThread;

		private bool sampleDelay;

		/********************************************************************/
		/// <summary>
		/// Constructor
		/// </summary>
		/********************************************************************/
		public DiskSaverWorker(AgentInfo[] outputAgents, AgentInfo[] sampleConverterAgents)
		{
			this.outputAgents = outputAgents;
			this.sampleConverterAgents = sampleConverterAgents;
		}

		#region IOutputAgent implementation
		/********************************************************************/
		/// <summary>
		/// Return some flags telling what the output agent supports
		/// </summary>
		/********************************************************************/
		public override OutputSupportFlag SupportFlags => OutputSupportFlag.FlushMe;



		/********************************************************************/
		/// <summary>
		/// Will initialize the output driver
		/// </summary>
		/********************************************************************/
		public override AgentResult Initialize(out string errorMessage)
		{
			errorMessage = string.Empty;

			settings = new DiskSaverSettings();

			// Is there selected any converters
			if (settings.OutputFormat == Guid.Empty)
			{
				errorMessage = Resources.IDS_ERR_NO_SELECTED_CONVERTER;
				return AgentResult.Error;
			}

			// Try to find the converter
			AgentInfo agentInfo = sampleConverterAgents.FirstOrDefault(a => (a.TypeId == settings.OutputFormat) && a.Enabled);
			if (agentInfo == null)
			{
				errorMessage = Resources.IDS_ERR_NO_CONVERTER;
				return AgentResult.Error;
			}

			converterInUse = (ISampleSaverAgent)agentInfo.Agent.CreateInstance(agentInfo.TypeId);

			if (!Directory.Exists(settings.DiskPath))
			{
				errorMessage = string.Format(Resources.IDS_ERR_DIRECTORY_NOT_FOUND, settings.DiskPath);
				return AgentResult.Error;
			}

			// Initialize stream
			stream = null;
			streamLock = new Lock();

			if (settings.OutputAgent == Guid.Empty)
			{
				outputAgentInUse = null;

				// Allocate buffer to use
				int channels = settings.OutputType == DiskSaverSettings.OutType.Stereo ? 2 : 1;

				mixerBuffer = new byte[MixerBufferSize];
				mixerBufferSizeInFrames = MixerBufferSize / channels / settings.OutputSize;

				// Create needed events
				shutdownEvent = new ManualResetEvent(false);
				pauseEvent = new ManualResetEvent(false);

				// Create thread to do the saving
				saverThread = new Thread(DoSaverThread);
				saverThread.Name = "Disk saver";
				saverThread.Start();
			}
			else
			{
				// Pass-through another output agent
				agentInfo = outputAgents.FirstOrDefault(a => (a.TypeId == settings.OutputAgent) && a.Enabled);
				if (agentInfo == null)
				{
					errorMessage = Resources.IDS_ERR_NO_OUTPUT;
					return AgentResult.Error;
				}

				outputAgentInUse = (IOutputAgent)agentInfo.Agent.CreateInstance(agentInfo.TypeId);

				if (outputAgentInUse.Initialize(out errorMessage) == AgentResult.Error)
					return AgentResult.Error;
			}

			return AgentResult.Ok;
		}



		/********************************************************************/
		/// <summary>
		/// Will shutdown the output driver
		/// </summary>
		/********************************************************************/
		public override void Shutdown()
		{
			if (outputAgentInUse == null)
			{
				// Tell the saver thread to exit
				shutdownEvent?.Set();

				// Wait the thread to exit
				saverThread?.Join();

				// Clear all variables
				saverThread = null;

				pauseEvent?.Dispose();
				pauseEvent = null;

				shutdownEvent?.Dispose();
				shutdownEvent = null;
			}
			else
			{
				outputAgentInUse.Shutdown();
				outputAgentInUse = null;

				stream?.Dispose();
			}

			converterBuffer = null;
			mixerBuffer = null;
			settings = null;
			stream = null;

			converterInUse = null;
		}



		/********************************************************************/
		/// <summary>
		/// Tell the engine to begin playing
		/// </summary>
		/********************************************************************/
		public override void Play()
		{
			if (outputAgentInUse == null)
				pauseEvent.Set();
			else
				outputAgentInUse.Play();
		}



		/********************************************************************/
		/// <summary>
		/// Tell the engine to stop playing
		/// </summary>
		/********************************************************************/
		public override void Stop()
		{
			lock (streamLock)
			{
				// Stop the audio
				if (outputAgentInUse == null)
					pauseEvent.Reset();
				else
					outputAgentInUse.Stop();

				// Cleanup the saver
				if (converterInUse != null)
				{
					if (fileStream != null)
						converterInUse.SaveTail(fileStream);

					converterInUse.CleanupSaver();
				}

				// Close the file again
				fileStream?.Dispose();
				fileStream = null;
			}
		}



		/********************************************************************/
		/// <summary>
		/// Tell the engine to pause playing
		/// </summary>
		/********************************************************************/
		public override void Pause()
		{
			lock (streamLock)
			{
				if (outputAgentInUse == null)
					pauseEvent.Reset();
				else
					outputAgentInUse.Pause();
			}
		}



		/********************************************************************/
		/// <summary>
		/// Will set the master volume (0-256)
		/// </summary>
		/********************************************************************/
		public override void SetMasterVolume(int volume)
		{
			// We want to use full volume when saving samples, so don't do anything
		}



		/********************************************************************/
		/// <summary>
		/// Will switch the stream to read the sound data from without
		/// interrupting the sound
		/// </summary>
		/********************************************************************/
		public override AgentResult SwitchStream(SoundStream soundStream, string fileName, string title, string author, out string errorMessage)
		{
			lock (streamLock)
			{
				// Close old file
				if (fileStream != null)
				{
					converterInUse.SaveTail(fileStream);
					fileStream.Dispose();
				}

				stream?.Dispose();

				// Build file name and check if it already exists
				if (ArchivePath.IsArchivePath(fileName))
					fileName = ArchivePath.GetEntryName(fileName);

				fileName = Path.Combine(settings.DiskPath, Path.ChangeExtension(Path.GetFileName(fileName), converterInUse.FileExtension));
				if (File.Exists(fileName))
				{
					using (CustomMessageBox dialog = new CustomMessageBox(string.Format(Resources.IDS_MSG_OVERWRITE_FILE, fileName), Resources.IDS_NAME, CustomMessageBox.IconType.Question))
					{
						dialog.AddButton(Resources.IDS_BUT_YES, 'y');
						dialog.AddButton(Resources.IDS_BUT_NO, 'N');
						dialog.ShowDialog();

						if (dialog.GetButtonResult('N') == 'N')
						{
							errorMessage = string.Empty;
							return AgentResult.Error;
						}
					}
				}

				SaveSampleFormatInfo formatInfo;

				if (outputAgentInUse == null)
				{
					int channels = settings.OutputType == DiskSaverSettings.OutType.Stereo ? 2 : 1;
					SpeakerFlag speakers = channels == 1 ? SpeakerFlag.FrontCenter : SpeakerFlag.FrontLeft | SpeakerFlag.FrontRight;
					soundStream.SetOutputFormat(new OutputInfo(channels, settings.OutputFrequency, mixerBufferSizeInFrames, speakers));

					formatInfo = new SaveSampleFormatInfo((byte)settings.OutputSize, channels, (uint)settings.OutputFrequency, 0, 0, title, author);
				}
				else
				{
					SaverStream saverStream = new SaverStream(this, soundStream);
					soundStream = saverStream;

					if (outputAgentInUse.SwitchStream(soundStream, fileName, title, author, out errorMessage) == AgentResult.Error)
						return AgentResult.Error;

					int outputChannels = saverStream.OutputInfo.Channels >= 2 ? 2 : 1;
					formatInfo = new SaveSampleFormatInfo(OutputInfo.BytesPerSample * 8, outputChannels, (uint)saverStream.OutputInfo.Frequency, 0, 0, title, author);
				}

				stream = soundStream;
				sampleDelay = false;

				// Initialize the saver
				if (!converterInUse.InitSaver(formatInfo, out errorMessage))
					return AgentResult.Error;

				// Open file to store the sound
				fileStream = new FileStream(fileName, FileMode.Create);

				// Write the header
				converterInUse.SaveHeader(fileStream);
			}

			return AgentResult.Ok;
		}
		#endregion

		#region IAgentSettingsRegistrar implementation
		/********************************************************************/
		/// <summary>
		/// Return the agent ID for the agent showing the settings
		/// </summary>
		/********************************************************************/
		public Guid GetSettingsAgentId()
		{
			return new Guid(Assembly.GetAssembly(GetType()).GetCustomAttribute<GuidAttribute>().Value);
		}
		#endregion

		/********************************************************************/
		/// <summary>
		/// Save the given buffer to disk
		/// </summary>
		/********************************************************************/
		public void SaveSampleBuffer(byte[] buffer, int lengthInFrames, int bitsPerSample)
		{
			if (lengthInFrames > 0)
			{
				int channels = settings.OutputType == DiskSaverSettings.OutType.Stereo ? 2 : 1;
				int numberOfSamples = lengthInFrames * channels;
				int numberOfBytes = numberOfSamples * (bitsPerSample / 8);

				if ((converterBuffer == null) || (converterBuffer.Length < numberOfSamples))
				{
					// Allocate converter buffer
					converterBuffer = new int[numberOfSamples];
				}

				if (bitsPerSample == 32)
				{
					// Convert 32-bit byte array to an int array
					for (int i = 0, j = 0; i < numberOfBytes; i += 4, j++)
					{
						int sample = BitConverter.ToInt32(buffer, i);
						converterBuffer[j] = sample;
					}
				}
				else if (bitsPerSample == 16)
				{
					// Convert 16-bit byte array to an int array
					for (int i = 0, j = 0; i < numberOfBytes; i += 2, j++)
					{
						int sample = BitConverter.ToInt16(buffer, i) << 16;
						converterBuffer[j] = sample;
					}
				}

				converterInUse.SaveData(fileStream, converterBuffer, numberOfSamples);
			}
		}

		#region Private methods
		/********************************************************************/
		/// <summary>
		/// Background thread the save the sound to disk
		/// </summary>
		/********************************************************************/
		private void DoSaverThread()
		{
			try
			{
				bool stillPlaying = true;
				WaitHandle[] waitArray = { shutdownEvent, pauseEvent };

				while (stillPlaying)
				{
					int waitResult = WaitHandle.WaitAny(waitArray);
					switch (waitResult)
					{
						// ShutdownEvent
						case 0:
						{
							stillPlaying = false;
							break;
						}

						// PauseEvent (is set when playing, false when paused)
						case 1:
						{
							FillBuffer();
							break;
						}
					}
				}
			}
			catch (Exception ex)
			{
				// If an exception is thrown, abort the thread
				OnPlayerFailed(ex.Message);
			}
		}



		/********************************************************************/
		/// <summary>
		/// Fill the buffer with the next bunch of samples
		/// </summary>
		/********************************************************************/
		private void FillBuffer()
		{
			lock (streamLock)
			{
				if (stream != null)
				{
					// Differences between when playing modules and samples when end-detection is triggered.
					//
					// When playing a module, the HasEndReached is set to true and the client has been notified
					// immediately. So in this case, we could just stop saving.
					//
					// But when playing samplings, an extra buffer if filled with zeros, so when playing the
					// sample through real audio output, it will make sure that the last part is actually played
					// before stopping. To take care of this, we delay the stopping after one more read, so we're
					// sure that the client has been notified before stopping. Notice that we will not save this
					// extra buffer.
					//
					// Since we do not know if we're playing a module or sampling, we always do the logic described
					// above, but it does not matter if we do it for modules too, since nothing extra is saved
					int framesRead = stream.Read(mixerBuffer, 0, mixerBufferSizeInFrames);

					if (!sampleDelay)
						SaveSampleBuffer(mixerBuffer, framesRead, 32);
					else
					{
						// Pause the playing
						pauseEvent.Reset();
					}

					if (stream.HasEndReached)
						sampleDelay = true;
				}
			}
		}
		#endregion
	}
}
