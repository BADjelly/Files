// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Data.Contracts;
using Files.App.Helpers;
using Windows.ApplicationModel.DataTransfer;

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class PasteItemToSelectionAction : BaseUIAction, IAction
	{
		private readonly IContentPageContext context;
		private readonly IDirectorySettingsService _directorySettingsService;

		public string Label
			=> Strings.PasteToSelectedFolder.GetLocalizedResource();

		public string Description
			=> Strings.PasteItemToSelectionDescription.GetLocalizedResource();

		public ActionCategory Category
			=> ActionCategory.FileSystem;

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.Paste");

		public string AccessKey
			=> "V";

		public HotKey HotKey
			=> new(Keys.V, KeyModifiers.CtrlShift);

		public override bool IsExecutable
			=> GetIsExecutable();

		public PasteItemToSelectionAction()
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

			string? path = context.SelectedItem is ListedItem selectedItem
				? selectedItem.ItemPath
				: shellPage.ShellViewModel?.WorkingDirectory;
			if (path is null)
				return;

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
			if (!App.AppModel.IsPasteEnabled)
				return false;

			if (context.PageType is ContentPageTypes.Home or ContentPageTypes.RecycleBin or ContentPageTypes.SearchResults or ContentPageTypes.ReleaseNotes or ContentPageTypes.Settings)
				return false;

			if (!context.HasSelection)
				return true;

			return
				context.SelectedItem?.PrimaryItemAttribute == Windows.Storage.StorageItemTypes.Folder &&
				UIHelpers.CanShowDialog;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.PageType):
				case nameof(IContentPageContext.SelectedItem):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
		private void AppModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(AppModel.IsPasteEnabled))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
