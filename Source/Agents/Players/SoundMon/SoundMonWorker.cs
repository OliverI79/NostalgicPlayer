﻿/******************************************************************************/
/* This source, or parts thereof, may be used in any software as long the     */
/* license of NostalgicPlayer is keep. See the LICENSE file for more          */
/* information.                                                               */
/******************************************************************************/
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Polycode.NostalgicPlayer.Agent.Player.SoundMon.Containers;
using Polycode.NostalgicPlayer.Kit;
using Polycode.NostalgicPlayer.Kit.Bases;
using Polycode.NostalgicPlayer.Kit.Containers;
using Polycode.NostalgicPlayer.Kit.Interfaces;
using Polycode.NostalgicPlayer.Kit.Streams;
using Polycode.NostalgicPlayer.Kit.Utility;

namespace Polycode.NostalgicPlayer.Agent.Player.SoundMon
{
	/// <summary>
	/// Main worker class
	/// </summary>
	internal class SoundMonWorker : ModulePlayerWithPositionDurationAgentBase
	{
		private static readonly Dictionary<Guid, ModuleType> moduleTypeLookup = new Dictionary<Guid, ModuleType>
		{
			{ SoundMon.Agent1Id, ModuleType.SoundMon11 },
			{ SoundMon.Agent2Id, ModuleType.SoundMon22 }
		};

		private readonly ModuleType currentModuleType;

		private string moduleName;
		private byte waveNum;
		private ushort stepNum;
		private ushort trackNum;

		private Instrument[] instruments;
		private Step[][] steps;
		private Track[][] tracks;
		private sbyte[] waveTables;

		private GlobalPlayingInfo playingInfo;
		private BpCurrent[] bpCurrent;

		private bool endReached;

		private const int InfoPositionLine = 4;
		private const int InfoTrackLine = 5;
		private const int InfoSpeedLine = 6;

		/********************************************************************/
		/// <summary>
		/// Constructor
		/// </summary>
		/********************************************************************/
		public SoundMonWorker(Guid typeId)
		{
			if (!moduleTypeLookup.TryGetValue(typeId, out currentModuleType))
				currentModuleType = ModuleType.Unknown;
		}

		#region IPlayerAgent implementation
		/********************************************************************/
		/// <summary>
		/// Returns the file extensions that identify this player
		/// </summary>
		/********************************************************************/
		public override string[] FileExtensions => [ "bp", "bp2", "bp3" ];



		/********************************************************************/
		/// <summary>
		/// Test the file to see if it could be identified
		/// </summary>
		/********************************************************************/
		public override AgentResult Identify(PlayerFileInfo fileInfo)
		{
			// Check the module
			ModuleType checkType = TestModule(fileInfo);
			if (checkType == currentModuleType)
				return AgentResult.Ok;

			return AgentResult.Unknown;
		}



		/********************************************************************/
		/// <summary>
		/// Return the name of the module
		/// </summary>
		/********************************************************************/
		public override string ModuleName => moduleName;



		/********************************************************************/
		/// <summary>
		/// Returns the description and value on the line given. If the line
		/// is out of range, false is returned
		/// </summary>
		/********************************************************************/
		public override bool GetInformationString(int line, out string description, out string value)
		{
			// Find out which line to take
			switch (line)
			{
				// Number of positions
				case 0:
				{
					description = Resources.IDS_BP_INFODESCLINE0;
					value = stepNum.ToString();
					break;
				}

				// Used tracks
				case 1:
				{
					description = Resources.IDS_BP_INFODESCLINE1;
					value = trackNum.ToString();
					break;
				}

				// Supported / used samples
				case 2:
				{
					description = Resources.IDS_BP_INFODESCLINE2;
					value = "15";
					break;
				}

				// Used wave tables
				case 3:
				{
					description = Resources.IDS_BP_INFODESCLINE3;
					value = waveNum.ToString();
					break;
				}

				// Playing position
				case 4:
				{
					description = Resources.IDS_BP_INFODESCLINE4;
					value = playingInfo.BpStep.ToString();
					break;
				}

				// Playing tracks
				case 5:
				{
					description = Resources.IDS_BP_INFODESCLINE5;
					value = FormatTracks();
					break;
				}

				// Current speed
				case 6:
				{
					description = Resources.IDS_BP_INFODESCLINE6;
					value = playingInfo.BpDelay.ToString();
					break;
				}

				default:
				{
					description = null;
					value = null;

					return false;
				}
			}

			return true;
		}
		#endregion

