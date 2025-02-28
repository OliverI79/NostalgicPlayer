﻿/******************************************************************************/
/* This source, or parts thereof, may be used in any software as long the     */
/* license of NostalgicPlayer is keep. See the LICENSE file for more          */
/* information.                                                               */
/******************************************************************************/
using System;
using System.Runtime.InteropServices;
using Polycode.NostalgicPlayer.Kit.Containers.Types;
using Polycode.NostalgicPlayer.PlayerLibrary.Sound.Mixer.Containers;
using Polycode.NostalgicPlayer.PlayerLibrary.Utility;

namespace Polycode.NostalgicPlayer.PlayerLibrary.Sound.Mixer
{
	/// <summary>
	/// Normal mixer implementation
	/// </summary>
	internal class MixerNormal : MixerBase
	{
		private const int FracBits = 11;
		private const int FracMask = ((1 << FracBits) - 1);

		private const int ClickShift = 6;
		private const int ClickBuffer = 1 << ClickShift;

		#region Overrides
		/********************************************************************/
		/// <summary>
		/// Will initialize mixer stuff
		/// </summary>
		/********************************************************************/
		protected override void InitMixer()
		{
		}



		/********************************************************************/
		/// <summary>
		/// Will cleanup mixer stuff
		/// </summary>
		/********************************************************************/
		protected override void CleanupMixer()
		{
		}



		/********************************************************************/
		/// <summary>
		/// Returns the click constant value
		/// </summary>
		/********************************************************************/
		public override int GetClickConstant()
		{
			return ClickBuffer;
		}



		/********************************************************************/
		/// <summary>
		/// This is the main mixer method
		/// </summary>
		/********************************************************************/
		public override void Mixing(MixerInfo mixerInfo, int[][] channelMap, int offsetInFrames, int todoInFrames)
		{
			int offsetInSamples = offsetInFrames * mixerInfo.MixerChannels;

			// Loop through all the channels and mix the samples into the buffer
			for (int t = 0; t < channelNumber; t++)
			{
				VoiceInfo vnf = voiceInfo[t];
				VoiceSampleInfo vsi = vnf.SampleInfo;

				if (vnf.Kick)
				{
					vnf.Current = ((long)vsi.Sample.Start) << FracBits;
					vnf.Kick = false;
					vnf.Active = true;
				}

				if (vnf.Frequency == 0)
					vnf.Active = false;

				if (vnf.Active)
				{
					vnf.Increment = ((long)vnf.Frequency << FracBits) / mixerFrequency;

					if ((vsi.Flags & SampleFlag.Reverse) != 0)
						vnf.Increment = -vnf.Increment;

					if ((vnf.Flags & VoiceFlag.ChangePosition) != 0)
					{
						long newPosition;

						if (vnf.RelativePosition)
							newPosition = (vnf.Current >> FracBits) + vnf.NewPosition;
						else
							newPosition = vnf.NewPosition;

						if ((newPosition >= 0) && (newPosition < (vsi.Loop != null ? (vsi.Loop.Start + vsi.Loop.Length) : (vsi.Sample.Start + vsi.Sample.Length))))
							vnf.Current = newPosition << FracBits;

						vnf.Flags &= ~VoiceFlag.ChangePosition;
					}

					int vol = vnf.Enabled ? vnf.Volume : 0;

					vnf.OldLeftVolume = vnf.LeftVolumeSelected;
					vnf.OldRightVolume = vnf.RightVolumeSelected;

					if (mixerInfo.MixerChannels == 2)
					{
						if (vnf.Panning != (int)ChannelPanningType.Surround)
						{
							// Stereo, calculate the volume with panning
							int pan = (((vnf.Panning - 128) * stereoSeparation) / 128) + 128;

							vnf.LeftVolumeSelected = (vol * ((int)ChannelPanningType.Right - pan)) >> 8;
							vnf.RightVolumeSelected = (vol * pan) >> 8;
						}
						else
						{
							// Dolby Surround
							vnf.LeftVolumeSelected = vnf.RightVolumeSelected = vol / 2;
						}
					}
					else
					{
						// Well, just mono
						vnf.LeftVolumeSelected = vol;
					}

					AddChannel(mixerInfo, vnf, channelMap[t], offsetInSamples, todoInFrames);
				}
			}
		}



		/********************************************************************/
		/// <summary>
		/// Convert the mix buffer to the output format and store the result
		/// in the supplied buffer
		/// </summary>
		/********************************************************************/
		public override void ConvertMixedData(MixerInfo mixerInfo, byte[] dest, int offsetInBytes, int[] source, int todoInFrames, int samplesToSkip)
		{
			MixConvertTo32(mixerInfo, MemoryMarshal.Cast<byte, int>(dest), offsetInBytes / 4, source, todoInFrames * mixerInfo.MixerChannels, samplesToSkip);
		}
		#endregion

