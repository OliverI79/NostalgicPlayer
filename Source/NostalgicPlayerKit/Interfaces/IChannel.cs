﻿/******************************************************************************/
/* This source, or parts thereof, may be used in any software as long the     */
/* license of NostalgicPlayer is keep. See the LICENSE file for more          */
/* information.                                                               */
/******************************************************************************/
using System;
using Polycode.NostalgicPlayer.Kit.Containers.Flags;
using Polycode.NostalgicPlayer.Kit.Containers.Types;

namespace Polycode.NostalgicPlayer.Kit.Interfaces
{
	/// <summary>
	/// Interface used to trigger samples when playing modules
	/// </summary>
	public interface IChannel
	{
		/// <summary>
		/// The maximum number of channels supported
		/// </summary>
		public const int MaxNumberOfChannels = 64;

		/// <summary>
		/// Will start to play the buffer in the channel. Only use this if
		/// your player is running in buffer mode. Note that the length then
		/// have to be the same for each channel
		/// </summary>
		/// <param name="adr">is a pointer to the sample in memory</param>
		/// <param name="startOffset">is the number of samples in the sample to start</param>
		/// <param name="length">is the length in samples to be played</param>
		/// <param name="flag">indicate the format of the sample and how to play it</param>
		void PlayBuffer(Array adr, uint startOffset, uint length, PlayBufferFlag flag = PlayBufferFlag.None);

		/// <summary>
		/// Will start to play the sample in the channel
		/// </summary>
		/// <param name="sampleNumber">is the sample number being played. If unknown, set it to -1</param>
		/// <param name="adr">is a pointer to the sample in memory</param>
		/// <param name="startOffset">is the number of samples in the sample to start</param>
		/// <param name="length">is the length in samples to be played</param>
		/// <param name="flag">indicate the format of the sample and how to play it</param>
		void PlaySample(short sampleNumber, Array adr, uint startOffset, uint length, PlaySampleFlag flag = PlaySampleFlag.None);

		/// <summary>
		/// Will play the sample in the channel, but first when the current
		/// sample stops or loops. No retrigger is made
		/// </summary>
		/// <param name="startOffset">is the number of samples in the sample to start</param>
		/// <param name="length">is the length in samples to be played</param>
		void SetSample(uint startOffset, uint length);

		/// <summary>
		/// Will play the sample in the channel, but first when the current
		/// sample stops or loops. No retrigger is made
		/// </summary>
		/// <param name="adr">is a pointer to the sample in memory</param>
		/// <param name="startOffset">is the number of samples in the sample to start</param>
		/// <param name="length">is the length in samples to be played</param>
		/// <param name="flag">indicate the format of the sample and how to play it</param>
		void SetSample(Array adr, uint startOffset, uint length, PlaySampleFlag flag = PlaySampleFlag.None);

		/// <summary>
		/// Will set the loop point in the sample
		/// </summary>
		/// <param name="startOffset">is the number of samples in the sample to start</param>
		/// <param name="length">is the length in samples to loop</param>
		/// <param name="type">is the type of the loop</param>
		void SetLoop(uint startOffset, uint length, ChannelLoopType type = ChannelLoopType.Normal);

		/// <summary>
		/// Will set the loop point and change the sample
		/// </summary>
		/// <param name="adr">is a pointer to the sample in memory</param>
		/// <param name="startOffset">is the number of samples in the sample to start</param>
		/// <param name="length">is the length in samples to loop</param>
		/// <param name="type">is the type of the loop</param>
		void SetLoop(Array adr, uint startOffset, uint length, ChannelLoopType type = ChannelLoopType.Normal);

		/// <summary>
		/// Set the current playing position of the sample to the value given
		/// </summary>
		/// <param name="position">The new position in the sample</param>
		/// <param name="relative">Indicate if the given position is relative to the current position or an absolute position. If true, position can be negative as well as position</param>
		void SetPosition(int position, bool relative);

		/// <summary>
		/// Will change the volume
		/// </summary>
		/// <param name="vol">is the new volume (0-256)</param>
		void SetVolume(ushort vol);

		/// <summary>
		/// Will change the volume using Amiga range
		/// </summary>
		/// <param name="vol">is the new volume (0-64)</param>
		void SetAmigaVolume(ushort vol);

		/// <summary>
		/// Will change the panning
		/// </summary>
		/// <param name="pan">is the new panning</param>
		void SetPanning(ushort pan);

		/// <summary>
		/// Will change the frequency
		/// </summary>
		/// <param name="freq">is the new frequency</param>
		void SetFrequency(uint freq);

		/// <summary>
		/// Will calculate the period given to a frequency and set it
		/// </summary>
		/// <param name="period">is the new frequency as an Amiga period</param>
		void SetAmigaPeriod(uint period);

		/// <summary>
		/// Will update the current sample number being used
		/// </summary>
		/// <param name="sampleNumber">is the sample number being played. If unknown, set it to -1</param>
		void SetSampleNumber(short sampleNumber);

		/// <summary>
		/// Will tell visuals the correct note and octave being played. This
		/// can be used when playing multi-octave samples. If this is not
		/// called, the old behaviour is used by using the frequency the
		/// sample is played with to find the note
		/// </summary>
		/// <param name="octave">is the octave between 0 and 9</param>
		/// <param name="note">is the note in the octave between 0 and 11</param>
		void SetNote(byte octave, byte note);

		/// <summary>
		/// Returns true or false depending on the channel is in use
		/// </summary>
		bool IsActive { get; }

		/// <summary>
		/// Returns true or false depending on the channel is muted or not
		/// </summary>
		bool IsMuted { get; }

		/// <summary>
		/// Mute the channel
		/// </summary>
		void Mute();

		/// <summary>
		/// Returns the current sample number used on the channel
		/// </summary>
		short GetSampleNumber();

		/// <summary>
		/// Returns the current volume on the channel
		/// </summary>
		ushort GetVolume();

		/// <summary>
		/// Returns the current frequency on the channel
		/// </summary>
		uint GetFrequency();

		/// <summary>
		/// Returns the length of the sample in samples
		/// </summary>
		uint GetSampleLength();

		/// <summary>
		/// Returns new sample position if set
		/// </summary>
		int GetSamplePosition();
	}
}
