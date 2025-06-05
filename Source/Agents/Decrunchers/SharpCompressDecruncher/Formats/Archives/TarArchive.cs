﻿/******************************************************************************/
/* This source, or parts thereof, may be used in any software as long the     */
/* license of NostalgicPlayer is keep. See the LICENSE file for more          */
/* information.                                                               */
/******************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using Polycode.NostalgicPlayer.Agent.Decruncher.SharpCompressDecruncher.Formats.Streams;
using Polycode.NostalgicPlayer.Kit.Exceptions;
using Polycode.NostalgicPlayer.Kit.Interfaces;
using Polycode.NostalgicPlayer.Kit.Streams;
using SharpCompress.Archives.Tar;

namespace Polycode.NostalgicPlayer.Agent.Decruncher.SharpCompressDecruncher.Formats.Archives
{
	/// <summary>
	/// Handle a Tar archive
	/// </summary>
	internal class TarArchive : IArchive
	{
		private readonly string agentName;
		private readonly SharpCompress.Archives.Tar.TarArchive archive;

		private readonly List<TarArchiveEntry> entries;

		/********************************************************************/
		/// <summary>
		/// Constructor
		/// </summary>
		/********************************************************************/
		public TarArchive(string agentName, ReaderStream archiveStream)
		{
			this.agentName = agentName;

			archive = SharpCompress.Archives.Tar.TarArchive.Open(archiveStream);

			// Make a copy of all the entries. For some reason, then the last
			// entry disappear sometimes in the original collection
			entries = archive.Entries.ToList();
		}

		#region IArchive implementation
		/********************************************************************/
		/// <summary>
		/// Open the entry with the name given
		/// </summary>
		/********************************************************************/
		public ArchiveStream OpenEntry(string entryName)
		{
			entryName = entryName.Replace('\\', '/');
			TarArchiveEntry entry = entries.FirstOrDefault(e => e.Key.Equals(entryName, StringComparison.OrdinalIgnoreCase));
			if (entry == null)
			{
				entryName = entryName.Replace('/', '\\');
				entry = archive.Entries.FirstOrDefault(e => e.Key.Equals(entryName, StringComparison.OrdinalIgnoreCase));
				if (entry == null)
					throw new DecruncherException(agentName, string.Format(Resources.IDS_SCOM_ERR_ENTRY_NOT_FOUND, entryName));
			}

			return new ArchiveEntryStream(entry, entry.OpenEntryStream());
		}



		/********************************************************************/
		/// <summary>
		/// Return all entries
		/// </summary>
		/********************************************************************/
		public IEnumerable<string> GetEntries()
		{
			foreach (TarArchiveEntry entry in entries.Where(e => !e.IsDirectory))
				yield return entry.Key;
		}
		#endregion
	}
}
