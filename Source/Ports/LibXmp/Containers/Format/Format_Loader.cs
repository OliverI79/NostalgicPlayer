﻿/******************************************************************************/
/* This source, or parts thereof, may be used in any software as long the     */
/* license of NostalgicPlayer is keep. See the LICENSE file for more          */
/* information.                                                               */
/******************************************************************************/
using System;
using Polycode.NostalgicPlayer.Ports.LibXmp.Loaders;

namespace Polycode.NostalgicPlayer.Ports.LibXmp.Containers.Format
{
	/// <summary>
	/// 
	/// </summary>
	internal class Format_Loader
	{
		public delegate IFormatLoader Create_Delegate(LibXmp libXmp, Xmp_Context ctx);

		public Guid Id { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public Create_Delegate Create { get; set; }
		public bool OnlyAvailableInTest { get; set; }
	}
}
