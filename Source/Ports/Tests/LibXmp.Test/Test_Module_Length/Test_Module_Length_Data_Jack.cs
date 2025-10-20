﻿/******************************************************************************/
/* This source, or parts thereof, may be used in any software as long the     */
/* license of NostalgicPlayer is keep. See the LICENSE file for more          */
/* information.                                                               */
/******************************************************************************/
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Polycode.NostalgicPlayer.Ports.LibXmp.Containers.Xmp;

namespace Polycode.NostalgicPlayer.Ports.Tests.LibXmp.Test.Test_Module_Length
{
	/// <summary>
	/// 
	/// </summary>
	public partial class Test_Module_Length
	{
		/********************************************************************/
		/// <summary>
		/// 
		/// </summary>
		/********************************************************************/
		[TestMethod]
		public void Test_Module_Length_Data_Jack()
		{
			Ports.LibXmp.LibXmp opaque = Ports.LibXmp.LibXmp.Xmp_Create_Context();
			c_int time = 0;

			c_int ret = LoadModule(Path.Combine(dataDirectory, "P"), "Data_Jack.s3m", opaque);
			Assert.AreEqual(0, ret, "Module load");

			opaque.Xmp_Get_Module_Info(out Xmp_Module_Info mi);
			opaque.Xmp_Get_Frame_Info(out Xmp_Frame_Info fi);

			Assert.AreEqual(94, mi.Mod.Len, "Module length");
			Assert.AreEqual(285000, fi.Total_Time, "Estimated time");

			opaque.Xmp_Start_Player(8000, 0);

			while (opaque.Xmp_Play_Frame() == 0)
			{
				opaque.Xmp_Get_Frame_Info(out fi);

				if (fi.Loop_Count > 0)
					break;

				time += fi.Frame_Time;
			}

			opaque.Xmp_End_Player();

			Assert.IsLessThan(5, time / 1000 - fi.Total_Time, "Elapsed time");

			opaque.Xmp_Release_Module();
			opaque.Xmp_Free_Context();
		}
	}
}