		#region Private methods
		/********************************************************************/
		/// <summary>
		/// Mix a channel into the buffer
		/// </summary>
		/********************************************************************/
		private void AddChannel(MixerInfo mixerInfo, VoiceInfo vnf, int[] buf, int offsetInSamples, int todoInFrames)
		{
			// todoInFrames at this point is actually the same as todoInSamples, since it works on the
			// sample to be mixed into the buf[] and the sample is in mono

			VoiceSampleInfo vsi = vnf.SampleInfo;

			Array sampleData = vsi.Sample.SampleData;

			if (sampleData == null)
			{
				vnf.Current = 0;
				vnf.Active = false;
				return;
			}

			// The current size of the playing sample in fixed point
			long idxEnd = vsi.Sample.Length != 0 ? ((long)(vsi.Sample.Start + vsi.Sample.Length) << FracBits) - 1 : 0;

			long idxLoopStart = 0, idxLoopEnd = 0;

			if (vsi.Loop != null)
			{
				// The loop start position in fixed point
				idxLoopStart = (long)vsi.Loop.Start << FracBits;

				// The loop end position in fixed point
				idxLoopEnd = vsi.Loop.Length != 0 ? ((long)(vsi.Loop.Start + vsi.Loop.Length) << FracBits) - 1 : 0;
			}

			// Update the 'current' index so the sample loops, or
			// stops playing if it reached the end of the sample
			while (todoInFrames > 0)
			{
				if ((vsi.Flags & SampleFlag.Reverse) != 0)
				{
					// The sampling is playing in reverse
					if ((vsi.Loop != null) && (vnf.Current < idxLoopStart))
					{
						// The sample is looping, and has reached the loop start index
						if ((vsi.Flags & SampleFlag.Bidi) != 0)
						{
							// Sample is doing bidirectional loops, so 'bounce'
							// the current index against the idxLoopStart
							vnf.Current = idxLoopStart + (idxLoopStart - vnf.Current);
							vnf.Increment = -vnf.Increment;
							vsi.Flags &= ~SampleFlag.Reverse;
						}
						else
						{
							// Normal backwards looping, so set the
							// current position to loopEnd index
							vnf.Current = idxLoopEnd - (idxLoopStart - vnf.Current);
						}
					}
					else
					{
						// The sample is not looping, so check if it reached index 0
						if (vnf.Current < 0)
						{
							// Playing index reached 0, so stop playing this sample
							vnf.Current = 0;
							vnf.Active = false;
							break;
						}
					}
				}
				else
				{
					void SetNewSample()
					{
						vnf.SampleInfo = vnf.NewSampleInfo;
						vnf.NewSampleInfo = null;

						vsi = vnf.SampleInfo;
						vnf.Current = (long)vsi.Sample.Start << FracBits;
						idxEnd = vsi.Sample.Length != 0 ? ((long)(vsi.Sample.Start + vsi.Sample.Length) << FracBits) - 1 : 0;

						if (vsi.Loop != null)
						{
							idxLoopStart = (long)vsi.Loop.Start << FracBits;
							idxLoopEnd = vsi.Loop.Length != 0 ? ((long)(vsi.Loop.Start + vsi.Loop.Length) << FracBits) - 1 : 0;
						}
						else
						{
							idxLoopStart = 0;
							idxLoopEnd = 0;
						}

						sampleData = vsi.Sample.SampleData;
					}

					// The sample is playing forward
					if (vsi.Loop != null)
					{
						if (vnf.Current >= idxLoopEnd)
						{
							if (vnf.NewSampleInfo != null)
								SetNewSample();
							else
							{
								// Loop sample
								//
								// Copy the loop address
								sampleData = vsi.Loop.SampleData;

								// Recalculate loop indexes
								long idxNewLoopStart = (long)vsi.Loop.Start << FracBits;
								long idxNewLoopEnd = vsi.Loop.Length != 0 ? ((long)(vsi.Loop.Start + vsi.Loop.Length) << FracBits) - 1 : 0;

								// The sample is looping, so check if it reached the loopEnd index
								if ((vnf.SampleInfo.Flags & SampleFlag.Bidi) != 0)
								{
									// Sample is doing bidirectional loops, so 'bounce'
									// the current index against the idxLoopEnd
									vnf.Current = idxNewLoopEnd - (vnf.Current - idxLoopEnd);
									vnf.Increment = -vnf.Increment;
									vsi.Flags |= SampleFlag.Reverse;
								}
								else
								{
									// Normal looping, so set the
									// current position to loopEnd index
									vnf.Current = idxNewLoopStart + (vnf.Current - idxLoopEnd);
								}

								idxLoopStart = idxNewLoopStart;
								idxLoopEnd = idxNewLoopEnd;
							}
						}
					}
					else
					{
						// Sample is not looping, so check if it reached the last position
						if (vnf.Current >= idxEnd)
						{
							if (vnf.NewSampleInfo != null)
								SetNewSample();
							else
							{
								// Stop playing this sample
								vnf.Current = 0;
								vnf.Active = false;
								break;
							}
						}
					}
				}

				long end = (vsi.Flags & SampleFlag.Reverse) != 0 ? vsi.Loop != null ? idxLoopStart : 0 :
							vsi.Loop != null ? idxLoopEnd : idxEnd;

				// If the sample is not blocked
				int done;

				if (((vnf.Increment > 0) && (vnf.Current >= end)) || ((vnf.Increment < 0) && (vnf.Current <= end)) || (vnf.Increment == 0))
					done = 0;
				else
				{
					done = Math.Min((int)((end - vnf.Current) / vnf.Increment + 1), todoInFrames);
					if (done < 0)
						done = 0;
				}

				if (done == 0)
				{
					vnf.Active = false;
					break;
				}

				long endPos = vnf.Current + done * vnf.Increment;

				if (vnf.Volume != 0)
				{
					if ((vsi.Flags & SampleFlag.Stereo) != 0)
					{
						if ((mixerInfo.MixerChannels == 2) && (vnf.Panning != (int)ChannelPanningType.Surround))
						{
							int oldVolume = vnf.RightVolumeSelected;
							vnf.RightVolumeSelected = 0;
							MixSample(mixerInfo, vnf, sampleData, 0, buf, offsetInSamples, done, 2);	// Left channel
							vnf.RightVolumeSelected = oldVolume;

							oldVolume = vnf.LeftVolumeSelected;
							vnf.LeftVolumeSelected = 0;
							vnf.Current = MixSample(mixerInfo, vnf, sampleData, 1, buf, offsetInSamples, done, 2);	// Right channel
							vnf.LeftVolumeSelected = oldVolume;
						}
						else
						{
							MixSample(mixerInfo, vnf, sampleData, 0, buf, offsetInSamples, done, 2);
							vnf.Current = MixSample(mixerInfo, vnf, sampleData, 1, buf, offsetInSamples, done, 2);
						}
					}
					else
						vnf.Current = MixSample(mixerInfo, vnf, sampleData, 0, buf, offsetInSamples, done, 1);
				}
				else
				{
					// Update the sample position
					vnf.Current = endPos;
				}

				todoInFrames -= done;
				offsetInSamples += mixerInfo.MixerChannels == 2 ? done << 1 : done;
			}
		}
		#endregion