		#region IModulePlayerAgent implementation
		/********************************************************************/
		/// <summary>
		/// Will load the file into memory
		/// </summary>
		/********************************************************************/
		public override AgentResult Load(PlayerFileInfo fileInfo, out string errorMessage)
		{
			errorMessage = string.Empty;

			try
			{
				ModuleStream moduleStream = fileInfo.ModuleStream;
				Encoding encoder = EncoderCollection.Amiga;

				// Start to read the module name
				moduleName = moduleStream.ReadString(encoder, 26);

				// Skip the mark
				moduleStream.Seek(3, SeekOrigin.Current);

				// Get the number of waveforms
				waveNum = moduleStream.Read_UINT8();

				// Get the number of positions
				stepNum = moduleStream.Read_B_UINT16();

				if (moduleStream.EndOfStream)
				{
					errorMessage = Resources.IDS_BP_ERR_LOADING_HEADER;
					Cleanup();

					return AgentResult.Error;
				}

				// Read the sample information
				instruments = new Instrument[15];

				for (int i = 0; i < 15; i++)
				{
					// First find out the instrument kind
					if (moduleStream.Read_UINT8() == 0xff)
					{
						// Synth instrument
						SynthInstrument inst = new SynthInstrument();
						instruments[i] = inst;

						if (currentModuleType == ModuleType.SoundMon11)
						{
							inst.WaveTable = moduleStream.Read_UINT8();
							inst.WaveLength = (ushort)(moduleStream.Read_B_UINT16() * 2);
							inst.AdsrControl = moduleStream.Read_UINT8();
							inst.AdsrTable = moduleStream.Read_UINT8();
							inst.AdsrLength = moduleStream.Read_B_UINT16();
							inst.AdsrSpeed = moduleStream.Read_UINT8();
							inst.LfoControl = moduleStream.Read_UINT8();
							inst.LfoTable = moduleStream.Read_UINT8();
							inst.LfoDepth = moduleStream.Read_UINT8();
							inst.LfoLength = moduleStream.Read_B_UINT16();
							moduleStream.Seek(1, SeekOrigin.Current);
							inst.LfoDelay = moduleStream.Read_UINT8();
							inst.LfoSpeed = moduleStream.Read_UINT8();
							inst.EgControl = moduleStream.Read_UINT8();
							inst.EgTable = moduleStream.Read_UINT8();
							moduleStream.Seek(1, SeekOrigin.Current);
							inst.EgLength = moduleStream.Read_B_UINT16();
							moduleStream.Seek(1, SeekOrigin.Current);
							inst.EgDelay = moduleStream.Read_UINT8();
							inst.EgSpeed = moduleStream.Read_UINT8();
							inst.FxControl = 0;
							inst.FxSpeed = 1;
							inst.FxDelay = 0;
							inst.ModControl = 0;
							inst.ModTable = 0;
							inst.ModSpeed = 1;
							inst.ModDelay = 0;
							inst.Volume = moduleStream.Read_UINT8();
							inst.ModLength = 0;
							moduleStream.Seek(6, SeekOrigin.Current);
						}
						else
						{
							inst.WaveTable = moduleStream.Read_UINT8();
							inst.WaveLength = (ushort)(moduleStream.Read_B_UINT16() * 2);
							inst.AdsrControl = moduleStream.Read_UINT8();
							inst.AdsrTable = moduleStream.Read_UINT8();
							inst.AdsrLength = moduleStream.Read_B_UINT16();
							inst.AdsrSpeed = moduleStream.Read_UINT8();
							inst.LfoControl = moduleStream.Read_UINT8();
							inst.LfoTable = moduleStream.Read_UINT8();
							inst.LfoDepth = moduleStream.Read_UINT8();
							inst.LfoLength = moduleStream.Read_B_UINT16();
							inst.LfoDelay = moduleStream.Read_UINT8();
							inst.LfoSpeed = moduleStream.Read_UINT8();
							inst.EgControl = moduleStream.Read_UINT8();
							inst.EgTable = moduleStream.Read_UINT8();
							inst.EgLength = moduleStream.Read_B_UINT16();
							inst.EgDelay = moduleStream.Read_UINT8();
							inst.EgSpeed = moduleStream.Read_UINT8();
							inst.FxControl = moduleStream.Read_UINT8();
							inst.FxSpeed = moduleStream.Read_UINT8();
							inst.FxDelay = moduleStream.Read_UINT8();
							inst.ModControl = moduleStream.Read_UINT8();
							inst.ModTable = moduleStream.Read_UINT8();
							inst.ModSpeed = moduleStream.Read_UINT8();
							inst.ModDelay = moduleStream.Read_UINT8();
							inst.Volume = moduleStream.Read_UINT8();
							inst.ModLength = moduleStream.Read_B_UINT16();
						}

						if (inst.Volume > 64)
							inst.Volume = 64;
					}
					else
					{
						// Sample instrument
						SampleInstrument inst = new SampleInstrument();
						instruments[i] = inst;

						moduleStream.Seek(-1, SeekOrigin.Current);

						inst.Name = moduleStream.ReadString(encoder, 24).TrimEnd();
						inst.Length = (ushort)(moduleStream.Read_B_UINT16() * 2);
						inst.LoopStart = moduleStream.Read_B_UINT16();
						inst.LoopLength = (ushort)(moduleStream.Read_B_UINT16() * 2);
						inst.Volume = moduleStream.Read_B_UINT16();
						inst.Adr = null;

						if (inst.Volume > 64)
							inst.Volume = 64;

						// Fix for Karate.bp
						if ((inst.LoopStart + inst.LoopLength) > inst.Length)
							inst.LoopLength = (ushort)(inst.Length - inst.LoopStart);
					}

					// Check for "end-of-file"
					if (moduleStream.EndOfStream)
					{
						errorMessage = Resources.IDS_BP_ERR_LOADING_INSTINFO;
						Cleanup();

						return AgentResult.Error;
					}
				}

				// Allocate step structures
				steps = new Step[4][];

				for (int i = 0; i < 4; i++)
					steps[i] = ArrayHelper.InitializeArray<Step>(stepNum);

				// Read the step data
				trackNum = 0;

				for (int i = 0; i < stepNum; i++)
				{
					for (int j = 0; j < 4; j++)
					{
						// Read track
						steps[j][i].TrackNumber = moduleStream.Read_B_UINT16();
						steps[j][i].SoundTranspose = moduleStream.Read_INT8();
						steps[j][i].Transpose = moduleStream.Read_INT8();

						if (steps[j][i].TrackNumber > trackNum)
							trackNum = steps[j][i].TrackNumber;
					}

					if (moduleStream.EndOfStream)
					{
						errorMessage = Resources.IDS_BP_ERR_LOADING_HEADER;
						Cleanup();

						return AgentResult.Error;
					}
				}

				// Allocate space to hold the tracks
				tracks = new Track[trackNum][];

				// Read the tracks
				for (int i = 0; i < trackNum; i++)
				{
					// Allocate one track
					tracks[i] = ArrayHelper.InitializeArray<Track>(16);

					// Read it
					for (int j = 0; j < 16; j++)
					{
						tracks[i][j].Note = moduleStream.Read_INT8();
						tracks[i][j].Instrument = moduleStream.Read_UINT8();
						tracks[i][j].Optional = (Optional)(tracks[i][j].Instrument & 0x0f);
						tracks[i][j].Instrument = (byte)((tracks[i][j].Instrument & 0xf0) >> 4);
						tracks[i][j].OptionalData = moduleStream.Read_UINT8();
					}

					if (moduleStream.EndOfStream)
					{
						errorMessage = Resources.IDS_BP_ERR_LOADING_TRACKS;
						Cleanup();

						return AgentResult.Error;
					}
				}

				// Allocate space to hold the wave tables
				waveTables = new sbyte[waveNum * 64];

				// Read the wave tables
				moduleStream.ReadSigned(waveTables, 0, waveNum * 64);

				if (moduleStream.EndOfStream)
				{
					errorMessage = Resources.IDS_BP_ERR_LOADING_SAMPLES;
					Cleanup();

					return AgentResult.Error;
				}

				// Okay, finally we read the samples
				for (int i = 0; i < 15; i++)
				{
					// Is the instrument a sample?
					if (instruments[i] is SampleInstrument inst)
					{
						// Yes, check the length
						if (inst.Length != 0)
						{
							// Read the sample data
							inst.Adr = moduleStream.ReadSampleData(i, inst.Length, out int _);

							if (moduleStream.EndOfStream)
							{
								errorMessage = Resources.IDS_BP_ERR_LOADING_SAMPLES;
								Cleanup();

								return AgentResult.Error;
							}
						}
					}
				}
			}
			catch (Exception)
			{
				Cleanup();
				throw;
			}

			// Ok, we're done
			return AgentResult.Ok;
		}



