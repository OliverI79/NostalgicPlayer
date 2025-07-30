﻿/******************************************************************************/
/* This source, or parts thereof, may be used in any software as long the     */
/* license of NostalgicPlayer is keep. See the LICENSE file for more          */
/* information.                                                               */
/******************************************************************************/
namespace Polycode.NostalgicPlayer.Ports.LibXmp.Containers.Common
{
	/// <summary>
	/// 
	/// </summary>
	internal class Scan_Data
	{
		/// <summary>
		/// Reply time in ms
		/// </summary>
		public c_int Time { get; set; }	// TODO: double
		public c_int Row { get; set; }
		public c_int Ord { get; set; }
		public c_int Num { get; set; }
	}
}
