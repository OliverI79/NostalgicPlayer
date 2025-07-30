﻿/******************************************************************************/
/* This source, or parts thereof, may be used in any software as long the     */
/* license of NostalgicPlayer is keep. See the LICENSE file for more          */
/* information.                                                               */
/******************************************************************************/
namespace Polycode.NostalgicPlayer.Agent.Player.FutureComposer.Containers
{
	/// <summary>
	/// Sequence structure
	/// </summary>
	internal class Sequence
	{
		public VoiceSeq[] VoiceSeq { get; set; } = new VoiceSeq[4];
		public byte Speed { get; set; }
	}
}