		/********************************************************************/
		/// <summary>
		/// Cleanup the player
		/// </summary>
		/********************************************************************/
		public override void CleanupPlayer()
		{
			Cleanup();

			base.CleanupPlayer();
		}



		/********************************************************************/
		/// <summary>
		/// Initializes the current song
		/// </summary>
		/********************************************************************/
		public override bool InitSound(int songNumber, out string errorMessage)
		{
			if (!base.InitSound(songNumber, out errorMessage))
				return false;

			InitializeSound(0);

			return true;
		}



		/********************************************************************/
		/// <summary>
		/// This is the main player method
		/// </summary>
		/********************************************************************/
		public override void Play()
		{
			// Call the player
			BpPlay();

			if (endReached)
			{
				OnEndReached(playingInfo.BpStep);
				endReached = false;

				MarkPositionAsVisited(playingInfo.BpStep);
			}
		}



		/********************************************************************/
		/// <summary>
		/// Returns all the samples available in the module. If none, null
		/// is returned
		/// </summary>
		/********************************************************************/
		public override IEnumerable<SampleInfo> Samples
		{
			get
			{
				// Build frequency table
				uint[] frequencies = new uint[10 * 12];

				for (int j = 0; j < 7 * 12; j++)
					frequencies[1 * 12 + j] = 3546895U / Tables.Periods[j];

				for (int i = 0; i < 15; i++)
				{
					Instrument inst = instruments[i];

					SampleInfo sampleInfo = new SampleInfo
					{
						Flags = SampleInfo.SampleFlag.None,
						Volume = (ushort)(inst.Volume * 4),
						Panning = -1,
						NoteFrequencies = frequencies
					};

					if (inst is SampleInstrument sampleInst)
					{
						sampleInfo.Type = SampleInfo.SampleType.Sample;
						sampleInfo.Name = sampleInst.Name;
						sampleInfo.Sample = sampleInst.Adr;
						sampleInfo.Length = sampleInst.Length > 2 ? sampleInst.Length : 0U;

						if (sampleInst.LoopLength <= 2)
						{
							// No loop
							sampleInfo.LoopStart = 0;
							sampleInfo.LoopLength = 0;
						}
						else
						{
							// Sample loops
							sampleInfo.Flags |= SampleInfo.SampleFlag.Loop;
							sampleInfo.LoopStart = sampleInst.LoopStart;
							sampleInfo.LoopLength = sampleInst.LoopLength;
						}
					}
					else
					{
						sampleInfo.Type = SampleInfo.SampleType.Synthesis;
						sampleInfo.Name = string.Empty;
						sampleInfo.Sample = null;
						sampleInfo.Length = 0;
						sampleInfo.LoopStart = 0;
						sampleInfo.LoopLength = 0;
					}

					yield return sampleInfo;
				}
			}
		}
		#endregion

		#region ModulePlayerWithPositionDurationAgentBase implementation
		/********************************************************************/
		/// <summary>
		/// Initialize all internal structures when beginning duration
		/// calculation on a new sub-song
		/// </summary>
		/********************************************************************/
		protected override int InitDuration(int songNumber, int startPosition)
		{
			InitializeSound(startPosition);
			MarkPositionAsVisited(startPosition);

			return startPosition;
		}



		/********************************************************************/
		/// <summary>
		/// Return the total number of positions
		/// </summary>
		/********************************************************************/
		protected override int GetTotalNumberOfPositions()
		{
			return stepNum;
		}



		/********************************************************************/
		/// <summary>
		/// Create a snapshot of all the internal structures and return it
		/// </summary>
		/********************************************************************/
		protected override ISnapshot CreateSnapshot()
		{
			return new Snapshot(playingInfo, bpCurrent);
		}



