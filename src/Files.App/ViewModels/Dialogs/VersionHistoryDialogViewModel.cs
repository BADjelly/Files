// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.ViewModels.Dialogs;

public sealed class VersionHistoryDialogViewModel : ObservableObject
{
	public string SubtitleText { get; }

	public IList<VersionHistoryItem> HistoryItems { get; }

	public VersionHistoryDialogViewModel(string itemName)
	{
		SubtitleText = string.Format(Strings.VersionHistoryDialogSubtitle.GetLocalizedResource(), itemName);

		HistoryItems = new List<VersionHistoryItem>()
		{
			new("2026-11-10 14:32:00", "v1.0 - Initial version", "Original file saved"),
			new("2026-11-08 09:15:42", "v0.9 - Beta release", "Major content update"),
			new("2026-11-05 18:47:11", "v0.8 - Pre-release", "Added new features"),
			new("2026-11-01 11:20:33", "v0.7 - Alpha 2", "Bug fixes and improvements"),
			new("2026-10-28 16:05:59", "v0.6 - Alpha 1", "First alpha release"),
			new("2026-10-22 10:12:45", "v0.5 - Prototype 3", "Refactored core modules"),
			new("2026-10-15 14:38:27", "v0.4 - Prototype 2", "Added data persistence"),
			new("2026-10-08 08:55:19", "v0.3 - Prototype 1", "Initial prototype build"),
			new("2026-10-01 17:42:03", "v0.2 - Concept", "Proof of concept implementation"),
			new("2026-09-25 12:00:00", "v0.1 - Planning", "Project kickoff and planning")
		};
	}
}
