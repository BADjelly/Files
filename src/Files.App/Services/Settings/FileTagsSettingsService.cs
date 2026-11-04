// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Utils.Serialization.Implementation;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.IO;
using Windows.Storage;

namespace Files.App.Services.Settings
{
	[JsonSourceGenerationOptions(WriteIndented = true)]
	[JsonSerializable(typeof(object))]
	[JsonSerializable(typeof(ConcurrentDictionary<string, JsonElement>))]
	[JsonSerializable(typeof(List<TagViewModel>))]
	[JsonSerializable(typeof(IList<TagViewModel>))]
	[JsonSerializable(typeof(Dictionary<string, IList<TagViewModel>>))]
	internal sealed partial class FileTagsSettingsJsonSerializationContext : JsonSerializerContext
	{
	}

	internal sealed class FileTagsSettingsService : BaseJsonSettings, IFileTagsSettingsService
	{
		public event EventHandler? OnSettingImportedEvent;

		public event EventHandler? OnTagsUpdated;

		private const string UncategorizedCategoryId = "00000000-0000-0000-0000-000000000000";

		private static readonly List<TagViewModel> DefaultFileTags =
		[
			new("Home", "#0072BD", "f7e0e137-2eb5-4fa4-a50d-ddd65df17c34", UncategorizedCategoryId),
			new("Work", "#D95319", "c84a8131-c4de-47d9-9440-26e859d14b3d", UncategorizedCategoryId),
			new("Photos", "#EDB120", "d4b8d4bd-ceaf-4e58-ac61-a185fcf96c5d", UncategorizedCategoryId),
			new("Important", "#77AC30", "79376daf-c44a-4fe4-aa3b-8b30baea453e", UncategorizedCategoryId),
			new("Example Tag", "#8B5CF6", "b2e4f8a1-3c7d-4e9f-b0a5-1d2c3e4f5a6b", "a1b2c3d4-e5f6-7890-abcd-ef1234567890")
		];

		public FileTagsSettingsService()
		{
			JsonSettingsSerializer = new DefaultJsonSettingsSerializer();
			var database = new ServerSettingsDatabase(Constants.LocalSettings.FileTagSettingsFileName, JsonSettingsSerializer, FileTagsSettingsJsonSerializationContext.Default);
			JsonSettingsDatabase = database;
			IsAvailable = true;
			database.Changed += (name, value) =>
			{
				RaiseOnSettingChangedEvent(this, new SettingChangedEventArgs(name, value));
				if (name == nameof(FileTagList))
					OnTagsUpdated?.Invoke(this, EventArgs.Empty);
			};
		}

		public IList<TagViewModel> FileTagList
		{
			get
			{
				var tags = Get<List<TagViewModel>>(DefaultFileTags) ?? DefaultFileTags;

				foreach (var tag in tags)
					tag.Color = ColorHelpers.FromHex(tag.Color).ToString();

				return tags;
			}
			set
			{
				Set(value);
				OnTagsUpdated?.Invoke(this, EventArgs.Empty);
			}
		}

		public TagViewModel? GetTagById(string uid)
		{
			var tags = FileTagList;
			var repair = tags.Any(x => x.Uid is null);
			if (repair)
			{
				App.Logger.LogWarning("Tags file is invalid, regenerate");
				tags = DefaultFileTags;
			}

			var tag = tags.SingleOrDefault(x => x.Uid == uid);
			TagViewModel? discoveredTag = null;

			if (!string.IsNullOrEmpty(uid) && tag is null)
			{
				tag = new TagViewModel(Strings.Unknown.GetLocalizedResource(), "#9ea3a1", uid);
				discoveredTag = tag;
			}

			// This lookup is called by FileTagsUI bindings; persistence must wait until they return.
			if (repair || discoveredTag is not null)
			{
				App.UiDispatcher?.TryEnqueue(() =>
				{
					var current = FileTagList;
					if (current.Any(x => x.Uid is null))
						current = DefaultFileTags;
					else if (discoveredTag is null || current.Any(x => x.Uid == uid))
						return;
					if (discoveredTag is not null && current.All(x => x.Uid != uid))
						current = current.Append(discoveredTag).ToList();
					FileTagList = current;
				});
			}

			return tag;
		}

		public IList<TagViewModel>? GetTagsByIds(string[]? uids)
		{
			return uids is null || uids.Length == 0
				? null
				: uids.Select(GetTagById).WhereNotNull().ToList();
		}

		public IEnumerable<TagViewModel> GetTagsByName(string tagName)
		{
			return FileTagList.Where(x => x.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase));
		}

		public void CreateNewTag(string newTagName, string color, string categoryId)
		{
			var newTag = new TagViewModel(
				newTagName,
				color,
				Guid.NewGuid().ToString())
			{
				CategoryId = categoryId
			};

			var oldTags = FileTagList.ToList();
			oldTags.Add(newTag);
			FileTagList = oldTags;
		}

		public void EditTag(string uid, string name, string color, string categoryId)
		{
			var index = GetTagIndex(uid);
			if (index == -1)
				return;

			var oldTags = FileTagList.ToList();
			oldTags.RemoveAt(index);
			var editedTag = new TagViewModel(name, color, uid)
			{
				CategoryId = categoryId
			};
			oldTags.Insert(index, editedTag);
			FileTagList = oldTags;
		}

		public void DeleteTag(string uid)
		{
			var index = GetTagIndex(uid);
			if (index == -1)
				return;

			var oldTags = FileTagList.ToList();
			oldTags.RemoveAt(index);
			FileTagList = oldTags;
			_ = SafetyExtensions.IgnoreExceptions(() => UntagAllFilesAsync(uid), App.Logger);
		}

		public override bool ImportSettings(object import)
		{
			if (import is string importString)
			{
				var settings = GetJsonSettingsSerializer().DeserializeFromJson(importString, FileTagsSettingsJsonSerializationContext.Default.DictionaryStringIListTagViewModel);
				if (settings is not null && settings.TryGetValue("FileTagList", out var importedTags))
					FileTagList = importedTags;
			}
			else if (import is IList<TagViewModel> importList)
			{
				FileTagList = importList;
			}

			FileTagList ??= DefaultFileTags;

			if (FileTagList is not null)
			{
				if (!FlushSettings())
					return false;
				OnSettingImportedEvent?.Invoke(this, EventArgs.Empty);
				return true;
			}

			return false;
		}

		public override object ExportSettings()
		{
			var settings = new Dictionary<string, IList<TagViewModel>>
			{
				{ "FileTagList", FileTagList }
			};

			// Serialize settings to JSON format
			return GetJsonSettingsSerializer().SerializeToJson(settings, FileTagsSettingsJsonSerializationContext.Default.DictionaryStringIListTagViewModel);
		}

		private int GetTagIndex(string uid)
		{
			for (int i = 0; i < FileTagList.Count; i++)
			{
				if (FileTagList[i].Uid == uid)
					return i;
			}

			return -1;
		}

		private async Task UntagAllFilesAsync(string uid)
		{
			var tagDoDelete = new string[] { uid };

			foreach (var item in await FileTagsHelper.GetDbInstance().GetAllAsync())
			{
				if (item.Tags.Contains(uid))
				{
					_ = FileTagsHelper.WriteFileTagAsync(
						item.FilePath,
						item.Tags.Except(tagDoDelete).ToArray());
				}
			}
		}

		private IJsonSettingsSerializer GetJsonSettingsSerializer()
		{
			return JsonSettingsSerializer
				?? throw new InvalidOperationException("The JSON settings serializer has not been initialized.");
		}
	}
}