		/********************************************************************/
		/// <summary>
		/// Initialize internal structures based on the snapshot given
		/// </summary>
		/********************************************************************/
		protected override bool SetSnapshot(ISnapshot snapshot, out string errorMessage)
		{
			errorMessage = string.Empty;

			// Start to make a clone of the snapshot
			Snapshot currentSnapshot = (Snapshot)snapshot;
			Snapshot clonedSnapshot = new Snapshot(currentSnapshot.PlayingInfo, currentSnapshot.Channels);

			playingInfo = clonedSnapshot.PlayingInfo;
			bpCurrent = clonedSnapshot.Channels;

			UpdateModuleInformation();

			return true;
		}
		#endregion

		#region Private methods
		/********************************************************************/
		/// <summary>
		/// Tests the module to see which type of module it is
		/// </summary>
		/********************************************************************/
		private ModuleType TestModule(PlayerFileInfo fileInfo)
		{
			ModuleStream moduleStream = fileInfo.ModuleStream;

			// Check the module size
			if (moduleStream.Length < 512)
				return ModuleType.Unknown;

			// Read the module mark
			moduleStream.Seek(26, SeekOrigin.Begin);
			uint mark = moduleStream.Read_B_UINT32();

			// Check the mark
			if ((mark & 0xffffff00) == 0x562e3200)		// V.2
				return ModuleType.SoundMon11;

			if ((mark & 0xffffff00) == 0x562e3300)		// V.3
				return ModuleType.SoundMon22;

			return ModuleType.Unknown;
		}



		/********************************************************************/
		/// <summary>
		/// Frees all the memory the player have allocated
		/// </summary>
		/********************************************************************/
		private void Cleanup()
		{
			instruments = null;
			steps = null;
			tracks = null;
			waveTables = null;

			playingInfo = null;
			bpCurrent = null;
		}



		/********************************************************************/
		/// <summary>
		/// Initialize sound structures
		/// </summary>
		/********************************************************************/
		private void InitializeSound(int startPosition)
		{
			// Initialize member variables
			playingInfo = new GlobalPlayingInfo
			{
				ArpCount = 1,
				BpCount = 1,
				BpDelay = 6,
				BpRepCount = 0,
				VibIndex = 0,
				BpStep = (ushort)startPosition,
				BpPatCount = 0,
				St = 0,
				Tr = 0,
				NewPos = 0,
				PosFlag = false
			};

			endReached = false;

			// Initialize the BpCurrent structure for each channel
			bpCurrent = ArrayHelper.InitializeArray<BpCurrent>(4);
			bpCurrent[0].SynthOffset = -1;
			bpCurrent[1].SynthOffset = -1;
			bpCurrent[2].SynthOffset = -1;
			bpCurrent[3].SynthOffset = -1;

			// Allocate the temporary synth data buffer
			playingInfo.SynthBuffer = new sbyte[4][];
			playingInfo.SynthBuffer[0] = new sbyte[32];
			playingInfo.SynthBuffer[1] = new sbyte[32];
			playingInfo.SynthBuffer[2] = new sbyte[32];
			playingInfo.SynthBuffer[3] = new sbyte[32];

			// Make a copy of the wave tables
			playingInfo.WaveTables = ArrayHelper.CloneArray(waveTables);
		}



		/********************************************************************/
		/// <summary>
		/// Plays the module
		/// </summary>
		/********************************************************************/
		private void BpPlay()
		{
			// First run the real-time effects
			DoEffects();

			// Then do the synth voices
			DoSynths();

			// At last, update the positions
			playingInfo.BpCount--;
			if (playingInfo.BpCount == 0)
			{
				playingInfo.BpCount = playingInfo.BpDelay;
				BpNext();

				for (int i = 0; i < 4; i++)
				{
					// Is the sound restarting?
					if (bpCurrent[i].Restart)
					{
						// Copy temporary synth data back
						if (bpCurrent[i].SynthOffset >= 0)
						{
							Array.Copy(playingInfo.SynthBuffer[i], 0, playingInfo.WaveTables, bpCurrent[i].SynthOffset, 32);
							bpCurrent[i].SynthOffset = -1;
						}

						// Play the sounds
						PlayIt(i);
					}
				}
			}
		}



