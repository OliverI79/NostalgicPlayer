﻿/******************************************************************************/
/* This source, or parts thereof, may be used in any software as long the     */
/* license of NostalgicPlayer is keep. See the LICENSE file for more          */
/* information.                                                               */
/******************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Polycode.NostalgicPlayer.Agent.SampleConverter.Iff8Svx.Formats;
using Polycode.NostalgicPlayer.Kit.Bases;
using Polycode.NostalgicPlayer.Kit.Containers;
using Polycode.NostalgicPlayer.Kit.Interfaces;

// This is needed to uniquely identify this agent
[assembly: Guid("4FC34553-033E-4AB7-B2BE-93DDFF028CF6")]

namespace Polycode.NostalgicPlayer.Agent.SampleConverter.Iff8Svx
{
	/// <summary>
	/// NostalgicPlayer agent interface implementation
	/// </summary>
	public class Iff8Svx : AgentBase
	{
		private static readonly Dictionary<Format, Guid> supportedFormats = new Dictionary<Format, Guid>
		{
			{ Format.Pcm, Guid.Parse("7B0F6B4E-D2A1-4798-BA39-1EA2B16ED64A") },
			{ Format.Fibonacci, Guid.Parse("F5B00E87-1154-4E50-A2C9-BE8B83D0D3EF") }
		};

		#region IAgent implementation
		/********************************************************************/
		/// <summary>
		/// Returns the name of this agent
		/// </summary>
		/********************************************************************/
		public override string Name => Resources.IDS_IFF8SVX_NAME;



		/********************************************************************/
		/// <summary>
		/// Returns all the formats/types this agent supports
		/// </summary>
		/********************************************************************/
		public override AgentSupportInfo[] AgentInformation =>
		[
			new AgentSupportInfo(Resources.IDS_IFF8SVX_NAME_AGENT1, string.Format(Resources.IDS_IFF8SVX_DESCRIPTION, Resources.IDS_IFF8SVX_DESCRIPTION_AGENT1), supportedFormats[Format.Pcm]),
			new AgentSupportInfo(Resources.IDS_IFF8SVX_NAME_AGENT2, string.Format(Resources.IDS_IFF8SVX_DESCRIPTION, Resources.IDS_IFF8SVX_DESCRIPTION_AGENT2), supportedFormats[Format.Fibonacci])
		];



		/********************************************************************/
		/// <summary>
		/// Creates a new worker instance
		/// </summary>
		/********************************************************************/
		public override IAgentWorker CreateInstance(Guid typeId)
		{
			Format format = supportedFormats.Where(pair => pair.Value == typeId).Select(pair => pair.Key).FirstOrDefault();

			switch (format)
			{
				case Format.Pcm:
					return new PcmFormat();

				case Format.Fibonacci:
					return new FibonacciFormat();
			}

			return null;
		}
		#endregion
	}
}