		#region Real mixing methods

		/********************************************************************/
		/// <summary>
		/// Mix the given sample into the output buffers
		/// </summary>
		/********************************************************************/
		private long MixSample(MixerInfo mixerInfo, VoiceInfo vnf, Array s, int sourceOffset, int[] buf, int offsetInSamples, int todoInSamples, int step)
		{
			// Check to see if we need to make interpolation on the mixing
			if (mixerInfo.EnableInterpolation)
			{
				if ((vnf.SampleInfo.Flags & SampleFlag._16Bits) != 0)
				{
					Span<short> source = SampleHelper.ConvertSampleTo16Bit(s, 0);

					// 16 bit input sample to be mixed
					if (mixerInfo.MixerChannels == 2)
					{
						if ((vnf.Panning == (int)ChannelPanningType.Surround) && mixerInfo.EnableSurround)
							return Mix16SurroundInterpolation(source, sourceOffset, buf, offsetInSamples, vnf.Current, vnf.Increment, todoInSamples, step, vnf.LeftVolumeSelected, vnf.RightVolumeSelected, vnf.OldLeftVolume, vnf.OldRightVolume, ref vnf.RampVolume);

						return Mix16StereoInterpolation(source, sourceOffset, buf, offsetInSamples, vnf.Current, vnf.Increment, todoInSamples, step, vnf.LeftVolumeSelected, vnf.RightVolumeSelected, vnf.OldLeftVolume, vnf.OldRightVolume, ref vnf.RampVolume);
					}

					return Mix16MonoInterpolation(source, sourceOffset, buf, offsetInSamples, vnf.Current, vnf.Increment, todoInSamples, step, vnf.LeftVolumeSelected, vnf.OldLeftVolume, ref vnf.RampVolume);
				}
				else
				{
					Span<sbyte> source = SampleHelper.ConvertSampleTo8Bit(s, 0);

					// 8 bit input sample to be mixed
					if (mixerInfo.MixerChannels == 2)
					{
						if ((vnf.Panning == (int)ChannelPanningType.Surround) && mixerInfo.EnableSurround)
							return Mix8SurroundInterpolation(source, sourceOffset, buf, offsetInSamples, vnf.Current, vnf.Increment, todoInSamples, step, vnf.LeftVolumeSelected, vnf.RightVolumeSelected, vnf.OldLeftVolume, vnf.OldRightVolume, ref vnf.RampVolume);

						return Mix8StereoInterpolation(source, sourceOffset, buf, offsetInSamples, vnf.Current, vnf.Increment, todoInSamples, step, vnf.LeftVolumeSelected, vnf.RightVolumeSelected, vnf.OldLeftVolume, vnf.OldRightVolume, ref vnf.RampVolume);
					}

					return Mix8MonoInterpolation(source, sourceOffset, buf, offsetInSamples, vnf.Current, vnf.Increment, todoInSamples, step, vnf.LeftVolumeSelected, vnf.OldLeftVolume, ref vnf.RampVolume);
				}
			}

			// No interpolation
			if ((vnf.SampleInfo.Flags & SampleFlag._16Bits) != 0)
			{
				Span<short> source = SampleHelper.ConvertSampleTo16Bit(s, 0);

				// 16 bit input sample to be mixed
				if (mixerInfo.MixerChannels == 2)
				{
					if ((vnf.Panning == (int)ChannelPanningType.Surround) && mixerInfo.EnableSurround)
						return Mix16SurroundNormal(source, sourceOffset, buf, offsetInSamples, vnf.Current, vnf.Increment, todoInSamples, step, vnf.LeftVolumeSelected, vnf.RightVolumeSelected);

					return Mix16StereoNormal(source, sourceOffset, buf, offsetInSamples, vnf.Current, vnf.Increment, todoInSamples, step, vnf.LeftVolumeSelected, vnf.RightVolumeSelected);
				}

				return Mix16MonoNormal(source, sourceOffset, buf, offsetInSamples, vnf.Current, vnf.Increment, todoInSamples, step, vnf.LeftVolumeSelected);
			}
			else
			{
				Span<sbyte> source = SampleHelper.ConvertSampleTo8Bit(s, 0);

				// 8 bit input sample to be mixed
				if (mixerInfo.MixerChannels == 2)
				{
					if ((vnf.Panning == (int)ChannelPanningType.Surround) && mixerInfo.EnableSurround)
						return Mix8SurroundNormal(source, sourceOffset, buf, offsetInSamples, vnf.Current, vnf.Increment, todoInSamples, step, vnf.LeftVolumeSelected, vnf.RightVolumeSelected);

					return Mix8StereoNormal(source, sourceOffset, buf, offsetInSamples, vnf.Current, vnf.Increment, todoInSamples, step, vnf.LeftVolumeSelected, vnf.RightVolumeSelected);
				}

				return Mix8MonoNormal(source, sourceOffset, buf, offsetInSamples, vnf.Current, vnf.Increment, todoInSamples, step, vnf.LeftVolumeSelected);
			}
		}

