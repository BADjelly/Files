// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Files.App.Data.Models;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System.ComponentModel;
using System.IO;
using Windows.System;

namespace Files.App.Views.Shells
{
	public sealed partial class ModernShellPage : BaseShellPage
	{
		protected override Frame ItemDisplay
			=> ItemDisplayFrame;

		private NavigationInteractionTracker _navigationInteractionTracker;

		private NavigationParams? _NavParams;
		public NavigationParams? NavParams
		{
			get => _NavParams;
			set
			{
				if (value != _NavParams)
				{
					_NavParams = value;

					if (IsLoaded)
						OnNavigationParamsChanged();
				}
			}
		}

		// Mirror properties for x:Bind (ShellViewModel is null when bindings are established)
		public bool IsSearchResults => ShellViewModel?.IsSearchResults ?? false;

		public bool IsTagSearch => ShellViewModel?.IsTagSearch ?? false;

		public ModernShellPage() : base(new CurrentInstanceViewModel())
		{
			InitializeComponent();

			ShellViewModel = new ShellViewModel(InstanceViewModel.FolderSettings);
			ShellViewModel.WorkingDirectoryModified += ViewModel_WorkingDirectoryModified;
			ShellViewModel.ItemLoadStatusChanged += FilesystemViewModel_ItemLoadStatusChanged;
			ShellViewModel.DirectoryInfoUpdated += FilesystemViewModel_DirectoryInfoUpdated;
			ShellViewModel.PageTypeUpdated += FilesystemViewModel_PageTypeUpdated;
			ShellViewModel.OnSelectionRequestedEvent += FilesystemViewModel_OnSelectionRequestedEvent;
			ShellViewModel.GitDirectoryUpdated += FilesystemViewModel_GitDirectoryUpdated;
			ShellViewModel.FocusFilterHeader += ShellViewModel_FocusFilterHeader;
			ShellViewModel.PropertyChanged += ShellViewModel_PropertyChanged;

			ToolbarViewModel.PathControlDisplayText = Strings.Home.GetLocalizedResource();
			ToolbarViewModel.RefreshWidgetsRequested += ModernShellPage_RefreshWidgetsRequested;

			ContentChanged += ModernShellPage_ContentChanged;

			_navigationInteractionTracker = new NavigationInteractionTracker(this, BackIcon, ForwardIcon);
			_navigationInteractionTracker.NavigationRequested += OverscrollNavigationRequested;
		}

