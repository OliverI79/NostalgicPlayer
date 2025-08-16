﻿/******************************************************************************/
/* This source, or parts thereof, may be used in any software as long the     */
/* license of NostalgicPlayer is keep. See the LICENSE file for more          */
/* information.                                                               */
/******************************************************************************/
namespace Polycode.NostalgicPlayer.Kit.Containers.Types
{
	/// <summary>
	/// This enum only holds some points in the panning
	/// </summary>
	public enum ChannelPanningType : ushort
	{
		/// <summary></summary>
		Left = 0,

		/// <summary></summary>
		Center = 128,

		/// <summary></summary>
		Right = 256,

		/// <summary>
		/// Panning value for Dolby Surround
		/// </summary>
		Surround = 512
	}
}
