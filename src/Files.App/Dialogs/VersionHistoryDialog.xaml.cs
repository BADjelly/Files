// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinRT;

namespace Files.App.Dialogs;

public sealed partial class VersionHistoryDialog : ContentDialog
{
	private FrameworkElement RootAppElement
	{
		[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
		get => (FrameworkElement)MainWindow.Instance.Content;
	}

	public VersionHistoryDialogViewModel ViewModel
	{
		get => (VersionHistoryDialogViewModel)DataContext;
		set => DataContext = value;
	}

	public VersionHistoryDialog(string itemPath)
	{
		ViewModel = new VersionHistoryDialogViewModel(itemPath);
		Title = ViewModel.TitleText;
		InitializeComponent();
	}

	private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
	{
		Hide();
	}

	private void ShowButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
	{
		if (sender is Button button && button.Tag is Helpers.VersionHistoryItem item)
		{
			ViewModel.ShowHistoryDirectory(item);
		}
	}

	private async void OpenButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
	{
		if (sender is Button button && button.Tag is Helpers.VersionHistoryItem item)
		{
			await ViewModel.OpenFileAsync(item);
		}
	}
}
