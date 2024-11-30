﻿/******************************************************************************/
/* This source, or parts thereof, may be used in any software as long the     */
/* license of NostalgicPlayer is keep. See the LICENSE file for more          */
/* information.                                                               */
/******************************************************************************/
using System;
using System.Runtime.InteropServices;
using Polycode.NostalgicPlayer.Kit.Bases;
using Polycode.NostalgicPlayer.Kit.Containers;
using Polycode.NostalgicPlayer.Kit.Interfaces;

// This is needed to uniquely identify this agent
[assembly: Guid("E6AF0110-8DFA-4A23-A78C-9B6CB7EA8F75")]

namespace Polycode.NostalgicPlayer.Agent.Player.FutureComposer
{
	/// <summary>
	/// NostalgicPlayer agent interface implementation
	/// </summary>
	public class FutureComposer : AgentBase
	{
		private static readonly Guid agent1Id = Guid.Parse("19084A96-760A-43FA-92A2-34B9A984EDC1");

		#region IAgent implementation
		/********************************************************************/
		/// <summary>
		/// Returns the name of this agent
		/// </summary>
		/********************************************************************/
		public override string Name => Resources.IDS_FC_NAME;



		/********************************************************************/
		/// <summary>
		/// Returns a description of this agent
		/// </summary>
		/********************************************************************/
		public override string Description => Resources.IDS_FC_DESCRIPTION;



		/********************************************************************/
		/// <summary>
		/// Returns all the formats/types this agent supports
		/// </summary>
		/********************************************************************/
		public override AgentSupportInfo[] AgentInformation =>
		[
			new AgentSupportInfo(Resources.IDS_FC_NAME_AGENT1, Resources.IDS_FC_DESCRIPTION_AGENT1, agent1Id)
		];



		/********************************************************************/
		/// <summary>
		/// Creates a new worker instance
		/// </summary>
		/********************************************************************/
		public override IAgentWorker CreateInstance(Guid typeId)
		{
			return new FutureComposerWorker();
		}
		#endregion
	}
}
