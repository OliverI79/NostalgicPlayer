﻿/******************************************************************************/
/* This source, or parts thereof, may be used in any software as long the     */
/* license of NostalgicPlayer is keep. See the LICENSE file for more          */
/* information.                                                               */
/******************************************************************************/
using System;

namespace Polycode.NostalgicPlayer.Kit.Containers.Flags
{
	/// <summary>
	/// Indicate the format of the sample and how to play it
	/// </summary>
	[Flags]
	public enum PlaySampleFlag
	{
		/// <summary>
		/// No flags
		/// </summary>
		None = 0,

		/// <summary>
		/// Sample is in 16-bit
		/// </summary>
		_16Bit = 0x0001,

		/// <summary>
		/// Sample is in interleaved stereo
		/// </summary>
		Stereo = 0x0002,

		/// <summary>
		/// Play the sample backwards
		/// </summary>
		Backwards = 0x1000
	}
}
