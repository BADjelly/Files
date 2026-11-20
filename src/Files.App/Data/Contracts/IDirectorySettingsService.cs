// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Contracts
{
	/// <summary>
	/// Manages per-directory settings stored in <c>.files-settings.json</c> files.
	/// Settings are resolved by walking up the directory tree from the target path.
	/// </summary>
	public interface IDirectorySettingsService
	{
		/// <summary>
		/// Gets the effective settings for the given path by walking up the directory tree.
		/// </summary>
		/// <param name="path">The directory path to resolve settings for.</param>
		/// <returns>The merged settings, or a default instance if no settings file is found.</returns>
		Task<DirectorySettingsDto> GetSettingsAsync(string path);

		/// <summary>
		/// Creates or updates a <c>.files-settings.json</c> file in the specified directory.
		/// </summary>
		/// <param name="directoryPath">The directory to write the settings file to.</param>
		/// <param name="settings">The settings to write.</param>
		/// <returns>True if the file was written successfully.</returns>
		Task<bool> SaveSettingsAsync(string directoryPath, DirectorySettingsDto settings);

		/// <summary>
		/// Deletes the <c>.files-settings.json</c> file from the specified directory.
		/// </summary>
		/// <param name="directoryPath">The directory containing the settings file.</param>
		/// <returns>True if the file was deleted successfully.</returns>
		Task<bool> DeleteSettingsAsync(string directoryPath);
	}
}
