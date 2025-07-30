﻿/******************************************************************************/
/* This source, or parts thereof, may be used in any software as long the     */
/* license of NostalgicPlayer is keep. See the LICENSE file for more          */
/* information.                                                               */
/******************************************************************************/
namespace Polycode.NostalgicPlayer.Agent.Player.DigiBoosterPro.Containers
{
	/// <summary>
	/// A single envelope point
	/// </summary>
	internal struct DB3ModuleEnvelopePoint
	{
		public uint16_t Position { get; set; }
		public int16_t Value { get; set; }
	}
}