		#region 8 bit sample

		#region Normal
		/********************************************************************/
		/// <summary>
		/// Mixes a 8 bit sample into a mono output buffer
		/// </summary>
		/********************************************************************/
		private long Mix8MonoNormal(Span<sbyte> source, int sourceOffset, int[] dest, int offsetInSamples, long index, long increment, int todoInSamples, int step, int volSel)
		{
			int len = source.Length;

			while (todoInSamples-- != 0)
			{
				long idx = (index >> FracBits) * step + sourceOffset;
				if (idx >= len)
					break;

				int sample = source[(int)idx] << 7;
				index += increment;

				dest[offsetInSamples++] += volSel * sample;
			}

			return index;
		}



		/********************************************************************/
		/// <summary>
		/// Mixes a 8 bit sample into a stereo output buffer
		/// </summary>
		/********************************************************************/
		private long Mix8StereoNormal(Span<sbyte> source, int sourceOffset, int[] dest, int offsetInSamples, long index, long increment, int todoInSamples, int step, int lVolSel, int rVolSel)
		{
			int len = source.Length;

			while (todoInSamples-- != 0)
			{
				long idx = (index >> FracBits) * step + sourceOffset;
				if (idx >= len)
					break;

				int sample = source[(int)idx] << 8;
				index += increment;

				dest[offsetInSamples++] += lVolSel * sample;
				dest[offsetInSamples++] += rVolSel * sample;
			}

			return index;
		}



