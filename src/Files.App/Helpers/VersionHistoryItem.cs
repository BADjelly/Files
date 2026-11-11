// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Helpers;

public sealed class VersionHistoryItem
{
	public string Timestamp { get; }
	public string Title { get; }
	public string Description { get; }
	public string HistoryDirPath { get; }
	public string Filename { get; }

	public VersionHistoryItem(string timestamp, string title, string description, string historyDirPath, string filename)
	{
		Timestamp = timestamp;
		Title = title;
		Description = description;
		HistoryDirPath = historyDirPath;
		Filename = filename;
	}

	public string FullPath => System.IO.Path.Combine(HistoryDirPath, Filename);
}