		/********************************************************************/
		/// <summary>
		/// Change the pattern position and/or step position
		/// </summary>
		/********************************************************************/
		private void BpNext()
		{
			for (int i = 0; i < 4; i++)
			{
				BpCurrent cur = bpCurrent[i];

				// Get the step information
				ushort track = steps[i][playingInfo.BpStep].TrackNumber;
				playingInfo.St = steps[i][playingInfo.BpStep].SoundTranspose;
				playingInfo.Tr = steps[i][playingInfo.BpStep].Transpose;

				// Find the track
				Track curTrack = tracks[track - 1][playingInfo.BpPatCount];

				// Is there any note?
				sbyte note = curTrack.Note;
				if (note != 0)
				{
					// Stop the effects
					cur.AutoSlide = 0;
					cur.AutoArp = 0;
					cur.Vibrato = 0;

					// Find the note number and period
					if ((curTrack.Optional != Optional.Transpose) || ((curTrack.OptionalData & 0xf0) == 0))
						note += playingInfo.Tr;

					cur.Note = (byte)note;
					cur.Period = Tables.Periods[note + 36 - 1];
					cur.Restart = false;

					// Should the voice be retrigged?
					if (curTrack.Optional < Optional.ChangeInversion)
					{
						cur.Restart = true;
						cur.UseDefaultVolume = true;
					}

					// Find the instrument
					byte inst = curTrack.Instrument;
					if (inst == 0)
						inst = cur.Instrument;

					if ((inst != 0) && ((curTrack.Optional != Optional.Transpose) || ((curTrack.OptionalData & 0x0f) == 0)))
					{
						inst += (byte)playingInfo.St;
						if ((inst < 1) || (inst > 15))
							inst -= (byte)playingInfo.St;
					}

					if ((curTrack.Optional < Optional.ChangeInversion) && (!cur.SynthMode || (cur.Instrument != inst)))
						cur.Instrument = inst;
				}

				DoOptionals(i, curTrack.Optional, curTrack.OptionalData);
			}

			// Change the position
			if (playingInfo.PosFlag)
			{
				playingInfo.BpPatCount = 0;
				playingInfo.BpStep = playingInfo.NewPos;
			}
			else
			{
				// Next row in the pattern
				playingInfo.BpPatCount++;
				if (playingInfo.BpPatCount == 16)
				{
					// Done with the pattern, now go to the next step
					playingInfo.PosFlag = true;
					playingInfo.BpPatCount = 0;
					playingInfo.BpStep++;

					if (playingInfo.BpStep == stepNum)
					{
						// Done with the module, repeat it
						playingInfo.BpStep = 0;
					}
				}
			}

			if (playingInfo.PosFlag)
			{
				ShowSongPosition();
				ShowTracks();

				if ((playingInfo.BpRepCount == 0) || playingInfo.FirstRepeat)
				{
					if (HasPositionBeenVisited(playingInfo.BpStep))
						endReached = true;

					MarkPositionAsVisited(playingInfo.BpStep);
				}

				playingInfo.PosFlag = false;
			}
		}



		/********************************************************************/
		/// <summary>
		/// Retrigs a sample or synth sound
		/// </summary>
		/********************************************************************/
		private void PlayIt(int voice)
		{
			BpCurrent cur = bpCurrent[voice];

			// Reset the retrig flag
			cur.Restart = false;
			VirtualChannels[voice].SetAmigaPeriod(cur.Period);

			// Get the instrument address
			if (cur.Instrument != 0)
			{
				Instrument inst = instruments[cur.Instrument - 1];

				// Is the instrument a sample?
				if (inst is SynthInstrument synthInst)
				{
					// Yes it is
					cur.SynthMode = true;
					cur.EgPtr = 0;
					cur.LfoPtr = 0;
					cur.AdsrPtr = 0;
					cur.ModPtr = 0;

					cur.EgCount = (byte)(synthInst.EgDelay + 1);
					cur.LfoCount = (byte)(synthInst.LfoDelay + 1);
					cur.AdsrCount = 1;							// Start immediate
					cur.ModCount = (byte)(synthInst.ModDelay + 1);
					cur.FxCount = (byte)(synthInst.FxDelay + 1);

					cur.FxControl = synthInst.FxControl;
					cur.EgControl = synthInst.EgControl;
					cur.LfoControl = synthInst.LfoControl;
					cur.AdsrControl = synthInst.AdsrControl;
					cur.ModControl = synthInst.ModControl;
					cur.OldEgValue = 0;

					// Play the synth sound
					int waveOffset = synthInst.WaveTable * 64;
					VirtualChannels[voice].PlaySample((short)(cur.Instrument - 1), playingInfo.WaveTables, (uint)waveOffset, synthInst.WaveLength);
					VirtualChannels[voice].SetLoop((uint)waveOffset, synthInst.WaveLength);

					// Initialize ADSR
					if (cur.AdsrControl != 0)
					{
						// Get table value
						int tmp = (playingInfo.WaveTables[synthInst.AdsrTable * 64] + 128) / 4;

						if (cur.UseDefaultVolume)
						{
							cur.Volume = (byte)synthInst.Volume;
							cur.UseDefaultVolume = false;
						}

						tmp = tmp * cur.Volume / 16;
						VirtualChannels[voice].SetVolume((ushort)(tmp > 256 ? 256 : tmp));
					}
					else
					{
						int tmp = (cur.UseDefaultVolume ? synthInst.Volume : cur.Volume) * 4;
						VirtualChannels[voice].SetVolume((ushort)(tmp > 256 ? 256 : tmp));
					}

					// Initialize the other effects
					if ((cur.EgControl != 0) || (cur.ModControl != 0) || (cur.FxControl != 0))
					{
						cur.SynthOffset = waveOffset;
						Array.Copy(playingInfo.WaveTables, waveOffset, playingInfo.SynthBuffer[voice], 0, 32);
					}
				}
				else if (inst is SampleInstrument sampleInst)
				{
					// No, it is a sample
					cur.SynthMode = false;
					cur.LfoControl = 0;

					if (sampleInst.Adr == null)
						VirtualChannels[voice].Mute();
					else
					{
						// Play the sample
						VirtualChannels[voice].PlaySample((short)(cur.Instrument - 1), sampleInst.Adr, 0, sampleInst.Length);

						// Set the loop if any
						if (sampleInst.LoopLength > 2)
							VirtualChannels[voice].SetLoop(sampleInst.LoopStart, sampleInst.LoopLength);

						// Set the volume
						int tmp = (cur.UseDefaultVolume ? sampleInst.Volume : cur.Volume) * 4;
						VirtualChannels[voice].SetVolume((ushort)(tmp > 256 ? 256 : tmp));
					}
				}
			}
		}



