// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinRT;

namespace Files.App.Dialogs
{
	public sealed partial class TagDialog : ContentDialog, IDialog<TagDialogViewModel>
	{
		private FrameworkElement RootAppElement
		{
			[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
			get => (FrameworkElement)MainWindow.Instance.Content;
		}

		public TagDialogViewModel ViewModel
		{
			get => (TagDialogViewModel)DataContext;
			set => DataContext = value;
		}

		public new string PrimaryButtonText
		{
			get => ViewModel.IsEditMode ? "Save" : "Create";
		}

		public TagDialog()
		{
			InitializeComponent();
		}

		public new async Task<DialogResult> ShowAsync()
		{
			// Set title based on mode
			Title = ViewModel.IsEditMode
				? "EditTag".GetLocalizedResource()
				: "CreateNewTag".GetLocalizedResource();

			// Set XamlRoot so ContentDialog can display (matches UIHelpers.SetContentDialogRoot pattern)
			if (Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
			{
				XamlRoot = MainWindow.Instance.Content.XamlRoot;
			}

			return (DialogResult)await base.ShowAsync();
		}

		private void ColorButton_Click(object sender, RoutedEventArgs e)
		{
			var flyout = new Flyout();
			var colorPicker = new CommunityToolkit.WinUI.Controls.ColorPicker
			{
				Color = ColorHelpers.FromHex(ViewModel.Color)
			};
			colorPicker.ColorChanged += (s, args) =>
			{
				ViewModel.Color = $"#{colorPicker.Color.A:X2}{colorPicker.Color.R:X2}{colorPicker.Color.G:X2}{colorPicker.Color.B:X2}";
			};
			flyout.Content = colorPicker;
			flyout.ShowAt(this);
		}
	}
}
