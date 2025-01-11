﻿/******************************************************************************/
/* This source, or parts thereof, may be used in any software as long the     */
/* license of NostalgicPlayer is keep. See the LICENSE file for more          */
/* information.                                                               */
/******************************************************************************/
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Polycode.NostalgicPlayer.Ports.LibXmp;

namespace Polycode.NostalgicPlayer.Ports.Tests.LibXmp.Test.Test_Read_Mem
{
	/// <summary>
	/// 
	/// </summary>
	public partial class Test_Read_Mem
	{
		/********************************************************************/
		/// <summary>
		/// 
		/// </summary>
		/********************************************************************/
		[TestMethod]
		public void Test_Read_Mem_32Bit_Little_Endian()
		{
			uint8[] mem = [ 1, 2, 3, 4 ];

			uint32 x = DataIo.ReadMem32L(mem);
			Assert.AreEqual(0x04030201U, x, "Read error");
		}
	}
}