		/********************************************************************/
		/// <summary>
		/// Parses the track optionals
		/// </summary>
		/********************************************************************/
		private void DoOptionals(int voice, Optional optional, byte optionalData)
		{
			BpCurrent cur = bpCurrent[voice];

			switch (optional)
			{
				// Arpeggio once
				case Optional.ArpeggioOnce:
				{
					cur.ArpValue = optionalData;
					break;
				}

				// Set volume
				case Optional.SetVolume:
				{
					if (optionalData > 64)
						optionalData = 64;

					cur.Volume = optionalData;
					cur.UseDefaultVolume = false;

					if (currentModuleType == ModuleType.SoundMon11)
						VirtualChannels[voice].SetAmigaVolume(optionalData);
					else
					{
						if (!cur.SynthMode)
							VirtualChannels[voice].SetAmigaVolume(optionalData);
					}
					break;
				}

				// Set speed
				case Optional.SetSpeed:
				{
					playingInfo.BpCount = optionalData;
					playingInfo.BpDelay = optionalData;

					ShowSpeed();
					break;
				}

				// Filter control
				case Optional.Filter:
				{
					AmigaFilter = optionalData != 0;
					break;
				}

				// Period lift up
				case Optional.PortUp:
				{
					cur.Period -= optionalData;
					cur.ArpValue = 0;
					break;
				}

				// Period lift down
				case Optional.PortDown:
				{
					cur.Period += optionalData;
					cur.ArpValue = 0;
					break;
				}

				// Set repeat count / Vibrato
				case Optional.Vibrato:
				{
					if (currentModuleType == ModuleType.SoundMon11)
					{
						if (playingInfo.BpRepCount == 0)
						{
							playingInfo.BpRepCount = optionalData;

							if (playingInfo.BpRepCount != 0)
								playingInfo.FirstRepeat = true;
						}
					}
					else
						cur.Vibrato = (sbyte)optionalData;

					break;
				}

				// DBRA repeat count / Jump to step
				case Optional.Jump:
				{
					switch (currentModuleType)
					{
						case ModuleType.SoundMon11:
						{
							if (playingInfo.BpRepCount != 0)
							{
								playingInfo.FirstRepeat = false;

								playingInfo.BpRepCount--;
								if (playingInfo.BpRepCount != 0)
								{
									playingInfo.NewPos = optionalData;
									playingInfo.PosFlag = true;

									// Set the "end" mark, if we have to
									// repeat the block a lot
									if ((playingInfo.BpRepCount == 254) || ((playingInfo.BpRepCount >= 15) && (playingInfo.NewPos < playingInfo.BpStep)))
										endReached = true;
								}
							}
							break;
						}

						case ModuleType.SoundMon22:
						{
							playingInfo.NewPos = optionalData;
							playingInfo.PosFlag = true;
							break;
						}
					}
					break;
				}

				// Set auto slide
				case Optional.SetAutoSlide:
				{
					cur.AutoSlide = (sbyte)optionalData;
					break;
				}

				// Set continuous arpeggio
				case Optional.SetArpeggio:
				{
					cur.AutoArp = optionalData;

					if (currentModuleType == ModuleType.SoundMon22)
					{
						cur.AdsrPtr = 0;

						if (cur.AdsrControl == 0)
							cur.AdsrControl = 1;
					}
					break;
				}

				// Change fx type
				case Optional.ChangeFx:
				{
					cur.FxControl = optionalData;
					break;
				}

				// Changes from inversion to backward inversion (or vice versa) or
				// from transform to backward transform
				case Optional.ChangeInversion:
				{
					cur.AutoArp = optionalData;
					cur.FxControl = (byte)(cur.FxControl ^ 1);
					cur.AdsrPtr = 0;

					if (cur.AdsrControl == 0)
						cur.AdsrControl = 1;

					break;
				}

				// Reset ADSR on synth sound, but not EG, averaging, transform etc.
				case Optional.ResetAdsr:
				{
					cur.AutoArp = optionalData;
					cur.AdsrPtr = 0;

					if (cur.AdsrControl == 0)
						cur.AdsrControl = 1;

					break;
				}

				// Same as above, but does not reset ADSR (just changes note)
				case Optional.ChangeNote:
				{
					cur.AutoArp = optionalData;
					break;
				}
			}
		}



		/********************************************************************/
		/// <summary>
		/// Updates the real-time effects
		/// </summary>
		/********************************************************************/
		private void DoEffects()
		{
			// Adjust the arpeggio counter
			playingInfo.ArpCount = (byte)((playingInfo.ArpCount - 1) & 3);

			// Adjust the vibrato table index
			playingInfo.VibIndex = (byte)((playingInfo.VibIndex + 1) & 7);

			for (int i = 0; i < 4; i++)
			{
				BpCurrent cur = bpCurrent[i];

				// Auto slide
				cur.Period = (ushort)(cur.Period + cur.AutoSlide);

				// Vibrato
				if (cur.Vibrato != 0)
					VirtualChannels[i].SetAmigaPeriod((ushort)(cur.Period + Tables.VibratoTable[playingInfo.VibIndex] / cur.Vibrato));
				else
					VirtualChannels[i].SetAmigaPeriod(cur.Period);

				// Arpeggio
				if ((cur.ArpValue != 0) || (cur.AutoArp != 0))
				{
					int note = (sbyte)cur.Note;

					if (playingInfo.ArpCount == 0)
						note += ((cur.ArpValue & 0xf0) >> 4) + ((cur.AutoArp & 0xf0) >> 4);
					else
					{
						if (playingInfo.ArpCount == 1)
							note += (cur.ArpValue & 0x0f) + (cur.AutoArp & 0x0f);
					}

					// Find the period
					cur.Restart = false;
					cur.Period = Tables.Periods[note + 36 - 1];
					VirtualChannels[i].SetAmigaPeriod(cur.Period);
				}
			}
		}



