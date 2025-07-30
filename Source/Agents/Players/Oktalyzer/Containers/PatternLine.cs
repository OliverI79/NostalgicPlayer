﻿/******************************************************************************/
/* This source, or parts thereof, may be used in any software as long the     */
/* license of NostalgicPlayer is keep. See the LICENSE file for more          */
/* information.                                                               */
/******************************************************************************/
namespace Polycode.NostalgicPlayer.Agent.Player.Oktalyzer.Containers
{
	/// <summary>
	/// PatternLine structure
	/// </summary>
	internal class PatternLine
	{
		public byte Note { get; set; }
		public byte SampleNum { get; set; }
		public byte Effect { get; set; }
		public byte EffectArg { get; set; }
	}
}
