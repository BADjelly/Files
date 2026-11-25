// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Win32;
using WinRT;

namespace Files.App.Dialogs;

public sealed partial class VersionHistoryDialog : ContentDialog
{
	private DateTimeOffset _lastItemClickTime;
	private DateTimeOffset _lastShowClickTime;
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

	[DynamicWindowsRuntimeCast(typeof(Button))]
	private void ShowButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
	{
		var now = DateTimeOffset.Now;
		if ((now - _lastShowClickTime).TotalMilliseconds < PInvoke.GetDoubleClickTime())
			return;

		_lastShowClickTime = now;

		if (sender is Button button && button.Tag is Helpers.VersionHistoryItem item)
		{
			ViewModel.ShowHistoryDirectory(item);
		}
	}

	[DynamicWindowsRuntimeCast(typeof(Button))]
	private async void OpenButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
	{
		if (sender is Button button && button.Tag is Helpers.VersionHistoryItem item)
		{
			await ViewModel.OpenFileAsync(item);
		}
	}

	[DynamicWindowsRuntimeCast(typeof(Button))]
	[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
	private static IEnumerable<Button> GetButtons(Grid grid)
	{
		var result = new List<Button>();
		for (var i = 0; i < VisualTreeHelper.GetChildrenCount(grid); i++)
		{
			var child = VisualTreeHelper.GetChild(grid, i);
			if (child is Button button)
				result.Add(button);
			else if (child is FrameworkElement fe)
				result.AddRange(GetButtonsRecursive(fe));
		}
		return result;
	}

	[DynamicWindowsRuntimeCast(typeof(Button))]
	[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
	private static IEnumerable<Button> GetButtonsRecursive(DependencyObject parent)
	{
		var result = new List<Button>();
		for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
		{
			var child = VisualTreeHelper.GetChild(parent, i);
			if (child is Button button)
				result.Add(button);
			else if (child is FrameworkElement fe)
				result.AddRange(GetButtonsRecursive(fe));
		}
		return result;
	}

	[DynamicWindowsRuntimeCast(typeof(Grid))]
	private void ItemGrid_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
	{
		if (sender is not Grid grid)
			return;

		var point = e.GetCurrentPoint(HistoryListView);
		if (!point.Properties.IsLeftButtonPressed)
			return;

		var position = point.Position;
		var gridBounds = grid.TransformToVisual(HistoryListView).TransformBounds(new Windows.Foundation.Rect(0, 0, grid.ActualWidth, grid.ActualHeight));

		// Check if the click is within the Grid bounds
		if (!gridBounds.Contains(position))
			return;

		// Check if the click is NOT within any button
		var buttons = GetButtons(grid);
		foreach (var button in buttons)
		{
			var buttonBounds = button.TransformToVisual(HistoryListView).TransformBounds(new Windows.Foundation.Rect(0, 0, button.ActualWidth, button.ActualHeight));
			if (buttonBounds.Contains(position))
				return;
		}

		var now = DateTimeOffset.Now;
		var timeSinceLastClick = (now - _lastItemClickTime).TotalMilliseconds;
		var isDoubleClick = timeSinceLastClick < PInvoke.GetDoubleClickTime();
		_lastItemClickTime = now;

		if (isDoubleClick && grid.DataContext is Helpers.VersionHistoryItem item)
		{
			_ = ViewModel.OpenFileAsync(item);
		}
	}
}
