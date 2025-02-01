﻿/******************************************************************************/
/* This source, or parts thereof, may be used in any software as long the     */
/* license of NostalgicPlayer is keep. See the LICENSE file for more          */
/* information.                                                               */
/******************************************************************************/
using System;
using System.Collections.Generic;
using System.IO;
using Polycode.NostalgicPlayer.Agent.Player.JamCracker.Containers;
using Polycode.NostalgicPlayer.Kit;
using Polycode.NostalgicPlayer.Kit.Bases;
using Polycode.NostalgicPlayer.Kit.Containers;
using Polycode.NostalgicPlayer.Kit.Interfaces;
using Polycode.NostalgicPlayer.Kit.Streams;

namespace Polycode.NostalgicPlayer.Agent.Player.JamCracker
{
	/// <summary>
	/// Main worker class
	/// </summary>
	internal class JamCrackerWorker : ModulePlayerWithPositionDurationAgentBase
	{
		private ushort samplesNum;
		private ushort patternNum;
		private ushort songLen;

		private InstInfo[] instTable;
		private PattInfo[] pattTable;
		private ushort[] songTable;

		private GlobalPlayingInfo playingInfo;
		private VoiceInfo[] variables;

		private bool endReached;

		private const int InfoPositionLine = 3;
		private const int InfoPatternLine = 4;
		private const int InfoSpeedLine = 5;

		#region IPlayerAgent implementation
		/********************************************************************/
		/// <summary>
		/// Returns the file extensions that identify this player
		/// </summary>
		/********************************************************************/
		public override string[] FileExtensions => [ "jam" ];



		/********************************************************************/
		/// <summary>
		/// Test the file to see if it could be identified
		/// </summary>
		/********************************************************************/
		public override AgentResult Identify(PlayerFileInfo fileInfo)
		{
			ModuleStream moduleStream = fileInfo.ModuleStream;

			// Check the module size
			if (moduleStream.Length < 6)
				return AgentResult.Unknown;

			// Check the mark
			moduleStream.Seek(0, SeekOrigin.Begin);

			if (moduleStream.Read_B_UINT32() == 0x42654570)	// BeEp
				return AgentResult.Ok;

			return AgentResult.Unknown;
		}



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
					description = Resources.IDS_JAM_INFODESCLINE0;
					value = songLen.ToString();
					break;
				}

				// Used patterns
				case 1:
				{
					description = Resources.IDS_JAM_INFODESCLINE1;
					value = patternNum.ToString();
					break;
				}

				// Used instruments
				case 2:
				{
					description = Resources.IDS_JAM_INFODESCLINE2;
					value = samplesNum.ToString();
					break;
				}

				// Playing position
				case 3:
				{
					description = Resources.IDS_JAM_INFODESCLINE3;
					value = playingInfo.SongPos.ToString();
					break;
				}

				// Playing pattern
				case 4:
				{
					description = Resources.IDS_JAM_INFODESCLINE4;
					value = songTable[playingInfo.SongPos].ToString();
					break;
				}

