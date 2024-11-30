﻿/******************************************************************************/
/* This source, or parts thereof, may be used in any software as long the     */
/* license of NostalgicPlayer is keep. See the LICENSE file for more          */
/* information.                                                               */
/******************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Polycode.NostalgicPlayer.Agent.SampleConverter.RiffWave.Formats;
using Polycode.NostalgicPlayer.Kit.Bases;
using Polycode.NostalgicPlayer.Kit.Containers;
using Polycode.NostalgicPlayer.Kit.Interfaces;

// This is needed to uniquely identify this agent
[assembly: Guid("AE32657A-9081-4C80-AD6D-BF8D71CA75BD")]

namespace Polycode.NostalgicPlayer.Agent.SampleConverter.RiffWave
{
	/// <summary>
	/// NostalgicPlayer agent interface implementation
	/// </summary>
	public class RiffWave : AgentBase
	{
		private static readonly Dictionary<WaveFormat, Guid> supportedFormats = new Dictionary<WaveFormat, Guid>
		{
			{ WaveFormat.WAVE_FORMAT_PCM, Guid.Parse("8E1352D0-863E-4E7F-8F43-DADC01F6558F") },
			{ WaveFormat.WAVE_FORMAT_IEEE_FLOAT, Guid.Parse("1F3B71B5-E86C-4DBC-A504-3CADB817E704") },
			{ WaveFormat.WAVE_FORMAT_ADPCM, Guid.Parse("1A5DCA0B-24F8-4D6A-A010-E06DBA96EED2") }
		};

		#region IAgent implementation
		/********************************************************************/
		/// <summary>
		/// Returns the name of this agent
		/// </summary>
		/********************************************************************/
		public override string Name => Resources.IDS_RIFFWAVE_NAME;



		/********************************************************************/
		/// <summary>
		/// Returns all the formats/types this agent supports
		/// </summary>
		/********************************************************************/
		public override AgentSupportInfo[] AgentInformation =>
		[
			new AgentSupportInfo(Resources.IDS_RIFFWAVE_NAME_AGENT1, string.Format(Resources.IDS_RIFFWAVE_DESCRIPTION, Resources.IDS_RIFFWAVE_DESCRIPTION_AGENT1), supportedFormats[WaveFormat.WAVE_FORMAT_PCM]),
			new AgentSupportInfo(Resources.IDS_RIFFWAVE_NAME_AGENT2, string.Format(Resources.IDS_RIFFWAVE_DESCRIPTION, Resources.IDS_RIFFWAVE_DESCRIPTION_AGENT2), supportedFormats[WaveFormat.WAVE_FORMAT_IEEE_FLOAT]),
			new AgentSupportInfo(Resources.IDS_RIFFWAVE_NAME_AGENT3, string.Format(Resources.IDS_RIFFWAVE_DESCRIPTION, Resources.IDS_RIFFWAVE_DESCRIPTION_AGENT3), supportedFormats[WaveFormat.WAVE_FORMAT_ADPCM])
		];



		/********************************************************************/
		/// <summary>
		/// Creates a new worker instance
		/// </summary>
		/********************************************************************/
		public override IAgentWorker CreateInstance(Guid typeId)
		{
			WaveFormat format = supportedFormats.Where(pair => pair.Value == typeId).Select(pair => pair.Key).FirstOrDefault();

			switch (format)
			{
				case WaveFormat.WAVE_FORMAT_PCM:
					return new PcmFormat();

				case WaveFormat.WAVE_FORMAT_IEEE_FLOAT:
					return new Ieee_FloatFormat();

				case WaveFormat.WAVE_FORMAT_ADPCM:
					return new AdpcmFormat();
			}

			return null;
		}
		#endregion
	}
}