		private void ShellViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(ShellViewModel.IsSearchResults) or nameof(ShellViewModel.CurrentSearchQuery) or nameof(ShellViewModel.TagChips))
			{
				UpdateSearchHeaderVisibility();

				if (TagChipsRepeater is not null && ShellViewModel is not null)
					TagChipsRepeater.ItemsSource = ShellViewModel.TagChips;
			}
		}

		private void SearchHeader_Loaded(object sender, RoutedEventArgs e)
		{
			UpdateSearchHeaderVisibility();
		}

		private void TagChipsRepeater_Loaded(object sender, RoutedEventArgs e)
		{
			if (sender is ItemsRepeater repeater && ShellViewModel is not null)
				repeater.ItemsSource = ShellViewModel.TagChips;
		}

		private void UpdateSearchHeaderVisibility()
		{
			if (ShellViewModel is null)
				return;

			bool showSearchHeader = ShellViewModel.IsSearchResults;
			bool showTagChips = ShellViewModel.IsTagSearch;

			if (SearchHeader is not null)
				SearchHeader.Visibility = showSearchHeader ? Visibility.Visible : Visibility.Collapsed;

			if (TagChipsRepeater is not null)
				TagChipsRepeater.Visibility = (showSearchHeader && showTagChips) ? Visibility.Visible : Visibility.Collapsed;
		}

		private async void ShellViewModel_FocusFilterHeader(object? sender, EventArgs e)
		{
			// Delay to ensure the UI is ready for focus
			await Task.Delay(100);
			if (FilterTextBox?.IsLoaded ?? false)
				FilterTextBox.Focus(FocusState.Programmatic);
		}

		private void ModernShellPage_RefreshWidgetsRequested(object? sender, EventArgs e)
		{
			if (ItemDisplayFrame?.Content is HomePage currentPage)
				currentPage.ViewModel.RefreshWidgetList();
		}

		private void ModernShellPage_ContentChanged(object? sender, TabBarItemParameter e)
		{
			UpdateStatusBarProperties();
			NotifyPropertyChanged(nameof(IsStatusBarVisible));
		}

		private void UpdateStatusBarProperties()
		{
			var contentPage = SlimContentPage is ColumnsLayoutPage columnsLayoutPage
				? columnsLayoutPage.ActiveColumnShellPage?.SlimContentPage
				: SlimContentPage;

			StatusBar.StatusBarViewModel = contentPage?.StatusBarViewModel;
			StatusBar.SelectedItemsPropertiesViewModel = contentPage?.SelectedItemsPropertiesViewModel;

			// The view model's own triggers can all fire before this page's content is
			// assigned (e.g. restoring a session inside an archive), so re-evaluate the
			// ZIP encoding selector once the content page is wired up
			_ = contentPage?.StatusBarViewModel.UpdateZipEncodingStateAsync();
		}

		protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
		{
			base.OnNavigatedTo(eventArgs);

			if (eventArgs.Parameter is string navPath)
				NavParams = new NavigationParams { NavPath = navPath };
			else if (eventArgs.Parameter is NavigationParams navParams)
				NavParams = navParams;
		}

		protected override void ShellPage_NavigationRequested(object sender, PathNavigationEventArgs e)
		{
			ItemDisplayFrame.Navigate(InstanceViewModel.FolderSettings.GetLayoutType(e.ItemPath), new NavigationArguments()
			{
				NavPathParam = e.ItemPath,
				AssociatedTabInstance = this
			},
			new SuppressNavigationTransitionInfo());
		}

		protected override void OnNavigationParamsChanged()
		{
			var navParams = NavParams;
			if (string.IsNullOrEmpty(navParams?.NavPath) || navParams.NavPath == "Home")
			{
				NavigateHome();
			}
			else if (navParams.NavPath == "ReleaseNotes")
			{
				NavigateToReleaseNotes();
			}
			else if (navParams.NavPath == "Settings")
			{
				NavigateToSettings(navParams.SelectItem);
			}
			else
			{
				var isTagSearch = navParams.NavPath.StartsWith("tag:");

				ItemDisplayFrame.Navigate(
					InstanceViewModel.FolderSettings.GetLayoutType(navParams.NavPath),
					new NavigationArguments()
					{
						NavPathParam = navParams.NavPath,
						SelectItems = !string.IsNullOrWhiteSpace(navParams.SelectItem) ? (string[])[navParams.SelectItem] : null,
						IsSearchResultPage = isTagSearch,
						SearchPathParam = isTagSearch ? "Home" : null,
						SearchQuery = isTagSearch ? navParams.NavPath : null,
						AssociatedTabInstance = this
					});
			}
		}

		protected override async void ViewModel_WorkingDirectoryModified(object? sender, WorkingDirectoryModifiedEventArgs e)
		{
			if (e is null || string.IsNullOrWhiteSpace(e.Path))
				return;

			if (e.IsLibrary)
				await UpdatePathUIToWorkingDirectoryAsync(null, e.Name);
			else
				await UpdatePathUIToWorkingDirectoryAsync(e.Path);
		}

		private async void ItemDisplayFrame_Navigated(object sender, NavigationEventArgs e)
		{
			ContentPage = await GetContentOrNullAsync();

			ToolbarViewModel.UpdateAdditionalActions();
			if (ItemDisplayFrame.CurrentSourcePageType == typeof(DetailsLayoutPage) ||
				ItemDisplayFrame.CurrentSourcePageType == typeof(GridLayoutPage))
			{
				// Reset DataGrid Rows that may be in "cut" command mode
				ContentPage!.ResetItemOpacity();
			}

			var parameters = (e.Parameter as NavigationArguments)!;
			var isTagSearch = parameters.NavPathParam is not null && parameters.NavPathParam.StartsWith("tag:");
			TabBarItemParameter = new()
			{
				InitialPageType = typeof(ModernShellPage),
				NavigationParameter = parameters.IsSearchResultPage && !isTagSearch ? parameters.SearchPathParam : parameters.NavPathParam
			};

			if (parameters.IsLayoutSwitch)
				FilesystemViewModel_DirectoryInfoUpdated(sender, EventArgs.Empty);

			// Update the ShellViewModel with the current working directory
			// Fixes https://github.com/files-community/Files/issues/17469
			if (parameters.IsSearchResultPage == false)
				ShellViewModel!.IsSearchResults = false;

			_navigationInteractionTracker.CanNavigateBackward = CanNavigateBackward;
			_navigationInteractionTracker.CanNavigateForward = CanNavigateForward;
		}

		private void OverscrollNavigationRequested(object? sender, OverscrollNavigationEventArgs e)
		{
			switch (e)
			{
				case OverscrollNavigationEventArgs.Forward:
					Forward_Click();
					break;

				case OverscrollNavigationEventArgs.Back:
					Back_Click();
					break;
			}
		}

		public override void Back_Click()
		{
			ToolbarViewModel.CanGoBack = false;
			if (!ItemDisplayFrame.CanGoBack)
				return;

			base.Back_Click();
		}

		public override void Forward_Click()
		{
			ToolbarViewModel.CanGoForward = false;
			if (!ItemDisplayFrame.CanGoForward)
				return;

			base.Forward_Click();
		}

		public override void Up_Click()
		{
			if (!ToolbarViewModel.CanNavigateToParent)
				return;

			ToolbarViewModel.CanNavigateToParent = false;
			if (string.IsNullOrEmpty(ShellViewModel?.WorkingDirectory))
				return;

			bool isPathRooted = string.Equals(ShellViewModel.WorkingDirectory, PathNormalization.GetPathRoot(ShellViewModel.WorkingDirectory), StringComparison.OrdinalIgnoreCase);
			if (isPathRooted)
			{
				ItemDisplayFrame.Navigate(
					typeof(HomePage),
					new NavigationArguments()
					{
						NavPathParam = "Home",
						AssociatedTabInstance = this
					},
					new SuppressNavigationTransitionInfo());
			}
			else
			{
				string parentDirectoryOfPath = ShellViewModel.WorkingDirectory.TrimEnd('\\', '/');

				var lastSlashIndex = parentDirectoryOfPath.LastIndexOf('\\');
				if (lastSlashIndex == -1)
					lastSlashIndex = parentDirectoryOfPath.LastIndexOf('/');
				if (lastSlashIndex != -1)
					parentDirectoryOfPath = ShellViewModel.WorkingDirectory.Remove(lastSlashIndex);
				if (parentDirectoryOfPath.EndsWith(':'))
					parentDirectoryOfPath += '\\';

				SelectSidebarItemFromPath();
				ItemDisplayFrame.Navigate(
					InstanceViewModel.FolderSettings.GetLayoutType(parentDirectoryOfPath),
					new NavigationArguments()
					{
						NavPathParam = parentDirectoryOfPath,
						AssociatedTabInstance = this
					},
					new SuppressNavigationTransitionInfo());
			}
		}

		public override void Dispose()
		{
			Bindings.StopTracking();
			ContentChanged -= ModernShellPage_ContentChanged;
			ToolbarViewModel.RefreshWidgetsRequested -= ModernShellPage_RefreshWidgetsRequested;
			if (ShellViewModel is not null)
				ShellViewModel.FocusFilterHeader -= ShellViewModel_FocusFilterHeader;
			ItemDisplayFrame.Navigated -= ItemDisplayFrame_Navigated;
			_navigationInteractionTracker.NavigationRequested -= OverscrollNavigationRequested;
			_navigationInteractionTracker.Dispose();

			base.Dispose();
		}

		public override void NavigateHome()
		{
			ItemDisplayFrame.Navigate(
				typeof(HomePage),
				new NavigationArguments()
				{
					NavPathParam = "Home",
					AssociatedTabInstance = this
				},
				new SuppressNavigationTransitionInfo());
		}

		public override void NavigateToReleaseNotes()
		{
			ItemDisplayFrame.Navigate(
				typeof(ReleaseNotesPage),
				new NavigationArguments()
				{
					NavPathParam = "ReleaseNotes",
					AssociatedTabInstance = this
				},
				new SuppressNavigationTransitionInfo());
		}

		public override void NavigateToSettings(string? selectItem = null)
		{
			ItemDisplayFrame.Navigate(
				typeof(SettingsPage),
				new NavigationArguments()
				{
					NavPathParam = "Settings",
					SelectItems = !string.IsNullOrWhiteSpace(selectItem) ? new[] { selectItem } : null,
					AssociatedTabInstance = this
				},
				new SuppressNavigationTransitionInfo());
		}

		public override void NavigateToPath(string? navigationPath, Type? sourcePageType, NavigationArguments? navArgs = null)
		{
			var shellViewModel = ShellViewModel!;
			shellViewModel.FilesAndFoldersFilter = null;

			if (sourcePageType is null && !string.IsNullOrEmpty(navigationPath))
				sourcePageType = InstanceViewModel.FolderSettings.GetLayoutType(navigationPath);

			if (navArgs is not null && navArgs.AssociatedTabInstance is not null)
			{
				ItemDisplayFrame.Navigate(
					sourcePageType,
					navArgs,
					new SuppressNavigationTransitionInfo());
			}
			else
			{
				if ((string.IsNullOrEmpty(navigationPath) ||
					string.IsNullOrEmpty(shellViewModel.WorkingDirectory) ||
					navigationPath.TrimEnd(Path.DirectorySeparatorChar).Equals(
						shellViewModel.WorkingDirectory.TrimEnd(Path.DirectorySeparatorChar),
						StringComparison.OrdinalIgnoreCase)) &&
					(TabBarItemParameter?.NavigationParameter is not string navArg ||
					string.IsNullOrEmpty(navArg) ||
					!navArg.StartsWith("tag:"))) // Return if already selected
				{
					if (InstanceViewModel?.FolderSettings is LayoutPreferencesManager fsModel)
						fsModel.IsLayoutModeChanging = false;

					return;
				}

				if (string.IsNullOrEmpty(navigationPath))
					return;

				ItemDisplayFrame.Navigate(
					sourcePageType,
					new NavigationArguments()
					{
						NavPathParam = navigationPath,
						AssociatedTabInstance = this
					},
					new SuppressNavigationTransitionInfo());
			}

			ToolbarViewModel.PathControlDisplayText = shellViewModel.WorkingDirectory;
		}

		private void FilterTextBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key is VirtualKey.Escape &&
				SlimContentPage is BaseGroupableLayoutPage { IsLoaded: true } svb)
				SlimContentPage.ItemManipulationModel.FocusFileList();
		}

		private async void ChipBorder_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			var border = sender as Border;
			if (border is { DataContext: TagChipViewModel chip } && ShellViewModel is not null)
			{
				var wasSelected = chip.IsSelected;
				chip.IsSelected = !chip.IsSelected;
				var blocked = ShellViewModel.UpdateTagChipSelection(chip);

				// If the view model blocked the deselection (last tag), restore the previous state and show a message
				if (blocked)
				{
					chip.IsSelected = wasSelected;

					var dialog = new ContentDialog()
					{
						Title = Strings.Search.GetLocalizedResource(),
						Content = Strings.SearchAtLeastOneTagSelected.GetLocalizedResource(),
						PrimaryButtonText = Strings.OK.GetLocalizedResource(),
					};

					if (Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
						dialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

					await dialog.TryShowAsync();
				}

				e.Handled = true;
			}
		}

		private void ChipBorder_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			if (sender is Border border)
				border.Opacity = 0.7;
		}

		private void ChipBorder_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			if (sender is Border border)
				border.Opacity = 1.0;
		}
	}
}
