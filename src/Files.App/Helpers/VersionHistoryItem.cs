// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Helpers;

public sealed class VersionHistoryItem
{
	public string Timestamp { get; }
	public string Title { get; }
	public string Description { get; }

	public VersionHistoryItem(string timestamp, string title, string description)
	{
		Timestamp = timestamp;
		Title = title;
		Description = description;
	}
}
