﻿/******************************************************************************/
/* This source, or parts thereof, may be used in any software as long the     */
/* license of NostalgicPlayer is keep. See the LICENSE file for more          */
/* information.                                                               */
/******************************************************************************/
using System;
using System.Net.Http;
using System.Text;
using Krypton.Toolkit;
using Polycode.NostalgicPlayer.Kit.Gui.Components;

namespace Polycode.NostalgicPlayer.Client.GuiPlayer.NewVersionWindow
{
	/// <summary>
	/// This shows the help documentation
	/// </summary>
	public partial class NewVersionWindowForm : KryptonForm
	{
		/********************************************************************/
		/// <summary>
		/// Constructor
		/// </summary>
		/********************************************************************/
		public NewVersionWindowForm(string fromVersion, string toVersion)
		{
			InitializeComponent();

			if (!DesignMode)
			{
				// Set the title of the window
				Text = Resources.IDS_NEWVERSION_TITLE;

				// Retrieve and build the history list
				BuildHistoryList(fromVersion, toVersion);
			}
		}



		/********************************************************************/
		/// <summary>
		/// Fill the rich text box with the list of changes
		/// </summary>
		/********************************************************************/
		private void BuildHistoryList(string fromVersion, string toVersion)
		{
			string historyHtml = RetrieveAllVersions();

			historyRichTextBox.Clear();

			// Find where the history starts in the HTML
			int index = historyHtml.IndexOf("<!-- History start -->");
			if (index != -1)
			{
				// First find the list for the version that has been upgraded to
				string currentVersion;

				do
				{
					currentVersion = SearchAfterVersion(historyHtml, ref index);
					if (string.IsNullOrEmpty(currentVersion))
						return;
				}
				while (currentVersion != toVersion);

				AddHistory(historyHtml, currentVersion, ref index);

				for (;;)
				{
					currentVersion = SearchAfterVersion(historyHtml, ref index);
					if (string.IsNullOrEmpty(currentVersion) || (currentVersion == fromVersion))
						break;

					historyRichTextBox.SelectedText = "\n";
					AddHistory(historyHtml, currentVersion, ref index);
				}
			}
		}



		/********************************************************************/
		/// <summary>
		/// Retrieve all versions from the internet
		/// </summary>
		/********************************************************************/
		private string RetrieveAllVersions()
		{
			try
			{
				using (HttpClient httpClient = new HttpClient())
				{
					byte[] bytes = httpClient.GetByteArrayAsync("https://nostalgicplayer.dk/history").Result;

					return Encoding.UTF8.GetString(bytes);
				}
			}
			catch (Exception)
			{
				return string.Empty;
			}
		}



		/********************************************************************/
		/// <summary>
		/// Search after the next version and return it
		/// </summary>
		/********************************************************************/
		private string SearchAfterVersion(string historyHtml, ref int index)
		{
			int startIndex = historyHtml.IndexOf("<h1>", index);
			if (startIndex == -1)
				return null;

			int endIndex = historyHtml.IndexOf("</h1>", startIndex + 4);
			if (endIndex == -1)
				return null;

			index = endIndex + 5;
			return historyHtml.Substring(startIndex + 4, endIndex - startIndex - 4);
		}



		/********************************************************************/
		/// <summary>
		/// Add all changes for the given version
		/// </summary>
		/********************************************************************/
		private void AddHistory(string historyHtml, string version, ref int index)
		{
			historyRichTextBox.SelectionFont = FontPalette.GetRegularFont(12.0f);
			historyRichTextBox.SelectedText = version + "\n";
			historyRichTextBox.SelectionFont = FontPalette.GetRegularFont(9.0f);
			historyRichTextBox.SelectedText = "\n";

			historyRichTextBox.SelectionBullet = true;
			historyRichTextBox.SelectionIndent = 4;
			historyRichTextBox.BulletIndent = 12;

			int startList = historyHtml.IndexOf("<ul", index);
			if (startList != -1)
			{
				int endList = historyHtml.IndexOf("</ul>", startList);
				if (endList != -1)
				{
					index = startList;

					for (;;)
					{
						int bulletStart = historyHtml.IndexOf("<li>", index);
						if ((bulletStart == -1) || (bulletStart > endList))
							break;

						int bulletEnd = historyHtml.IndexOf("</li>", bulletStart + 4);
						if (bulletEnd == -1)
							break;

						string bullet = historyHtml.Substring(bulletStart + 4, bulletEnd - bulletStart - 4);
						bullet = bullet.Replace("&quot;", "\"").Replace("<i>", string.Empty).Replace("</i>", string.Empty);
						historyRichTextBox.SelectedText = bullet + "\n";

						index = bulletEnd + 5;
					}
				}
			}

			historyRichTextBox.SelectionBullet = false;
			historyRichTextBox.SelectionIndent = 0;
		}
	}
}
