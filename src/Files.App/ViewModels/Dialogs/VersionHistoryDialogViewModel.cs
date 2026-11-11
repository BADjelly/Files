// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using Files.App.Utils.Serialization;

namespace Files.App.ViewModels.Dialogs;

public sealed class VersionHistoryDialogViewModel : ObservableObject
{
	public string TitleText { get; }

	public IList<VersionHistoryItem> HistoryItems { get; }

	public VersionHistoryDialogViewModel(string itemPath)
	{
		string itemName = Path.GetFileName(itemPath);
		string parentDir = Path.GetDirectoryName(itemPath) ?? string.Empty;
		string historyDir = PathNormalization.Combine(parentDir, $"{itemName}.fileshistory");
		string manifestPath = PathNormalization.Combine(historyDir, "manifest.json");

		TitleText = string.Format(Strings.VersionHistoryDialogSubtitle.GetLocalizedResource(), itemName);

		HistoryItems = LoadHistoryItems(manifestPath, historyDir);
	}

	private static IList<VersionHistoryItem> LoadHistoryItems(string manifestPath, string historyDir)
	{
		var items = new List<VersionHistoryItem>();

		try
		{
			if (!File.Exists(manifestPath))
				return items;

			string json = File.ReadAllText(manifestPath);
			var entries = JsonSerializer.Deserialize(json, AppJsonSerializerContext.Default.ListVersionHistoryEntry) ?? [];
			if (entries is null)
				return items;

			foreach (var entry in entries)
			{
				string timestamp = ExtractTimestamp(entry.Filename!);
				string title = entry.Title ?? string.Empty;
				string description = entry.Description ?? string.Empty;
				items.Add(new VersionHistoryItem(timestamp, title, description));
			}

			items.Reverse();
		}
		catch
		{
			// Return empty list on any error
		}

		return items;
	}

	private static string ExtractTimestamp(string filename)
	{
		// Filename format: "yyyyMMdd-HHmmss - name - title.copy.ext"
		Match match = Regex.Match(filename, @"^(\d{4})(\d{2})(\d{2})-(\d{2})(\d{2})(\d{2})");
		if (match.Success)
		{
			return $"{match.Groups[1].Value}-{match.Groups[2].Value}-{match.Groups[3].Value} {match.Groups[4].Value}:{match.Groups[5].Value}:{match.Groups[6].Value}";
		}

		return filename;
	}

	public sealed class VersionHistoryEntry
	{
		public string? Filename { get; set; }
		public string? Title { get; set; }
		public string? Description { get; set; }
	}
}
