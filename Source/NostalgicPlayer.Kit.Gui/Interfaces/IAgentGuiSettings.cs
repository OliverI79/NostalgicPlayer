﻿/******************************************************************************/
/* This source, or parts thereof, may be used in any software as long the     */
/* license of NostalgicPlayer is keep. See the LICENSE file for more          */
/* information.                                                               */
/******************************************************************************/
using Polycode.NostalgicPlayer.Kit.Interfaces;

namespace Polycode.NostalgicPlayer.Kit.Gui.Interfaces
{
	/// <summary>
	/// Derive from this interface, if you have some settings in your agent
	/// </summary>
	public interface IAgentGuiSettings : IAgentSettings
	{
		/// <summary>
		/// Return a new instance of the settings control
		/// </summary>
		ISettingsControl GetSettingsControl();

		/// <summary>
		/// Return the anchor name on the help page or null if none exists
		/// </summary>
		string HelpAnchor { get; }
	}
}
