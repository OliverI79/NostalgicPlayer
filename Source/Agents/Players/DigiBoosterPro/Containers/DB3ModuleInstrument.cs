﻿/******************************************************************************/
/* This source, or parts thereof, may be used in any software as long the     */
/* license of NostalgicPlayer is keep. See the LICENSE file for more          */
/* information.                                                               */
/******************************************************************************/
namespace Polycode.NostalgicPlayer.Agent.Player.DigiBoosterPro.Containers
{
	/// <summary>
	/// An instrument
	/// </summary>
	internal class DB3ModuleInstrument
	{
		public string Name { get; set; }
		public uint16_t Volume { get; set; }			// 0 to 64 (including)
		public int16_t Panning { get; set; }			// -128 full left, +128 full right
		public InstrumentType Type { get; set; }
		public uint16_t VolumeEnvelope { get; set; }	// Index of volume envelope, 0xffff if none
		public uint16_t PanningEnvelope { get; set; }	// Index of volume envelope, 0xffff if none
	}
}
