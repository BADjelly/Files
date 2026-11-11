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

	public VersionHistoryDialog(string itemName)
	{
		ViewModel = new VersionHistoryDialogViewModel(itemName);
		Title = string.Format(Strings.VersionHistoryDialogSubtitle.GetLocalizedResource(), itemName);
		InitializeComponent();
	}

	private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
	{
		Hide();
	}
}