		/********************************************************************/
		/// <summary>
		/// Mixes a 8 bit surround sample into a stereo output buffer
		/// </summary>
		/********************************************************************/
		private long Mix8SurroundNormal(Span<sbyte> source, int sourceOffset, int[] dest, int offsetInSamples, long index, long increment, int todoInSamples, int step, int lVolSel, int rVolSel)
		{
			int len = source.Length;

			if (lVolSel >= rVolSel)
			{
				while (todoInSamples-- != 0)
				{
					long idx = (index >> FracBits) * step + sourceOffset;
					if (idx >= len)
						break;

					int sample = source[(int)idx] << 8;
					index += increment;

					dest[offsetInSamples++] += lVolSel * sample;
					dest[offsetInSamples++] -= lVolSel * sample;
				}
			}
			else
			{
				while (todoInSamples-- != 0)
				{
					long idx = (index >> FracBits) * step + sourceOffset;
					if (idx >= len)
						break;

					int sample = source[(int)idx] << 8;
					index += increment;

					dest[offsetInSamples++] -= rVolSel * sample;
					dest[offsetInSamples++] += rVolSel * sample;
				}
			}

			return index;
		}
		#endregion

		#region Interpolation
		/********************************************************************/
		/// <summary>
		/// Mixes a 8 bit sample into a mono output buffer with interpolation
		/// </summary>
		/********************************************************************/
		private long Mix8MonoInterpolation(Span<sbyte> source, int sourceOffset, int[] dest, int offsetInSamples, long index, long increment, int todoInSamples, int step, int volSel, int oldVol, ref int rampVol)
		{
			int len = source.Length;

			if (rampVol != 0)
			{
				oldVol -= volSel;

				while (todoInSamples-- != 0)
				{
					int idx = (int)((index >> FracBits) * step + sourceOffset);
					if (idx >= len)
						break;

					long a = (long)source[idx] << 7;
					long b = idx + step >= source.Length ? a : (long)source[idx + step] << 7;

					int sample = (int)(a + ((b - a) * (index & FracMask) >> FracBits));
					index += increment;

					dest[offsetInSamples++] += ((volSel << ClickShift) + oldVol * rampVol) * sample >> ClickShift;

					if (--rampVol == 0)
						break;
				}

				if (todoInSamples < 0)
					return index;
			}

			while (todoInSamples-- != 0)
			{
				int idx = (int)((index >> FracBits) * step + sourceOffset);
				if (idx >= len)
					break;

				long a = (long)source[idx] << 7;
				long b = idx + step >= source.Length ? a : (long)source[idx + step] << 7;

				int sample = (int)(a + ((b - a) * (index & FracMask) >> FracBits));
				index += increment;

				dest[offsetInSamples++] += volSel * sample;
			}

			return index;
		}



		/********************************************************************/
		/// <summary>
		/// Mixes a 8 bit sample into a stereo output buffer with
		/// interpolation
		/// </summary>
		/********************************************************************/
		private long Mix8StereoInterpolation(Span<sbyte> source, int sourceOffset, int[] dest, int offsetInSamples, long index, long increment, int todoInSamples, int step, int lVolSel, int rVolSel, int oldLVol, int oldRVol, ref int rampVol)
		{
			int len = source.Length;

			if (rampVol != 0)
			{
				oldLVol -= lVolSel;
				oldRVol -= rVolSel;

				while (todoInSamples-- != 0)
				{
					int idx = (int)((index >> FracBits) * step + sourceOffset);
					if (idx >= len)
						break;

					long a = (long)source[idx] << 8;
					long b = idx + step >= source.Length ? a : (long)source[idx + step] << 8;

					int sample = (int)(a + ((b - a) * (index & FracMask) >> FracBits));
					index += increment;

					dest[offsetInSamples++] += ((lVolSel << ClickShift) + oldLVol * rampVol) * sample >> ClickShift;
					dest[offsetInSamples++] += ((rVolSel << ClickShift) + oldRVol * rampVol) * sample >> ClickShift;

					if (--rampVol == 0)
						break;
				}

				if (todoInSamples < 0)
					return index;
			}

			while (todoInSamples-- != 0)
			{
				int idx = (int)((index >> FracBits) * step + sourceOffset);
				if (idx >= len)
					break;

				long a = (long)source[idx] << 8;
				long b = idx + step >= source.Length ? a : (long)source[idx + step] << 8;

				int sample = (int)(a + ((b - a) * (index & FracMask) >> FracBits));
				index += increment;

				dest[offsetInSamples++] += lVolSel * sample;
				dest[offsetInSamples++] += rVolSel * sample;
			}

			return index;
		}



