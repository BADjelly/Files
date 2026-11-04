// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Text.Json;

namespace Files.App.Server;

internal static class StartupServices
{
	private static readonly Lazy<Task> menuInitialization = new(() => SafetyExtensions.IgnoreExceptions(ContextMenu.WarmUpQueryContextMenuAsync, Program.Logger));

	internal static async Task InitializeAsync()
	{
		// Read the shared tags directory from user_settings.json before any tags stores are accessed.
		// The user store is always local, so this is safe to read here.
		var userStore = SettingsStore.Get("user_settings.json");
		string? sharedPath = null;
		try
		{
			sharedPath = userStore.ReadString("SharedTagsPath");
		}
		catch
		{
			// Ignore errors reading the shared path; fall back to default location.
		}

		SettingsStore.SharedTagsDirectory = sharedPath;

		CatalogWatchers.Initialize();
		_ = SafetyExtensions.IgnoreExceptions(UpdateCatalog.InitializeAsync, Program.Logger);
		_ = menuInitialization.Value;
		await Task.WhenAll(
			SafetyExtensions.IgnoreExceptions(InitializationCache.InitializeAsync, Program.Logger),
			SafetyExtensions.IgnoreExceptions(TagCatalog.InitializeAsync, Program.Logger),
			SafetyExtensions.IgnoreExceptions(LocationCatalog.RecentFiles.GetAsync, Program.Logger),
			SafetyExtensions.IgnoreExceptions(LocationCatalog.RecentFolders.GetAsync, Program.Logger),
			SafetyExtensions.IgnoreExceptions(LocationCatalog.NetworkShortcuts.GetAsync, Program.Logger),
			SafetyExtensions.IgnoreExceptions(JumpListCatalog.RefreshAsync, Program.Logger),
			WarmDrivesAsync());
	}

	private static async Task WarmDrivesAsync()
	{
		var ids = await SafetyExtensions.IgnoreExceptions(DriveCatalog.GetIdsAsync, Program.Logger);
		foreach (var id in ids ?? Array.Empty<string>())
			_ = SafetyExtensions.IgnoreExceptions(() => DriveCatalog.GetAsync(id), Program.Logger);
	}

	internal static void Stop()
	{
		UpdateCatalog.Stop();
		SettingsStore.Stop();
		CatalogWatchers.Dispose();
		DriveCatalog.Stop();
	}
}
