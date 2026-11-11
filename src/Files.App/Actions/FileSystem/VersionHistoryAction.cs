// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Dialogs;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;

namespace Files.App.Actions;

[GeneratedRichCommand]
internal sealed partial class VersionHistoryAction : ObservableObject, IAction
{
	private readonly IContentPageContext context;

	public string Label
		=> Strings.VersionHistory.GetLocalizedResource();

	public string Description
		=> Strings.VersionHistoryDescription.GetLocalizedResource();

	public ActionCategory Category
		=> ActionCategory.FileSystem;

	public RichGlyph Glyph
		=> new(themedIconStyle: "App.ThemedIcons.Actions.Sync");

	public bool IsExecutable =>
		context.ShellPage is not null &&
		IsPageTypeValid() &&
		context.HasSelection &&
		context.SelectedItems.Count == 1 &&
		context.SelectedItems[0].PrimaryItemAttribute == StorageItemTypes.File;

	public VersionHistoryAction()
	{
		context = Ioc.Default.GetRequiredService<IContentPageContext>();

		context.PropertyChanged += Context_PropertyChanged;
	}

	public async Task ExecuteAsync(object? parameter = null)
	{
		if (context.SelectedItems.Count != 1)
			return;

		var item = context.SelectedItems[0];
		if (item.PrimaryItemAttribute != StorageItemTypes.File)
			return;

		var dialog = new VersionHistoryDialog(item.ItemNameRaw!);
		await dialog.TryShowAsync();
	}

	private bool IsPageTypeValid()
	{
		return
			context.PageType != ContentPageTypes.None &&
			context.PageType != ContentPageTypes.Home &&
			context.PageType != ContentPageTypes.RecycleBin &&
			context.PageType != ContentPageTypes.ZipFolder &&
			context.PageType != ContentPageTypes.ReleaseNotes &&
			context.PageType != ContentPageTypes.Settings;
	}

	private void Context_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(IContentPageContext.ShellPage):
			case nameof(IContentPageContext.PageType):
			case nameof(IContentPageContext.HasSelection):
			case nameof(IContentPageContext.SelectedItems):
				OnPropertyChanged(nameof(IsExecutable));
				break;
		}
	}
}
