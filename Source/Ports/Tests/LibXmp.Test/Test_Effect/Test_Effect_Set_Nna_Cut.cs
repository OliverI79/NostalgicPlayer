﻿/******************************************************************************/
/* This source, or parts thereof, may be used in any software as long the     */
/* license of NostalgicPlayer is keep. See the LICENSE file for more          */
/* information.                                                               */
/******************************************************************************/
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Polycode.NostalgicPlayer.Ports.LibXmp.Containers;
using Polycode.NostalgicPlayer.Ports.LibXmp.Containers.Common;
using Polycode.NostalgicPlayer.Ports.LibXmp.Containers.Mixer;

namespace Polycode.NostalgicPlayer.Ports.Tests.LibXmp.Test.Test_Effect
{
	/// <summary>
	/// 
	/// </summary>
	public partial class Test_Effect
	{
		/********************************************************************/
		/// <summary>
		/// 
		/// </summary>
		/********************************************************************/
		[TestMethod]
		public void Test_Effect_Set_Nna_Cut()
		{
			Ports.LibXmp.LibXmp opaque = Ports.LibXmp.LibXmp.Xmp_Create_Context();
			Xmp_Context ctx = GetContext(opaque);
			Player_Data p = ctx.P;

			Create_Simple_Module(opaque, 2, 2);
			Set_Instrument_Volume(opaque, 0, 0, 22);
			Set_Instrument_Volume(opaque, 1, 0, 33);

			New_Event(opaque, 0, 0, 0, 60, 1, 44, 0x0f, 2, Effects.Fx_It_InstFunc, 0x03);
			New_Event(opaque, 0, 1, 0, 50, 2, 0, 0x00, 0, 0, 0);
			Set_Quirk(opaque, Quirk_Flag.It, Read_Event.It);

			opaque.Xmp_Start_Player(44100, 0);

			// Row 0
			opaque.Xmp_Play_Frame();

			c_int voc = Map_Channel(p, 0);
			Assert.IsGreaterThanOrEqualTo(0, voc, "Virtual map");
			Mixer_Voice vi = p.Virt.Voice_Array[voc];

			Assert.AreEqual(59, vi.Note, "Set note");
			Assert.AreEqual(0, vi.Ins, "Set instrument");
			Assert.AreEqual(43 * 16, vi.Vol, "Set volume");
			Assert.AreEqual(0, vi.Pos0, "Sample position");

			opaque.Xmp_Play_Frame();

			// Row 1: New event to test NNA
			opaque.Xmp_Play_Frame();

			Assert.AreEqual(1, vi.Ins, "Not new instrument");
			Assert.AreEqual(49, vi.Note, "Not new note");
			Assert.AreEqual(33 * 16, vi.Vol, "Not new instrument volume");
			Assert.AreEqual(0, vi.Pos0, "Sample didn't reset");

			c_int i;
			for (i = 0; i < p.Virt.MaxVoc; i++)
			{
				vi = p.Virt.Voice_Array[i];
				if ((i != 0) && (vi.Root == 0))
					break;
			}

			Assert.AreEqual(i, p.Virt.MaxVoc, "Used virtual voice");

			opaque.Xmp_Play_Frame();

			opaque.Xmp_Release_Module();
			opaque.Xmp_Free_Context();
		}
	}
}
