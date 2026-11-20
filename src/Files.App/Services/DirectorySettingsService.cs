// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Files.App.Services
{
	public sealed class DirectorySettingsService : IDirectorySettingsService
	{
		private const string SettingsFileName = ".files-settings.json";

		private readonly ConcurrentDictionary<string, DirectorySettingsDto> _cache = new(StringComparer.OrdinalIgnoreCase);
		private readonly ILogger<DirectorySettingsService>? _logger;

		public DirectorySettingsService(ILogger<DirectorySettingsService>? logger = null)
		{
			_logger = logger;
		}

		public async Task<DirectorySettingsDto> GetSettingsAsync(string path)
		{
			if (string.IsNullOrWhiteSpace(path))
				return new();

			string directory = path.TrimEnd('/', '\\');
			if (string.IsNullOrEmpty(directory))
				return new();

			// Walk up the directory tree, collecting settings from nearest to farthest
			var settingsStack = new Stack<DirectorySettingsDto>();
			string? current = directory;

			while (!string.IsNullOrEmpty(current))
			{
				var settingsPath = SystemIO.Path.Combine(current, SettingsFileName);
				var fileExists = SystemIO.File.Exists(settingsPath);

				if (fileExists)
				{
					if (_cache.TryGetValue(current, out var cached))
					{
						settingsStack.Push(cached);
					}
					else
					{
						try
						{
							var json = await SystemIO.File.ReadAllTextAsync(settingsPath, CancellationToken.None);
							var settings = ParseSettings(json);
							if (settings is not null)
							{
								_cache[current] = settings;
								settingsStack.Push(settings);
							}
						}
						catch (Exception ex)
						{
							_logger?.LogWarning(ex, "Failed to read settings file: {Path}", settingsPath);
						}
					}
				}

				var parent = SystemIO.Path.GetDirectoryName(current);
				if (string.IsNullOrEmpty(parent) || parent == current)
					break;

				current = parent;
			}

			// Merge: nearest settings override parent settings
			var result = new DirectorySettingsDto();
			while (settingsStack.Count > 0)
			{
				var overrideSettings = settingsStack.Pop();
				if (overrideSettings.CopyPasteBehavior != CopyPasteBehaviorType.Copy)
					result.CopyPasteBehavior = overrideSettings.CopyPasteBehavior;
			}

			return result;
		}

		private static DirectorySettingsDto? ParseSettings(string json)
		{
			if (string.IsNullOrWhiteSpace(json))
				return null;

			using var doc = JsonDocument.Parse(json);
			var root = doc.RootElement;

			var settings = new DirectorySettingsDto();

			if (root.TryGetProperty("copyPasteBehavior", out var copyPasteBehavior))
			{
				if (Enum.TryParse(copyPasteBehavior.GetString(), ignoreCase: true, out CopyPasteBehaviorType behavior))
					settings.CopyPasteBehavior = behavior;
			}

			return settings;
		}

		public async Task<bool> SaveSettingsAsync(string directoryPath, DirectorySettingsDto settings)
		{
			if (string.IsNullOrWhiteSpace(directoryPath))
				return false;

			var settingsPath = SystemIO.Path.Combine(directoryPath, SettingsFileName);

			try
			{
				var json = JsonSerializer.Serialize(settings);
				await SystemIO.File.WriteAllTextAsync(settingsPath, json, CancellationToken.None);

				// Update cache
				_cache[directoryPath] = settings;
				return true;
			}
			catch (Exception ex)
			{
				_logger?.LogError(ex, "Failed to write settings file: {Path}", settingsPath);
				return false;
			}
		}

		public async Task<bool> DeleteSettingsAsync(string directoryPath)
		{
			if (string.IsNullOrWhiteSpace(directoryPath))
				return false;

			var settingsPath = SystemIO.Path.Combine(directoryPath, SettingsFileName);

			try
			{
				if (SystemIO.File.Exists(settingsPath))
				{
					SystemIO.File.Delete(settingsPath);
				}

				_cache.TryRemove(directoryPath, out _);
				return true;
			}
			catch (Exception ex)
			{
				_logger?.LogError(ex, "Failed to delete settings file: {Path}", settingsPath);
				return false;
			}
		}
	}
}