		/********************************************************************/
		/// <summary>
		/// Mixes a 8 bit surround sample into a stereo output buffer with
		/// interpolation
		/// </summary>
		/********************************************************************/
		private long Mix8SurroundInterpolation(Span<sbyte> source, int sourceOffset, int[] dest, int offsetInSamples, long index, long increment, int todoInSamples, int step, int lVolSel, int rVolSel, int oldLVol, int oldRVol, ref int rampVol)
		{
			int oldVol, vol;
			int len = source.Length;

			if (lVolSel >= rVolSel)
			{
				vol = lVolSel;
				oldVol = oldLVol;
			}
			else
			{
				vol = rVolSel;
				oldVol = oldRVol;
			}

			if (rampVol != 0)
			{
				oldVol -= vol;

				while (todoInSamples-- != 0)
				{
					int idx = (int)((index >> FracBits) * step + sourceOffset);
					if (idx >= len)
						break;

					long a = (long)source[idx] << 8;
					long b = idx + step >= source.Length ? a : (long)source[idx + step] << 8;

					int sample = (int)(a + ((b - a) * (index & FracMask) >> FracBits));
					index += increment;

					sample = ((vol << ClickShift) + oldVol * rampVol) * sample >> ClickShift;
					dest[offsetInSamples++] += sample;
					dest[offsetInSamples++] -= sample;

					if (--rampVol == 0)
						break;
				}

				if (todoInSamples < 0)
					return index;
			}

			while (todoInSamples-- != 0)
			{
				int idx = (int)((index >> FracBits) * step + sourceOffset);
				if (idx >= len)
					break;

				long a = (long)source[idx] << 8;
				long b = idx + step >= source.Length ? a : (long)source[idx + step] << 8;

				int sample = (int)(a + ((b - a) * (index & FracMask) >> FracBits));
				index += increment;

				dest[offsetInSamples++] += vol * sample;
				dest[offsetInSamples++] -= vol * sample;
			}

			return index;
		}
		#endregion

		#endregion

		#region 16 bit sample

		#region Normal
		/********************************************************************/
		/// <summary>
		/// Mixes a 16 bit sample into a mono output buffer
		/// </summary>
		/********************************************************************/
		private long Mix16MonoNormal(Span<short> source, int sourceOffset, int[] dest, int offsetInSamples, long index, long increment, int todoInSamples, int step, int volSel)
		{
			int len = source.Length;

			while (todoInSamples-- != 0)
			{
				long idx = (index >> FracBits) * step + sourceOffset;
				if (idx >= len)
					break;

				int sample = source[(int)idx];
				index += increment;

				dest[offsetInSamples++] += volSel * sample;
			}

			return index;
		}



		/********************************************************************/
		/// <summary>
		/// Mixes a 16 bit sample into a stereo output buffer
		/// </summary>
		/********************************************************************/
		private long Mix16StereoNormal(Span<short> source, int sourceOffset, int[] dest, int offsetInSamples, long index, long increment, int todoInSamples, int step, int lVolSel, int rVolSel)
		{
			int len = source.Length;

			while (todoInSamples-- != 0)
			{
				long idx = (index >> FracBits) * step + sourceOffset;
				if (idx >= len)
					break;

				int sample = source[(int)idx];
				index += increment;

				dest[offsetInSamples++] += lVolSel * sample;
				dest[offsetInSamples++] += rVolSel * sample;
			}

			return index;
		}



		/********************************************************************/
		/// <summary>
		/// Mixes a 16 bit surround sample into a stereo output buffer
		/// </summary>
		/********************************************************************/
		private long Mix16SurroundNormal(Span<short> source, int sourceOffset, int[] dest, int offsetInSamples, long index, long increment, int todoInSamples, int step, int lVolSel, int rVolSel)
		{
			int len = source.Length;

			if (lVolSel >= rVolSel)
			{
				while (todoInSamples-- != 0)
				{
					long idx = (index >> FracBits) * step + sourceOffset;
					if (idx >= len)
						break;

					int sample = source[(int)idx];
					index += increment;

					dest[offsetInSamples++] += lVolSel * sample;
					dest[offsetInSamples++] -= lVolSel * sample;
				}
			}
			else
			{
				while (todoInSamples-- != 0)
				{
					long idx = (index >> FracBits) * step + sourceOffset;
					if (idx >= len)
						break;

					int sample = source[(int)idx];
					index += increment;

					dest[offsetInSamples++] -= rVolSel * sample;
					dest[offsetInSamples++] += rVolSel * sample;
				}
			}

			return index;
		}
		#endregion

		#region Interpolation
		/********************************************************************/
		/// <summary>
		/// Mixes a 16 bit sample into a mono output buffer with interpolation
		/// </summary>
		/********************************************************************/
		private long Mix16MonoInterpolation(Span<short> source, int sourceOffset, int[] dest, int offsetInSamples, long index, long increment, int todoInSamples, int step, int volSel, int oldVol, ref int rampVol)
		{
			int len = source.Length;

			if (rampVol != 0)
			{
				oldVol -= volSel;

				while (todoInSamples-- != 0)
				{
					int idx = (int)((index >> FracBits) * step + sourceOffset);
					if (idx >= len)
						break;

					long a = source[idx];
					long b = idx + step >= len ? a : source[idx + step];

					int sample = (int)(a + ((b - a) * (index & FracMask) >> FracBits));
					index += increment;

					dest[offsetInSamples++] += ((volSel << ClickShift) + oldVol * rampVol) * sample >> ClickShift;

					if (--rampVol == 0)
						break;
				}

				if (todoInSamples < 0)
					return index;
			}

			while (todoInSamples-- != 0)
			{
				int idx = (int)((index >> FracBits) * step + sourceOffset);
				if (idx >= len)
					break;

				long a = source[idx];
				long b = idx + step >= len ? a : source[idx + step];

				int sample = (int)(a + ((b - a) * (index & FracMask) >> FracBits));
				index += increment;

				dest[offsetInSamples++] += volSel * sample;
			}

			return index;
		}