		/********************************************************************/
		/// <summary>
		/// Calculate and run synth effects
		/// </summary>
		/********************************************************************/
		private void DoSynths()
		{
			for (int i = 0; i < 4; i++)
			{
				BpCurrent cur = bpCurrent[i];

				// Do the current voice play a synth sample?
				if (cur.SynthMode)
				{
					// Yes, begin to do the synth effects
					//
					// Get the instrument
					if (instruments[cur.Instrument - 1] is SynthInstrument synthInst)
					{
						DoAdsr(i, cur, synthInst);
						DoLfo(i, cur, synthInst);

						// Do we have a synth buffer?
						if (cur.SynthOffset >= 0)
						{
							DoEg(i, cur, synthInst);
							DoFx(i, cur, synthInst);
							DoMod(cur, synthInst);
						}
					}
				}
			}
		}



		/********************************************************************/
		/// <summary>
		/// Handle the ADSR
		/// </summary>
		/********************************************************************/
		private void DoAdsr(int voice, BpCurrent cur, SynthInstrument synthInst)
		{
			if (cur.AdsrControl != 0)
			{
				cur.AdsrCount--;
				if (cur.AdsrCount == 0)
				{
					// Reset counter
					cur.AdsrCount = synthInst.AdsrSpeed;

					// Calculate new volume
					int tableValue = (((playingInfo.WaveTables[synthInst.AdsrTable * 64 + cur.AdsrPtr] + 128) / 4) * cur.Volume) / 16;
					VirtualChannels[voice].SetVolume((ushort)(tableValue > 256 ? 256 : tableValue));

					// Update the ADSR pointer
					cur.AdsrPtr++;
					if (cur.AdsrPtr == synthInst.AdsrLength)
					{
						cur.AdsrPtr = 0;

						// Only do the ADSR once?
						if (cur.AdsrControl == 1)
							cur.AdsrControl = 0;
					}
				}
			}
		}



		/********************************************************************/
		/// <summary>
		/// Handle the LFO
		/// </summary>
		/********************************************************************/
		private void DoLfo(int voice, BpCurrent cur, SynthInstrument synthInst)
		{
			if (cur.LfoControl != 0)
			{
				cur.LfoCount--;
				if (cur.LfoCount == 0)
				{
					// Reset counter
					cur.LfoCount = synthInst.LfoSpeed;

					// Get the wave table value
					int tableValue = playingInfo.WaveTables[synthInst.LfoTable * 64 + cur.LfoPtr];

					// Adjust the value by the LFO depth
					if (synthInst.LfoDepth != 0)
						tableValue /= synthInst.LfoDepth;

					// Calculate and set the new period
					VirtualChannels[voice].SetAmigaPeriod((ushort)(cur.Period + tableValue));

					// Update the LFO pointer
					cur.LfoPtr++;
					if (cur.LfoPtr == synthInst.LfoLength)
					{
						cur.LfoPtr = 0;

						// Only do the LFO once?
						if (cur.LfoControl == 1)
							cur.LfoControl = 0;
					}
				}
			}
		}



		/********************************************************************/
		/// <summary>
		/// Handle the EG
		/// </summary>
		/********************************************************************/
		private void DoEg(int voice, BpCurrent cur, SynthInstrument synthInst)
		{
			if (cur.EgControl != 0)
			{
				cur.EgCount--;
				if (cur.EgCount == 0)
				{
					// Reset counter
					cur.EgCount = synthInst.EgSpeed;

					// Calculate new EG value
					int tableValue = cur.OldEgValue;
					cur.OldEgValue = (byte)((playingInfo.WaveTables[synthInst.EgTable * 64 + cur.EgPtr] + 128) / 8);

					// Do we need to do the EG thing at all?
					if (cur.OldEgValue != tableValue)
					{
						// Find the source and destination offset
						sbyte[] source = playingInfo.SynthBuffer[voice];
						int sourceOffset = tableValue;
						int destOffset = cur.SynthOffset + tableValue;

						if (cur.OldEgValue < tableValue)
						{
							tableValue = tableValue - cur.OldEgValue;

							for (int j = 0; j < tableValue; j++)
								playingInfo.WaveTables[--destOffset] = source[--sourceOffset];
						}
						else
						{
							tableValue = cur.OldEgValue - tableValue;

							for (int j = 0; j < tableValue; j++)
								playingInfo.WaveTables[destOffset++] = (sbyte)-source[sourceOffset++];
						}
					}

					// Update the EG pointer
					cur.EgPtr++;
					if (cur.EgPtr == synthInst.EgLength)
					{
						cur.EgPtr = 0;

						// Only do the EG once?
						if (cur.EgControl == 1)
							cur.EgControl = 0;
					}
				}
			}
		}



		/********************************************************************/
		/// <summary>
		/// Handle the synth effects
		/// </summary>
		/********************************************************************/
		private void DoFx(int voice, BpCurrent cur, SynthInstrument synthInst)
		{
			switch (cur.FxControl)
			{
				case 1:
				{
					cur.FxCount--;
					if (cur.FxControl == 0)
					{
						// Reset counter
						cur.FxCount = synthInst.FxSpeed;
						Averaging(voice);
					}
					break;
				}

				case 2:
				{
					Transform2(voice, (sbyte)synthInst.FxSpeed);
					break;
				}

				case 3:
				case 5:
				{
					Transform3(voice, (sbyte)synthInst.FxSpeed);
					break;
				}

				case 4:
				{
					Transform4(voice, (sbyte)synthInst.FxSpeed);
					break;
				}

				case 6:
				{
					cur.FxCount--;
					if (cur.FxCount == 0)
					{
						cur.FxControl = 0;
						cur.FxCount = 1;

						Array.Copy(playingInfo.WaveTables, cur.SynthOffset + 64, playingInfo.WaveTables, cur.SynthOffset, 32);
					}
					break;
				}
			}
		}



