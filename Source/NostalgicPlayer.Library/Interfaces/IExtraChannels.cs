﻿/******************************************************************************/
/* This source, or parts thereof, may be used in any software as long the     */
/* license of NostalgicPlayer is keep. See the LICENSE file for more          */
/* information.                                                               */
/******************************************************************************/
using Polycode.NostalgicPlayer.Kit.Interfaces;

namespace Polycode.NostalgicPlayer.Library.Interfaces
{
	/// <summary>
	/// Implement this interface if you want to add extra channels to the mixer
	/// </summary>
	public interface IExtraChannels
	{
		/// <summary>
		/// Do any needed initialization
		/// </summary>
		void Initialize();

		/// <summary>
		/// Cleanup
		/// </summary>
		void Cleanup();

		/// <summary>
		/// Play the extra channels
		/// </summary>
		bool PlayChannels(IChannel[] channels);

		/// <summary>
		/// Return the number of extra channels
		/// </summary>
		int ExtraChannels { get; }
	}
}