		/********************************************************************/
		/// <summary>
		/// Mixes a 16 bit sample into a stereo output buffer with
		/// interpolation
		/// </summary>
		/********************************************************************/
		private long Mix16StereoInterpolation(Span<short> source, int sourceOffset, int[] dest, int offsetInSamples, long index, long increment, int todoInSamples, int step, int lVolSel, int rVolSel, int oldLVol, int oldRVol, ref int rampVol)
		{
			int len = source.Length;

			if (rampVol != 0)
			{
				oldLVol -= lVolSel;
				oldRVol -= rVolSel;

				while (todoInSamples-- != 0)
				{
					int idx = (int)((index >> FracBits) * step + sourceOffset);
					if (idx >= len)
						break;

					long a = source[idx];
					long b = idx + step >= len ? a : source[idx + step];

					int sample = (int)(a + ((b - a) * (index & FracMask) >> FracBits));
					index += increment;

					dest[offsetInSamples++] += ((lVolSel << ClickShift) + oldLVol * rampVol) * sample >> ClickShift;
					dest[offsetInSamples++] += ((rVolSel << ClickShift) + oldRVol * rampVol) * sample >> ClickShift;

					if (--rampVol == 0)
						break;
				}

				if (todoInSamples < 0)
					return index;
			}

			while (todoInSamples-- != 0)
			{
				int idx = (int)((index >> FracBits) * step + sourceOffset);
				if (idx >= len)
					break;

				long a = source[idx];
				long b = idx + step >= len ? a : source[idx + step];

				int sample = (int)(a + ((b - a) * (index & FracMask) >> FracBits));
				index += increment;

				dest[offsetInSamples++] += lVolSel * sample;
				dest[offsetInSamples++] += rVolSel * sample;
			}

			return index;
		}



		/********************************************************************/
		/// <summary>
		/// Mixes a 16 bit surround sample into a stereo output buffer with
		/// interpolation
		/// </summary>
		/********************************************************************/
		private long Mix16SurroundInterpolation(Span<short> source, int sourceOffset, int[] dest, int offsetInSamples, long index, long increment, int todoInSamples, int step, int lVolSel, int rVolSel, int oldLVol, int oldRVol, ref int rampVol)
		{
			int oldVol, vol;
			int len = source.Length;

			if (lVolSel >= rVolSel)
			{
				vol = lVolSel;
				oldVol = oldLVol;
			}
			else
			{
				vol = rVolSel;
				oldVol = oldRVol;
			}

			if (rampVol != 0)
			{
				oldVol -= vol;

				while (todoInSamples-- != 0)
				{
					int idx = (int)((index >> FracBits) * step + sourceOffset);
					if (idx >= len)
						break;

					long a = source[idx];
					long b = idx + step >= len ? a : source[idx + step];

					int sample = (int)(a + ((b - a) * (index & FracMask) >> FracBits));
					index += increment;

					sample = ((vol << ClickShift) + oldVol * rampVol) * sample >> ClickShift;
					dest[offsetInSamples++] += sample;
					dest[offsetInSamples++] -= sample;

					if (--rampVol == 0)
						break;
				}

				if (todoInSamples < 0)
					return index;
			}

			while (todoInSamples-- != 0)
			{
				int idx = (int)((index >> FracBits) * step + sourceOffset);
				if (idx >= len)
					break;

				long a = source[idx];
				long b = idx + step >= len ? a : source[idx + step];

				int sample = (int)(a + ((b - a) * (index & FracMask) >> FracBits));
				index += increment;

				dest[offsetInSamples++] += vol * sample;
				dest[offsetInSamples++] -= vol * sample;
			}

			return index;
		}
		#endregion

		#endregion

		#endregion

		#region Conversion methods
		private const int MixBitShift = 7;