		/********************************************************************/
		/// <summary>
		/// Handle the MOD
		/// </summary>
		/********************************************************************/
		private void DoMod(BpCurrent cur, SynthInstrument synthInst)
		{
			if (cur.ModControl != 0)
			{
				cur.ModCount--;
				if (cur.ModCount == 0)
				{
					// Reset counter
					cur.ModCount = synthInst.ModSpeed;

					// Get the table value and store it in the synth sample
					playingInfo.WaveTables[cur.SynthOffset + 32] = playingInfo.WaveTables[synthInst.ModTable * 64 + cur.ModPtr];

					// Update the MOD pointer
					cur.ModPtr++;
					if (cur.ModPtr == synthInst.ModLength)
					{
						cur.ModPtr = 0;

						// Only do the MOD once?
						if (cur.ModControl == 1)
							cur.ModControl = 0;
					}
				}
			}
		}



		/********************************************************************/
		/// <summary>
		/// Averages the synth sample
		/// </summary>
		/********************************************************************/
		private void Averaging(int voice)
		{
			int synthOffset = bpCurrent[voice].SynthOffset;
			sbyte lastVal = playingInfo.WaveTables[synthOffset];

			for (int i = 0; i < 32 - 1; i++)
			{
				lastVal = (sbyte)((lastVal + playingInfo.WaveTables[synthOffset + 1]) / 2);
				playingInfo.WaveTables[synthOffset++] = lastVal;
			}
		}



		/********************************************************************/
		/// <summary>
		/// Transform the synth sample using method 2
		/// </summary>
		/********************************************************************/
		private void Transform2(int voice, sbyte delta)
		{
			sbyte[] source = playingInfo.SynthBuffer[voice];
			int sourceOffset = 31;
			int destOffset = bpCurrent[voice].SynthOffset;

			for (int i = 0; i < 32; i++)
			{
				sbyte sourceVal = source[sourceOffset];
				sbyte destVal = playingInfo.WaveTables[destOffset];

				if (sourceVal < destVal)
					playingInfo.WaveTables[destOffset] -= delta;
				else
				{
					if (sourceVal > destVal)
						playingInfo.WaveTables[destOffset] += delta;
				}

				sourceOffset--;
				destOffset++;
			}
		}



		/********************************************************************/
		/// <summary>
		/// Transform the synth sample using method 3
		/// </summary>
		/********************************************************************/
		private void Transform3(int voice, sbyte delta)
		{
			sbyte[] source = playingInfo.SynthBuffer[voice];
			int sourceOffset = 0;
			int destOffset = bpCurrent[voice].SynthOffset;

			for (int i = 0; i < 32; i++)
			{
				sbyte sourceVal = source[sourceOffset];
				sbyte destVal = playingInfo.WaveTables[destOffset];

				if (sourceVal < destVal)
					playingInfo.WaveTables[destOffset] -= delta;
				else
				{
					if (sourceVal > destVal)
						playingInfo.WaveTables[destOffset] += delta;
				}

				sourceOffset++;
				destOffset++;
			}
		}



		/********************************************************************/
		/// <summary>
		/// Transform the synth sample using method 4
		/// </summary>
		/********************************************************************/
		private void Transform4(int voice, sbyte delta)
		{
			int sourceOffset = bpCurrent[voice].SynthOffset + 64;
			int destOffset = bpCurrent[voice].SynthOffset;

			for (int i = 0; i < 32; i++)
			{
				sbyte sourceVal = sourceOffset >= playingInfo.WaveTables.Length ? (sbyte)0 : playingInfo.WaveTables[sourceOffset];
				sbyte destVal = playingInfo.WaveTables[destOffset];

				if (sourceVal < destVal)
					playingInfo.WaveTables[destOffset] -= delta;
				else
				{
					if (sourceVal > destVal)
						playingInfo.WaveTables[destOffset] += delta;
				}

				sourceOffset++;
				destOffset++;
			}
		}



		/********************************************************************/
		/// <summary>
		/// Will update the module information with current song position
		/// </summary>
		/********************************************************************/
		private void ShowSongPosition()
		{
			OnModuleInfoChanged(InfoPositionLine, playingInfo.BpStep.ToString());
		}



		/********************************************************************/
		/// <summary>
		/// Will update the module information with track numbers
		/// </summary>
		/********************************************************************/
		private void ShowTracks()
		{
			OnModuleInfoChanged(InfoTrackLine, FormatTracks());
		}



		/********************************************************************/
		/// <summary>
		/// Will update the module information with current speed
		/// </summary>
		/********************************************************************/
		private void ShowSpeed()
		{
			OnModuleInfoChanged(InfoSpeedLine, playingInfo.BpDelay.ToString());
		}



		/********************************************************************/
		/// <summary>
		/// Will update the module information with all dynamic values
		/// </summary>
		/********************************************************************/
		private void UpdateModuleInformation()
		{
			ShowSongPosition();
			ShowTracks();
			ShowSpeed();
		}



		/********************************************************************/
		/// <summary>
		/// Return a string containing the playing tracks
		/// </summary>
		/********************************************************************/
		private string FormatTracks()
		{
			StringBuilder sb = new StringBuilder();

			for (int i = 0; i < 4; i++)
			{
				sb.Append(steps[i][playingInfo.BpStep].TrackNumber);
				sb.Append(", ");
			}

			sb.Remove(sb.Length - 2, 2);

			return sb.ToString();
		}
		#endregion
	}
}
