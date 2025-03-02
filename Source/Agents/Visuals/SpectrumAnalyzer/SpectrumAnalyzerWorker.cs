﻿/******************************************************************************/
/* This source, or parts thereof, may be used in any software as long the     */
/* license of NostalgicPlayer is keep. See the LICENSE file for more          */
/* information.                                                               */
/******************************************************************************/
using System.Windows.Forms;
using NAudio.Dsp;
using Polycode.NostalgicPlayer.Agent.Visual.SpectrumAnalyzer.Display;
using Polycode.NostalgicPlayer.GuiKit.Interfaces;
using Polycode.NostalgicPlayer.Kit.Containers;
using Polycode.NostalgicPlayer.Kit.Containers.Flags;
using Polycode.NostalgicPlayer.Kit.Interfaces;

namespace Polycode.NostalgicPlayer.Agent.Visual.SpectrumAnalyzer
{
	/// <summary>
	/// Main worker class
	/// </summary>
	internal class SpectrumAnalyzerWorker : ISampleDataVisualAgent, IAgentGuiDisplay
	{
		private const int FftLength = 2048;		// ~20 ms

		private Analyzer analyzer;

		private SpectrumAnalyzerControl userControl;

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
			userControl = new SpectrumAnalyzerControl();
			return userControl;
		}



		/********************************************************************/
		/// <summary>
		/// Return the anchor name on the help page or null if none exists
		/// </summary>
		/********************************************************************/
		public string HelpAnchor => "spectrumanalyzer";
		#endregion

		#region IVisualAgent implementation
		/********************************************************************/
		/// <summary>
		/// Initializes the visual
		/// </summary>
		/********************************************************************/
		public void InitVisual(int channels, int virtualChannels)
		{
			analyzer = new Analyzer(FftLength);
			analyzer.FftCalculated += Analyzer_FftCalculated;
		}



		/********************************************************************/
		/// <summary>
		/// Cleanup the visual
		/// </summary>
		/********************************************************************/
		public void CleanupVisual()
		{
			userControl.Update(null);
			analyzer = null;
		}



		/********************************************************************/
		/// <summary>
		/// Set the pause state
		/// </summary>
		/********************************************************************/
		public void SetPauseState(bool paused)
		{
			if (paused)
				userControl.Update(new Complex[FftLength]);
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
			analyzer?.AddValues(sampleData.SampleData, sampleData.ChannelCount);
		}
		#endregion

		#region Private methods
		/********************************************************************/
		/// <summary>
		/// Update the window with the FFT values
		/// </summary>
		/********************************************************************/
		private void Analyzer_FftCalculated(object sender, Analyzer.FftEventArgs e)
		{
			userControl.Update(e.Result);
		}
		#endregion
	}
}