		/********************************************************************/
		/// <summary>
		/// Converts the mixed data to a 32 bit sample buffer
		/// </summary>
		/********************************************************************/
		private void MixConvertTo32(MixerInfo mixerInfo, Span<int> dest, int offsetInSamples, int[] source, int countInSamples, int samplesToSkip)
		{
			long x1, x2, x3, x4;
			int remain;

			int sourceOffset = 0;

			if (mixerInfo.SwapSpeakers)
			{
				if (samplesToSkip == 0)
				{
					remain = countInSamples & 3;

					for (countInSamples >>= 2; countInSamples != 0; countInSamples--)
					{
						x1 = (long)source[sourceOffset++] << MixBitShift;
						x2 = (long)source[sourceOffset++] << MixBitShift;
						x3 = (long)source[sourceOffset++] << MixBitShift;
						x4 = (long)source[sourceOffset++] << MixBitShift;

						x1 = (x1 >= 2147483647) ? 2147483647 - 1 : (x1 < -2147483647) ? -2147483647 : x1;
						x2 = (x2 >= 2147483647) ? 2147483647 - 1 : (x2 < -2147483647) ? -2147483647 : x2;
						x3 = (x3 >= 2147483647) ? 2147483647 - 1 : (x3 < -2147483647) ? -2147483647 : x3;
						x4 = (x4 >= 2147483647) ? 2147483647 - 1 : (x4 < -2147483647) ? -2147483647 : x4;

						dest[offsetInSamples++] = (int)x2;
						dest[offsetInSamples++] = (int)x1;
						dest[offsetInSamples++] = (int)x4;
						dest[offsetInSamples++] = (int)x3;
					}
				}
				else
				{
					remain = countInSamples & 1;

					for (countInSamples >>= 1; countInSamples != 0; countInSamples--)
					{
						x1 = (long)source[sourceOffset++] << MixBitShift;
						x2 = (long)source[sourceOffset++] << MixBitShift;

						x1 = (x1 >= 2147483647) ? 2147483647 - 1 : (x1 < -2147483647) ? -2147483647 : x1;
						x2 = (x2 >= 2147483647) ? 2147483647 - 1 : (x2 < -2147483647) ? -2147483647 : x2;

						dest[offsetInSamples++] = (int)x2;
						dest[offsetInSamples++] = (int)x1;

						for (int i = 0; i < samplesToSkip; i++)
							dest[offsetInSamples++] = 0;
					}
				}
			}
			else
			{
				if (mixerInfo.MixerChannels == 2)
				{
					if (samplesToSkip == 0)
					{
						remain = countInSamples & 3;

						for (countInSamples >>= 2; countInSamples != 0; countInSamples--)
						{
							x1 = (long)source[sourceOffset++] << MixBitShift;
							x2 = (long)source[sourceOffset++] << MixBitShift;
							x3 = (long)source[sourceOffset++] << MixBitShift;
							x4 = (long)source[sourceOffset++] << MixBitShift;

							x1 = (x1 >= 2147483647) ? 2147483647 - 1 : (x1 < -2147483647) ? -2147483647 : x1;
							x2 = (x2 >= 2147483647) ? 2147483647 - 1 : (x2 < -2147483647) ? -2147483647 : x2;
							x3 = (x3 >= 2147483647) ? 2147483647 - 1 : (x3 < -2147483647) ? -2147483647 : x3;
							x4 = (x4 >= 2147483647) ? 2147483647 - 1 : (x4 < -2147483647) ? -2147483647 : x4;

							dest[offsetInSamples++] = (int)x1;
							dest[offsetInSamples++] = (int)x2;
							dest[offsetInSamples++] = (int)x3;
							dest[offsetInSamples++] = (int)x4;
						}
					}
					else
					{
						remain = countInSamples & 1;

						for (countInSamples >>= 1; countInSamples != 0; countInSamples--)
						{
							x1 = (long)source[sourceOffset++] << MixBitShift;
							x2 = (long)source[sourceOffset++] << MixBitShift;

							x1 = (x1 >= 2147483647) ? 2147483647 - 1 : (x1 < -2147483647) ? -2147483647 : x1;
							x2 = (x2 >= 2147483647) ? 2147483647 - 1 : (x2 < -2147483647) ? -2147483647 : x2;

							dest[offsetInSamples++] = (int)x1;
							dest[offsetInSamples++] = (int)x2;

							for (int i = 0; i < samplesToSkip; i++)
								dest[offsetInSamples++] = 0;
						}
					}
				}
				else
				{
					remain = 0;

					for (; countInSamples != 0; countInSamples--)
					{
						x1 = (long)source[sourceOffset++] << MixBitShift;
						x1 = (x1 >= 2147483647) ? 2147483647 - 1 : (x1 < -2147483647) ? -2147483647 : x1;
						dest[offsetInSamples++] = (int)x1;
					}
				}
			}

			while (remain-- != 0)
			{
				x1 = (long)source[sourceOffset++] << MixBitShift;
				x1 = (x1 >= 2147483647) ? 2147483647 - 1 : (x1 < -2147483647) ? -2147483647 : x1;
				dest[offsetInSamples++] = (int)x1;

				for (int i = 0; i < samplesToSkip; i++)
					dest[offsetInSamples++] = 0;
			}
		}
		#endregion
	}
}
