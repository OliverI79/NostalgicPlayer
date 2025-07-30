﻿/******************************************************************************/
/* This source, or parts thereof, may be used in any software as long the     */
/* license of NostalgicPlayer is keep. See the LICENSE file for more          */
/* information.                                                               */
/******************************************************************************/
namespace Polycode.NostalgicPlayer.Agent.Player.SidMon10.Containers
{
	/// <summary>
	/// Holds information about a row
	/// </summary>
	internal class TrackRow
	{
		public sbyte Note { get; set; }
		public byte Instrument { get; set; }
		public byte Effect { get; set; }
		public byte EffectParam { get; set; }
		public byte Duration { get; set; }
	}
}
