// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Models
{
	/// <summary>
	/// Directory-level settings stored in a <c>.files-settings.json</c> file.
	/// </summary>
	public sealed class DirectorySettingsDto
	{
		/// <summary>
		/// How paste operations behave in this directory hierarchy.
		/// </summary>
		public CopyPasteBehaviorType CopyPasteBehavior { get; set; } = CopyPasteBehaviorType.Copy;
	}

	/// <summary>
	/// Defines how paste operations behave in a directory with settings.
	/// </summary>
	public enum CopyPasteBehaviorType
	{
		/// <summary>
		/// Duplicate files on paste (default).
		/// </summary>
		Copy,

		/// <summary>
		/// Create a .lnk shortcut on paste instead of duplicating the file.
		/// </summary>
		Shortcut
	}
}
