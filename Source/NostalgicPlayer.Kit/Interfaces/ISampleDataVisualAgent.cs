﻿/******************************************************************************/
/* This source, or parts thereof, may be used in any software as long the     */
/* license of NostalgicPlayer is keep. See the LICENSE file for more          */
/* information.                                                               */
/******************************************************************************/
using Polycode.NostalgicPlayer.Kit.Containers;

namespace Polycode.NostalgicPlayer.Kit.Interfaces
{
	/// <summary>
	/// Agents of this type can act as a visual, which can show what is played.
	/// You also need to implement the IAgentGuiDisplay interface
	/// </summary>
	public interface ISampleDataVisualAgent : IVisualAgent
	{
		/// <summary>
		/// Tell the visual about new sample data
		/// </summary>
		void SampleData(NewSampleData sampleData);
	}
}
