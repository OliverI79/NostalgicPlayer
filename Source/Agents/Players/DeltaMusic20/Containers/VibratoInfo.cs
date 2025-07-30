﻿/******************************************************************************/
/* This source, or parts thereof, may be used in any software as long the     */
/* license of NostalgicPlayer is keep. See the LICENSE file for more          */
/* information.                                                               */
/******************************************************************************/
namespace Polycode.NostalgicPlayer.Agent.Player.DeltaMusic20.Containers
{
	/// <summary>
	/// Holds information about a single entry in the vibrato table
	/// </summary>
	internal class VibratoInfo
	{
		public byte Speed { get; set; }
		public byte Delay { get; set; }
		public byte Sustain { get; set; }
	}
}