﻿/******************************************************************************/
/* This source, or parts thereof, may be used in any software as long the     */
/* license of NostalgicPlayer is keep. See the LICENSE file for more          */
/* information.                                                               */
/******************************************************************************/
using Polycode.NostalgicPlayer.Kit.Containers.Types;

namespace Polycode.NostalgicPlayer.Agent.Player.Hippel
{
	/// <summary>
	/// Different tables needed
	/// </summary>
	internal static class Tables
	{
		/********************************************************************/
		/// <summary>
		/// Default envelope table
		/// </summary>
		/********************************************************************/
		public static readonly byte[] DefaultCommandTable =
		[
			0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xe1
		];



		/********************************************************************/
		/// <summary>
		/// Periods 1
		/// </summary>
		/********************************************************************/
		public static readonly ushort[] Periods1 =
		[
			1712, 1616, 1524, 1440, 1356, 1280, 1208, 1140, 1076, 1016,  960,  906,
			 856,  808,  762,  720,  678,  640,  604,  570,  538,  508,  480,  453,
			 428,  404,  381,  360,  339,  320,  302,  285,  269,  254,  240,  226,
			 214,  202,  190,  180,  170,  160,  151,  143,  135,  127,  120,  113,
			 113,  113,  113,  113,  113,  113
		];



		/********************************************************************/
		/// <summary>
		/// Periods 2
		/// </summary>
		/********************************************************************/
		public static readonly ushort[] Periods2 =
		[
			1712, 1616, 1524, 1440, 1356, 1280, 1208, 1140, 1076, 1016,  960,  906,
			 856,  808,  762,  720,  678,  640,  604,  570,  538,  508,  480,  453,
			 428,  404,  381,  360,  339,  320,  302,  285,  269,  254,  240,  226,
			 214,  202,  190,  180,  170,  160,  151,  143,  135,  127,  120,  113,
			 113,  113,  113,  113,  113,  113,  113,  113,  113,  113,  113,  113,
			3424, 3232, 3048, 2880, 2712, 2560, 2416, 2280, 2152, 2032, 1920, 1812
		];



		/********************************************************************/
		/// <summary>
		/// Panning values for 4 channels modules
		/// </summary>
		/********************************************************************/
		public static readonly ChannelPanningType[] Pan4 =
		[
			ChannelPanningType.Left, ChannelPanningType.Right, ChannelPanningType.Right, ChannelPanningType.Left
		];



		/********************************************************************/
		/// <summary>
		/// Panning values for 7 channels modules
		/// </summary>
		/********************************************************************/
		public static readonly ChannelPanningType[] Pan7 =
		[
			ChannelPanningType.Right, ChannelPanningType.Right, ChannelPanningType.Right,
			ChannelPanningType.Left, ChannelPanningType.Left, ChannelPanningType.Left, ChannelPanningType.Right
		];
	}
}