				// Current speed
				case 5:
				{
					description = Resources.IDS_JAM_INFODESCLINE5;
					value = playingInfo.Wait.ToString();
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

				// Skip the module mark
				moduleStream.Read_B_UINT32();

				// Get the number of instruments
				samplesNum = moduleStream.Read_B_UINT16();

				// Allocate the instrument structures
				instTable = new InstInfo[samplesNum];

				// Read the instrument info
				for (int i = 0; i < samplesNum; i++)
				{
					InstInfo instInfo = new InstInfo();

					moduleStream.ReadString(instInfo.Name, 31);

					instInfo.Flags = moduleStream.Read_UINT8();
					instInfo.Size = moduleStream.Read_B_UINT32();
					moduleStream.Read_B_UINT32();		// Skip the address

					instTable[i] = instInfo;
				}

				// Get the number of patterns
				patternNum = moduleStream.Read_B_UINT16();

				// Allocate pattern structures
				pattTable = new PattInfo[patternNum];

				// Read the pattern information
				for (int i = 0; i < patternNum; i++)
				{
					PattInfo pattInfo = new PattInfo();

					pattInfo.Size = moduleStream.Read_B_UINT16();
					moduleStream.Read_B_UINT32();		// Skip the address

					pattTable[i] = pattInfo;
				}

				// Get the song length
				songLen = moduleStream.Read_B_UINT16();

				// Allocate and read the position array
				songTable = new ushort[songLen];

				moduleStream.ReadArray_B_UINT16s(songTable, 0, songLen);

				if (moduleStream.EndOfStream)
				{
					errorMessage = Resources.IDS_JAM_ERR_LOADING_HEADER;
					Cleanup();

					return AgentResult.Error;
				}

				// Get the pattern data
				for (int i = 0; i < patternNum; i++)
				{
					// Allocate the pattern data
					NoteInfo[] note = new NoteInfo[pattTable[i].Size * 4];
					pattTable[i].Address = note;

					// Read the data from the stream
					for (int j = 0; j < pattTable[i].Size * 4; j++)
					{
						NoteInfo noteInfo = new NoteInfo();

						noteInfo.Period = moduleStream.Read_UINT8();
						noteInfo.Instr = moduleStream.Read_INT8();
						noteInfo.Speed = moduleStream.Read_UINT8();
						noteInfo.Arpeggio = moduleStream.Read_UINT8();
						noteInfo.Vibrato = moduleStream.Read_UINT8();
						noteInfo.Phase = moduleStream.Read_UINT8();
						noteInfo.Volume = moduleStream.Read_UINT8();
						noteInfo.Porta = moduleStream.Read_UINT8();

						note[j] = noteInfo;
					}

					if (moduleStream.EndOfStream)
					{
						errorMessage = Resources.IDS_JAM_ERR_LOADING_PATTERNS;
						Cleanup();

						return AgentResult.Error;
					}
				}

				// Read the samples
				for (int i = 0; i < samplesNum; i++)
				{
					InstInfo instInfo = instTable[i];

					if (instInfo.Size != 0)
					{
						instInfo.Address = moduleStream.ReadSampleData(i, (int)instInfo.Size, out _);

						if (moduleStream.EndOfStream && (i != samplesNum - 1))
						{
							errorMessage = Resources.IDS_JAM_ERR_LOADING_SAMPLES;
							Cleanup();

							return AgentResult.Error;
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
			if (--playingInfo.WaitCnt == 0)
			{
				NewNote();
				playingInfo.WaitCnt = playingInfo.Wait;
			}

			SetChannel(variables[0], VirtualChannels[0]);
			SetChannel(variables[1], VirtualChannels[1]);
			SetChannel(variables[2], VirtualChannels[2]);
			SetChannel(variables[3], VirtualChannels[3]);

			// Have we reached the end of the module
			if (endReached)
			{
				OnEndReached(playingInfo.SongPos);
				endReached = false;

				MarkPositionAsVisited(playingInfo.SongPos);
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

				for (int j = 0; j < 3 + 3 * 12; j++)
					frequencies[4 * 12 - 3 + j] = PeriodToFrequency(Tables.Periods[j]);

				foreach (InstInfo instInfo in instTable)
				{
					SampleInfo sampleInfo = new SampleInfo
					{
						Name = EncoderCollection.Amiga.GetString(instInfo.Name),
						Flags = SampleInfo.SampleFlag.None,
						Volume = 256,
						Panning = -1,
						NoteFrequencies = frequencies
					};

					if ((instInfo.Flags & 2) != 0)
					{
						// AM sample
						sampleInfo.Type = SampleInfo.SampleType.Synthesis;
						sampleInfo.Sample = null;
						sampleInfo.Length = 0;
						sampleInfo.LoopStart = 0;
						sampleInfo.LoopLength = 0;
					}
					else
					{
						// Normal sample
						sampleInfo.Type = SampleInfo.SampleType.Sample;
						sampleInfo.Sample = instInfo.Address;
						sampleInfo.Length = instInfo.Size;

						if ((instInfo.Flags & 1) != 0)
						{
							// Sample loops
							sampleInfo.Flags |= SampleInfo.SampleFlag.Loop;
							sampleInfo.LoopStart = 0;
							sampleInfo.LoopLength = instInfo.Size;
						}
						else
						{
							// No loop
							sampleInfo.LoopStart = 0;
							sampleInfo.LoopLength = 0;
						}
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
			return songLen;
		}



		/********************************************************************/
		/// <summary>
		/// Create a snapshot of all the internal structures and return it
		/// </summary>
		/********************************************************************/
		protected override ISnapshot CreateSnapshot()
		{
			return new Snapshot(playingInfo, variables);
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
			variables = clonedSnapshot.Channels;

			UpdateModuleInformation();

			return true;
		}
		#endregion

		#region Private methods
		/********************************************************************/
		/// <summary>
		/// Initialize sound structures
		/// </summary>
		/********************************************************************/
		private void InitializeSound(int startPosition)
		{
			// Initialize other variables
			PattInfo pattInfo = pattTable[songTable[startPosition]];

			playingInfo = new GlobalPlayingInfo
			{
				SongPos = (ushort)startPosition,

				NoteCnt = pattInfo.Size,
				Address = pattInfo.Address,

				AddressIndex = 0,

				Wait = 6,
				WaitCnt = 1
			};

			endReached = false;

			// Initialize channel variables
			ushort waveOff = 0x80;

			variables = new VoiceInfo[4];
			for (int i = 0; i < 4; i++)
			{
				VoiceInfo voiceInfo = new VoiceInfo();

				voiceInfo.WaveOffset = waveOff;
				voiceInfo.Dmacon = (ushort)(1 << i);
				voiceInfo.InsNum = -1;
				voiceInfo.InsLen = 0;
				voiceInfo.InsAddress = null;
				voiceInfo.RealInsAddress = null;
				voiceInfo.PerIndex = 0;
				voiceInfo.Pers[0] = 1019;
				voiceInfo.Pers[1] = 0;
				voiceInfo.Pers[2] = 0;
				voiceInfo.Por = 0;
				voiceInfo.DeltaPor = 0;
				voiceInfo.PorLevel = 0;
				voiceInfo.Vib = 0;
				voiceInfo.DeltaVib = 0;
				voiceInfo.Vol = 0;
				voiceInfo.DeltaVol = 0;
				voiceInfo.VolLevel = 0x40;
				voiceInfo.Phase = 0;
				voiceInfo.DeltaPhase = 0;
				voiceInfo.VibCnt = 0;
				voiceInfo.VibMax = 0;
				voiceInfo.Flags = 0;

				variables[i] = voiceInfo;

				waveOff += 0x40;
			}
		}



		/********************************************************************/
		/// <summary>
		/// Frees all the memory the player has allocated
		/// </summary>
		/********************************************************************/
		private void Cleanup()
		{
			songTable = null;
			pattTable = null;
			instTable = null;

			playingInfo = null;
			variables = null;
		}



		/********************************************************************/
		/// <summary>
		/// Will get a new note from the current pattern + skip to the next
		/// pattern if necessary
		/// </summary>
		/********************************************************************/
		private void NewNote()
		{
			NoteInfo[] adr = playingInfo.Address;
			int adrIndex = playingInfo.AddressIndex;
			playingInfo.AddressIndex += 4;				// Go to the next row

			if (--playingInfo.NoteCnt == 0)
			{
				playingInfo.SongPos++;
				if (playingInfo.SongPos >= songLen)
					playingInfo.SongPos = 0;

				if (HasPositionBeenVisited(playingInfo.SongPos))
					endReached = true;

				MarkPositionAsVisited(playingInfo.SongPos);

				PattInfo pattInfo = pattTable[songTable[playingInfo.SongPos]];
				playingInfo.NoteCnt = pattInfo.Size;
				playingInfo.Address = pattInfo.Address;

				playingInfo.AddressIndex = 0;

				ShowSongPosition();
				ShowPattern();
			}

			playingInfo.TmpDmacon = 0;

			NwNote(adr[adrIndex], variables[0]);
			NwNote(adr[++adrIndex], variables[1]);
			NwNote(adr[++adrIndex], variables[2]);
			NwNote(adr[++adrIndex], variables[3]);

			SetVoice(variables[0], VirtualChannels[0]);
			SetVoice(variables[1], VirtualChannels[1]);
			SetVoice(variables[2], VirtualChannels[2]);
			SetVoice(variables[3], VirtualChannels[3]);
		}



		/********************************************************************/
		/// <summary>
		/// Parse the given pattern and set up the voice structure given
		/// </summary>
		/********************************************************************/
		private void NwNote(NoteInfo adr, VoiceInfo voice)
		{
			int perIndex;

			if (adr.Period != 0)
			{
				perIndex = adr.Period - 1;

				if ((adr.Speed & 64) != 0)
					voice.PorLevel = (short)Tables.Periods[perIndex];
				else
				{
					playingInfo.TmpDmacon |= voice.Dmacon;

					voice.PerIndex = perIndex;
					voice.Pers[0] = Tables.Periods[perIndex];
					voice.Pers[1] = Tables.Periods[perIndex];
					voice.Pers[2] = Tables.Periods[perIndex];

					voice.Por = 0;

					if (adr.Instr > samplesNum)
					{
						voice.InsAddress = null;
						voice.RealInsAddress = null;
						voice.InsLen = 0;
						voice.InsNum = -1;
						voice.Flags = 0;
					}
					else
					{
						InstInfo instInfo = instTable[adr.Instr];
						if (instInfo.Address == null)
						{
							voice.InsAddress = null;
							voice.RealInsAddress = null;
							voice.InsLen = 0;
							voice.InsNum = -1;
							voice.Flags = 0;
						}
						else
						{
							if ((instInfo.Flags & 2) == 0)
							{
								voice.InsAddress = instInfo.Address;
								voice.RealInsAddress = instInfo.Address;
								voice.InsLen = (ushort)(instInfo.Size / 2);
							}
							else
							{
								voice.RealInsAddress = instInfo.Address;
								voice.InsAddress = voice.WaveBuffer;
								Array.Copy(instInfo.Address, voice.WaveOffset, voice.InsAddress, 0, 0x40);
								voice.InsLen = 0x20;
							}

							voice.Flags = instInfo.Flags;
							voice.Vol = (short)voice.VolLevel;
							voice.InsNum = adr.Instr;
						}
					}
				}
			}

			if ((adr.Speed & 15) != 0)
			{
				playingInfo.Wait = (byte)(adr.Speed & 15);

				ShowSpeed();
			}

			// Do arpeggio
			perIndex = voice.PerIndex;

			if (adr.Arpeggio != 0)
			{
				if (adr.Arpeggio == 255)
				{
					voice.Pers[0] = Tables.Periods[perIndex];
					voice.Pers[1] = Tables.Periods[perIndex];
					voice.Pers[2] = Tables.Periods[perIndex];
				}
				else
				{
					voice.Pers[2] = Tables.Periods[perIndex + (adr.Arpeggio & 15)];
					voice.Pers[1] = Tables.Periods[perIndex + (adr.Arpeggio >> 4)];
					voice.Pers[0] = Tables.Periods[perIndex];
				}
			}

			// Do vibrato
			if (adr.Vibrato != 0)
			{
				if (adr.Vibrato == 255)
				{
					voice.Vib = 0;
					voice.DeltaVib = 0;
					voice.VibCnt = 0;
				}
				else
				{
					voice.Vib = 0;
					voice.DeltaVib = (short)(adr.Vibrato & 15);
					voice.VibMax = (byte)(adr.Vibrato >> 4);
					voice.VibCnt = (byte)(adr.Vibrato >> 5);
				}
			}

			// Do phase
			if (adr.Phase != 0)
			{
				if (adr.Phase == 255)
				{
					voice.Phase = 0;
					voice.DeltaPhase = -1;
				}
				else
				{
					voice.Phase = 0;
					voice.DeltaPhase = (short)(adr.Phase & 15);
				}
			}

			// Do volume
			short temp;
			if ((temp = adr.Volume) == 0)
			{
				if ((adr.Speed & 128) != 0)
				{
					voice.Vol = temp;
					voice.VolLevel = (ushort)temp;
					voice.DeltaVol = 0;
				}
			}
			else
			{
				if (temp == 255)
					voice.DeltaVol = 0;
				else
				{
					if ((adr.Speed & 128) != 0)
					{
						voice.Vol = temp;
						voice.VolLevel = (ushort)temp;
						voice.DeltaVol = 0;
					}
					else
					{
						temp &= 0x7f;
						if ((adr.Volume & 128) != 0)
							temp = (short)-temp;

						voice.DeltaVol = temp;
					}
				}
			}

			// Do portamento
			if ((temp = adr.Porta) != 0)
			{
				if (temp == 255)
				{
					voice.Por = 0;
					voice.DeltaPor = 0;
				}
				else
				{
					voice.Por = 0;
					if ((adr.Speed & 64) != 0)
					{
						if (voice.PorLevel <= voice.Pers[0])
							temp = (short)-temp;
					}
					else
					{
						temp &= 0x7f;
						if ((adr.Porta & 128) == 0)
						{
							temp = (short)-temp;
							voice.PorLevel = 135;
						}
						else
							voice.PorLevel = 1019;
					}

					voice.DeltaPor = temp;
				}
			}
		}



		/********************************************************************/
		/// <summary>
		/// Will setup the voice in the channel structure
		/// </summary>
		/********************************************************************/
		private void SetVoice(VoiceInfo voice, IChannel chan)
		{
			if ((playingInfo.TmpDmacon & voice.Dmacon) != 0)
			{
				// Setup the start sample
				if (voice.InsAddress == null)
					chan.Mute();
				else
				{
					chan.PlaySample(voice.InsNum, voice.InsAddress, 0, (uint)(voice.InsLen * 2));
					chan.SetAmigaPeriod(voice.Pers[0]);
				}

				// Check to see if sample loops
				if ((voice.Flags & 1) == 0)
				{
					voice.InsAddress = null;
					voice.RealInsAddress = null;
					voice.InsLen = 0;
					voice.InsNum = -1;
				}

				// Setup loop
				if (voice.InsAddress != null)
					chan.SetLoop(0, (uint)(voice.InsLen * 2));
			}
		}



		/********************************************************************/
		/// <summary>
		/// Will setup the channel structure
		/// </summary>
		/********************************************************************/
		private void SetChannel(VoiceInfo voice, IChannel chan)
		{
			while (voice.Pers[0] == 0)
				RotatePeriods(voice);

			short per = (short)(voice.Pers[0] + voice.Por);
			if (voice.Por < 0)
			{
				if (per < voice.PorLevel)
					per = voice.PorLevel;
			}
			else
			{
				if ((voice.Por != 0) && (per > voice.PorLevel))
					per = voice.PorLevel;
			}

			// Add vibrato
			per += voice.Vib;

			if (per < 135)
				per = 135;
			else if (per > 1019)
				per = 1019;

			chan.SetAmigaPeriod((uint)per);
			RotatePeriods(voice);

			voice.Por += voice.DeltaPor;
			if (voice.Por < -1019)
				voice.Por = -1019;
			else if (voice.Por > 1019)
				voice.Por = 1019;

			if (voice.VibCnt != 0)
			{
				voice.Vib += voice.DeltaVib;
				if (--voice.VibCnt == 0)
				{
					voice.DeltaVib = (short)-voice.DeltaVib;
					voice.VibCnt = voice.VibMax;
				}
			}

			chan.SetAmigaVolume((ushort)(voice.Vol));

			voice.Vol += voice.DeltaVol;
			if (voice.Vol < 0)
				voice.Vol = 0;
			else if (voice.Vol > 64)
				voice.Vol = 64;

			if (((voice.Flags & 1) != 0) && (voice.DeltaPhase != 0))
			{
				if (voice.DeltaPhase < 0)
					voice.DeltaPhase = 0;

				sbyte[] instData = voice.InsAddress;
				sbyte[] wave = voice.RealInsAddress;
				int wavePhase = voice.Phase / 4;

				for (int i = 0; i < 64; i++)
				{
					short temp = wave[i];
					temp += wave[wavePhase++];
					temp >>= 1;
					instData[i] = (sbyte)temp;
				}

				voice.Phase = (ushort)(voice.Phase + voice.DeltaPhase);
				if (voice.Phase >= 256)
					voice.Phase -= 256;
			}
		}



		/********************************************************************/
		/// <summary>
		/// Rotates the periods in the pers array
		/// </summary>
		/********************************************************************/
		private void RotatePeriods(VoiceInfo voice)
		{
			ushort temp1 = voice.Pers[0];
			voice.Pers[0] = voice.Pers[1];
			voice.Pers[1] = voice.Pers[2];
			voice.Pers[2] = temp1;
		}



		/********************************************************************/
		/// <summary>
		/// Will update the module information with current song position
		/// </summary>
		/********************************************************************/
		private void ShowSongPosition()
		{
			OnModuleInfoChanged(InfoPositionLine, playingInfo.SongPos.ToString());
		}



		/********************************************************************/
		/// <summary>
		/// Will update the module information with pattern number
		/// </summary>
		/********************************************************************/
		private void ShowPattern()
		{
			OnModuleInfoChanged(InfoPatternLine, songTable[playingInfo.SongPos].ToString());
		}



		/********************************************************************/
		/// <summary>
		/// Will update the module information with current speed
		/// </summary>
		/********************************************************************/
		private void ShowSpeed()
		{
			OnModuleInfoChanged(InfoSpeedLine, playingInfo.Wait.ToString());
		}



		/********************************************************************/
		/// <summary>
		/// Will update the module information with all dynamic values
		/// </summary>
		/********************************************************************/
		private void UpdateModuleInformation()
		{
			ShowSongPosition();
			ShowPattern();
			ShowSpeed();
		}
		#endregion
	}
}
