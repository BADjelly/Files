// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Data.Models;
using Microsoft.UI.Xaml.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Files.App.Helpers;

public sealed partial class VersionHistoryItem : ObservableObject
{
	public string Timestamp { get; }
	public string Title { get; }
	public string Description { get; }
	public string HistoryDirPath { get; }
	public string Filename { get; }
	public IReadOnlyList<TagViewModel> Tags { get; internal set; }

	private BitmapImage? icon;
	public BitmapImage? Icon
	{
		get => icon;
		set => SetProperty(ref icon, value);
	}

	public VersionHistoryItem(string timestamp, string title, string description, string historyDirPath, string filename)
	{
		Timestamp = timestamp;
		Title = title;
		Description = description;
		HistoryDirPath = historyDirPath;
		Filename = filename;
		Tags = []; // Will be populated by the ViewModel
	}

	public string FullPath => System.IO.Path.Combine(HistoryDirPath, Filename);
}
