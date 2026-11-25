// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Data.Contracts;
using Files.App.Helpers;
using Windows.ApplicationModel.DataTransfer;

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class PasteItemAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;
		private readonly IDirectorySettingsService _directorySettingsService;

		public string Label
			=> Strings.Paste.GetLocalizedResource();

		public string Description
			=> Strings.PasteItemDescription.GetLocalizedResource();

		public ActionCategory Category
			=> ActionCategory.FileSystem;

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.Paste");

		public string AutomationId
			=> "InnerNavigationToolbarPasteButton";

		public string AccessKey
			=> "V";

		public HotKey HotKey
			=> new(Keys.V, KeyModifiers.Ctrl);

		public bool IsExecutable
			=> GetIsExecutable();

		public PasteItemAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();
			_directorySettingsService = Ioc.Default.GetRequiredService<IDirectorySettingsService>();

			context.PropertyChanged += Context_PropertyChanged;
			App.AppModel.PropertyChanged += AppModel_PropertyChanged;
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			if (context.ShellPage is not { } shellPage)
				return;

			var shellViewModel = shellPage.GetRequiredShellViewModel();
			string path = shellViewModel.WorkingDirectory!;

			var settings = await _directorySettingsService.GetSettingsAsync(path);

			if (settings.CopyPasteBehavior == Data.Models.CopyPasteBehaviorType.Shortcut)
			{
				// Cut (move) operations bypass directory settings — move files normally
				var requestedOp = Clipboard.GetContent()?.RequestedOperation;
				if (requestedOp.HasValue && requestedOp.Value.HasFlag(Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move))
				{
					await UIFilesystemHelpers.PasteItemAsync(path, shellPage);
					return;
				}

				// Check if all clipboard items are already shortcuts (.lnk)
				var clipboardContent = Clipboard.GetContent();
				bool allShortcuts = clipboardContent is not null
					&& clipboardContent.Contains(StandardDataFormats.StorageItems)
					&& (await clipboardContent.GetStorageItemsAsync()).All(item => item.Name.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase));

				if (allShortcuts)
				{
					await UIFilesystemHelpers.PasteItemAsync(path, shellPage);
					return;
				}

				var confirmed = await DialogDisplayHelper.ShowDialogAsync(
					Strings.DirectoryShortcutPasteWarningTitle.GetLocalizedResource(),
					Strings.DirectoryShortcutPasteWarningMessage.GetLocalizedResource(),
					Strings.ConflictingItemsDialogPrimaryButtonText.GetLocalizedResource(),
					Strings.Cancel.GetLocalizedResource());

				if (!confirmed)
					return;

				await UIFilesystemHelpers.PasteItemAsShortcutAsync(path, shellPage);
			}
			else
			{
				await UIFilesystemHelpers.PasteItemAsync(path, shellPage);
			}
		}

		public bool GetIsExecutable()
		{
			return
				App.AppModel.IsPasteEnabled &&
				context.PageType != ContentPageTypes.Home &&
				context.PageType != ContentPageTypes.RecycleBin &&
				context.PageType != ContentPageTypes.SearchResults &&
				context.PageType != ContentPageTypes.ReleaseNotes &&
				context.PageType != ContentPageTypes.Settings;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.PageType))
				OnPropertyChanged(nameof(IsExecutable));
		}

		private void AppModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(AppModel.IsPasteEnabled))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
