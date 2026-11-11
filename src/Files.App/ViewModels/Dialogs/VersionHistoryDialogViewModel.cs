// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using Files.App.Data.Contracts;
using Files.App.Data.Models;
using Files.App.Services.Settings;
using Files.App.Utils.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Windows.Storage;
using Windows.System;

namespace Files.App.ViewModels.Dialogs;

public sealed class VersionHistoryDialogViewModel : ObservableObject
{
	public string TitleText { get; }

	public IList<VersionHistoryItem> HistoryItems { get; }

	public ICommand ShowCommand { get; }

	public VersionHistoryDialogViewModel(string itemPath)
	{
		string itemName = Path.GetFileName(itemPath);
		string parentDir = Path.GetDirectoryName(itemPath) ?? string.Empty;
		string historyDir = PathNormalization.Combine(parentDir, $"{itemName}.fileshistory");
		string manifestPath = PathNormalization.Combine(historyDir, "manifest.json");

		TitleText = string.Format(Strings.VersionHistoryDialogSubtitle.GetLocalizedResource(), itemName);
		ShowCommand = new RelayCommand<VersionHistoryItem>(ShowHistoryDirectory);

		HistoryItems = LoadHistoryItems(manifestPath, historyDir);
	}

	internal void ShowHistoryDirectory(VersionHistoryItem? item)
	{
		if (item is null || string.IsNullOrWhiteSpace(item.HistoryDirPath))
			return;

		_ = Process.Start(new ProcessStartInfo
		{
			FileName = "explorer.exe",
			Arguments = item.HistoryDirPath,
			UseShellExecute = true
		});
	}

	internal async Task OpenFileAsync(VersionHistoryItem? item)
	{
		if (item is null || string.IsNullOrWhiteSpace(item.FullPath))
			return;

		try
		{
			StorageFile storageFile = await StorageFile.GetFileFromPathAsync(item.FullPath);
			_ = await Launcher.LaunchFileAsync(storageFile);
		}
		catch
		{
			// Silently fail - file may not exist anymore
		}
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

			IFileTagsSettingsService fileTagsSettingsService = Ioc.Default.GetRequiredService<IFileTagsSettingsService>();
			IEnumerable<TagViewModel> allTags = fileTagsSettingsService.FileTagList;

			foreach (var entry in entries)
			{
				string timestamp = ExtractTimestamp(entry.Filename!);
				string title = entry.Title ?? string.Empty;
				string description = entry.Description ?? string.Empty;
				string fullPath = PathNormalization.Combine(historyDir, entry.Filename!);
				string[] tagUids = FileTagsHelper.ReadFileTag(fullPath);
				IReadOnlyList<TagViewModel> tags = ResolveTags(tagUids, allTags);
				items.Add(new VersionHistoryItem(timestamp, title, description, historyDir, entry.Filename!) { Tags = tags });
			}

			items.Reverse();
		}
		catch
		{
			// Return empty list on any error
		}

		return items;
	}

	private static IReadOnlyList<TagViewModel> ResolveTags(string[] tagUids, IEnumerable<TagViewModel>? allTags)
	{
		if (tagUids.Length == 0 || allTags is null)
			return [];

		var result = new List<TagViewModel>();
		var uidSet = new HashSet<string>(tagUids);
		foreach (var tag in allTags)
		{
			if (uidSet.Contains(tag.Uid))
				result.Add(tag);
		}
		return result;
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
