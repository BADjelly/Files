// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using System.IO;
using System.Text.Json;
using Windows.Storage;

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class SaveVersionAction : ObservableObject, IAction
	{
		private const string ManifestFileName = "manifest.json";

		private readonly IContentPageContext context;

		public string Label
			=> Strings.SaveVersion.GetLocalizedResource();

		public string Description
			=> Strings.SaveVersionDescription.GetLocalizedResource();

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

		public SaveVersionAction()
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

			var dialog = DynamicDialogFactory.GetFor_SaveVersionDialog(item.ItemNameRaw!);
			await dialog.TryShowAsync();

			if (dialog.DynamicResult != DynamicDialogResult.Primary)
				return;

			var additionalData = dialog.ViewModel.AdditionalData as SaveVersionDialogData;
			if (additionalData is null)
				return;

			await SaveVersionAsync(item, additionalData);
		}

		private static async Task SaveVersionAsync(ListedItem item, SaveVersionDialogData data)
		{
			var parentDir = Path.GetDirectoryName(item.ItemPath!)
				?? throw new InvalidOperationException("File has no parent directory.");

			var fileName = Path.GetFileName(item.ItemPath!);
			var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(item.ItemPath!);
			var sidecarDir = PathNormalization.Combine(parentDir, $"{fileName}.fileshistory");

			_ = Directory.CreateDirectory(sidecarDir);

			var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
			var extension = Path.GetExtension(item.ItemPath!);
			var checkpointName = $"{timestamp} - {fileNameWithoutExtension} - {data.Title}.copy{extension}";
			var checkpointPath = PathNormalization.Combine(sidecarDir, checkpointName);

			try
			{
				File.Copy(item.ItemPath!, checkpointPath, overwrite: true);
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, "Failed to save version for '{FilePath}'.", LogPathHelper.RedactPath(item.ItemPath!));
				await DialogDisplayHelper.ShowDialogAsync(
					Strings.SaveVersionErrorTitle.GetLocalizedResource(),
					Strings.SaveVersionErrorText.GetLocalizedResource());
				return;
			}

			await UpdateManifestAsync(sidecarDir, checkpointName, data.Title, data.Description);
		}

		private static async Task UpdateManifestAsync(string sidecarDir, string checkpointName, string title, string description)
		{
			var manifestPath = PathNormalization.Combine(sidecarDir, ManifestFileName);

			List<VersionManifestEntry> entries = [];
			if (File.Exists(manifestPath))
			{
				try
				{
					var json = await SystemIO.File.ReadAllTextAsync(manifestPath);
					entries = JsonSerializer.Deserialize(json, AppJsonSerializerContext.Default.ListVersionManifestEntry) ?? [];
				}
				catch (Exception ex)
				{
					App.Logger.LogWarning(ex, "Failed to read version manifest for '{SidecarDir}'.", LogPathHelper.RedactPath(sidecarDir));
				}
			}

			entries.Add(new VersionManifestEntry
			{
				Filename = checkpointName,
				Title = title,
				Description = description
			});

			try
			{
				var json = JsonSerializer.Serialize(entries, AppJsonSerializerContext.Default.ListVersionManifestEntry);
				await SystemIO.File.WriteAllTextAsync(manifestPath, json);
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, "Failed to write version manifest for '{SidecarDir}'.", LogPathHelper.RedactPath(sidecarDir));
			}
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

	internal sealed class SaveVersionDialogData
	{
		public string Title { get; set; } = string.Empty;
		public string Description { get; set; } = string.Empty;
	}

	public sealed class VersionManifestEntry
	{
		public string Filename { get; set; } = string.Empty;
		public string Title { get; set; } = string.Empty;
		public string Description { get; set; } = string.Empty;
	}
}
