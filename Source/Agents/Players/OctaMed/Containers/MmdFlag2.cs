﻿/******************************************************************************/
/* This source, or parts thereof, may be used in any software as long the     */
/* license of NostalgicPlayer is keep. See the LICENSE file for more          */
/* information.                                                               */
/******************************************************************************/
using System;

namespace Polycode.NostalgicPlayer.Agent.Player.OctaMed.Containers
{
	/// <summary>
	/// Song flags
	/// </summary>
	[Flags]
	internal enum MmdFlag2
	{
		BMask = 0x1f,
		Bpm = 0x20,
		Mix = 0x80					// Uses mixing (V7+)
	}
}