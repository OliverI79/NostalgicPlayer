﻿/******************************************************************************/
/* This source, or parts thereof, may be used in any software as long the     */
/* license of NostalgicPlayer is keep. See the LICENSE file for more          */
/* information.                                                               */
/******************************************************************************/
using System.Windows.Forms;
using Polycode.NostalgicPlayer.Agent.Visual.Oscilloscope.Display;
using Polycode.NostalgicPlayer.GuiKit.Interfaces;
using Polycode.NostalgicPlayer.Kit.Containers;
using Polycode.NostalgicPlayer.Kit.Containers.Flags;
using Polycode.NostalgicPlayer.Kit.Interfaces;

namespace Polycode.NostalgicPlayer.Agent.Visual.Oscilloscope
{
	/// <summary>
	/// Main worker class
	/// </summary>
	internal class OscilloscopeWorker : ISampleDataVisualAgent, IAgentGuiDisplay
	{
		private OscilloscopeControl userControl;

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
			userControl = new OscilloscopeControl();
			return userControl;
		}



		/********************************************************************/
		/// <summary>
		/// Return the anchor name on the help page or null if none exists
		/// </summary>
		/********************************************************************/
		public string HelpAnchor => "oscilloscope";
		#endregion

		#region IVisualAgent implementation
		/********************************************************************/
		/// <summary>
		/// Initializes the visual
		/// </summary>
		/********************************************************************/
		public void InitVisual(int channels, int virtualChannels, SpeakerFlag speakersToShow)
		{
			userControl.InitVisual(speakersToShow);
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
			if (paused)
				userControl.ClearSample();
		}
		#endregion

		#region ISampleDataVisualAgent implementation
		/********************************************************************/
		/// <summary>
		/// Tell the visual about new sample data
		/// </summary>
		/********************************************************************/
		public void SampleData(NewSampleData sampleData)
		{
			userControl.SampleData(sampleData);
		}
		#endregion
	}
}
