﻿/******************************************************************************/
/* This source, or parts thereof, may be used in any software as long the     */
/* license of NostalgicPlayer is keep. See the LICENSE file for more          */
/* information.                                                               */
/******************************************************************************/
using System.Windows.Forms;
using Polycode.NostalgicPlayer.Agent.Visual.SpinningSquares.Display;
using Polycode.NostalgicPlayer.Kit.Containers;
using Polycode.NostalgicPlayer.Kit.Containers.Flags;
using Polycode.NostalgicPlayer.Kit.Gui.Interfaces;
using Polycode.NostalgicPlayer.Kit.Interfaces;

namespace Polycode.NostalgicPlayer.Agent.Visual.SpinningSquares
{
	/// <summary>
	/// Main worker class
	/// </summary>
	internal class SpinningSquaresWorker : IChannelChangeVisualAgent, IAgentGuiDisplay
	{
		private SpinningSquaresControl userControl;

		#region IAgentDisplay implementation
		/********************************************************************/
		/// <summary>
		/// Return some flags telling how to set up the display window
		/// </summary>
		/********************************************************************/
		public DisplayFlag Flags => DisplayFlag.None;
		#endregion

		#region IAgentGuiDisplay implementation
		/********************************************************************/
		/// <summary>
		/// Return the user control to show
		/// </summary>
		/********************************************************************/
		public UserControl GetUserControl()
		{
			userControl = new SpinningSquaresControl();
			return userControl;
		}



		/********************************************************************/
		/// <summary>
		/// Return the anchor name on the help page or null if none exists
		/// </summary>
		/********************************************************************/
		public string HelpAnchor => "spinningsquares";
		#endregion

		#region IVisualAgent implementation
		/********************************************************************/
		/// <summary>
		/// Initializes the visual
		/// </summary>
		/********************************************************************/
		public void InitVisual(int channels, int virtualChannels, SpeakerFlag speakersToShow)
		{
			userControl.InitVisual(channels);
		}



		/********************************************************************/
		/// <summary>
		/// Cleanup the visual
		/// </summary>
		/********************************************************************/
		public void CleanupVisual()
		{
			userControl.CleanupVisual();
		}



		/********************************************************************/
		/// <summary>
		/// Set the pause state
		/// </summary>
		/********************************************************************/
		public void SetPauseState(bool paused)
		{
		}
		#endregion

		#region IChannelChangeVisualAgent implementation
		/********************************************************************/
		/// <summary>
		/// Is called when initializing the visual agent. The array contains
		/// all the frequencies for each note per sample
		/// </summary>
		/********************************************************************/
		public void SetNoteFrequencies(uint[][] noteFrequencies)
		{
		}



		/********************************************************************/
		/// <summary>
		/// Tell the visual about changes of the channels
		/// </summary>
		/********************************************************************/
		public void ChannelsChanged(ChannelChanged[] channelChanged)
		{
			userControl.ChannelChange(channelChanged);
		}
		#endregion
	}
}
