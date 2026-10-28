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
	[JsonSerializable(typeof(List<CategoryViewModel>))]
	[JsonSerializable(typeof(IList<CategoryViewModel>))]
	[JsonSerializable(typeof(Dictionary<string, IList<CategoryViewModel>>))]
	internal sealed partial class FileTagsCategoriesSettingsJsonSerializationContext : JsonSerializerContext
	{
	}

	internal sealed class FileTagsCategoriesSettingsService : BaseJsonSettings, IFileTagsCategoriesSettingsService
	{
		public event EventHandler? OnSettingImportedEvent;

		public event EventHandler? OnCategoriesUpdated;

		private static readonly List<CategoryViewModel> DefaultFileCategories =
		[
			new("Uncategorized", "00000000-0000-0000-0000-000000000000")
		];

		public FileTagsCategoriesSettingsService()
		{
			var settingsSerializer = new DefaultSettingsSerializer();
			SettingsSerializer = settingsSerializer;

			Initialize(Path.Combine(ApplicationData.Current.LocalFolder.Path,
				Constants.LocalSettings.SettingsFolderName, Constants.LocalSettings.FileTagsCategoriesSettingsFileName));

			var jsonSettingsSerializer = new DefaultJsonSettingsSerializer();
			JsonSettingsSerializer = jsonSettingsSerializer;
			JsonSettingsDatabase = new CachingJsonSettingsDatabase(
				settingsSerializer,
				jsonSettingsSerializer,
				FileTagsCategoriesSettingsJsonSerializationContext.Default);
		}

		public IList<CategoryViewModel> FileCategoryList
		{
			get
			{
				var categories = Get<List<CategoryViewModel>>(DefaultFileCategories) ?? DefaultFileCategories;

				return categories;
			}
			set
			{
				Set(value);
				OnCategoriesUpdated?.Invoke(this, EventArgs.Empty);
			}
		}

		public CategoryViewModel? GetCategoryById(string uid)
		{
			if (FileCategoryList.Any(x => x.Uid is null))
			{
				App.Logger.LogWarning("Categories file is invalid, regenerate");
				FileCategoryList = DefaultFileCategories;
			}

			return FileCategoryList.SingleOrDefault(x => x.Uid == uid);
		}

		public void CreateNewCategory(string newCategoryName)
		{
			var newCategory = new CategoryViewModel(
				newCategoryName,
				Guid.NewGuid().ToString());

			var oldCategories = FileCategoryList.ToList();
			oldCategories.Add(newCategory);
			FileCategoryList = oldCategories;
		}

		public void EditCategory(string uid, string name)
		{
			var index = GetCategoryIndex(uid);
			if (index == -1)
				return;

			var oldCategories = FileCategoryList.ToList();
			oldCategories.RemoveAt(index);
			oldCategories.Insert(index, new CategoryViewModel(name, uid));
			FileCategoryList = oldCategories;
		}

		public void DeleteCategory(string uid)
		{
			var index = GetCategoryIndex(uid);
			if (index == -1)
				return;

			var oldCategories = FileCategoryList.ToList();
			oldCategories.RemoveAt(index);
			FileCategoryList = oldCategories;

			// Clean up expansion state for the deleted category
			var general = Ioc.Default.GetRequiredService<IGeneralSettingsService>();
			if (general.FileTagsCategoryExpansionStates is { } dict && dict.Remove(uid))
			{
				general.FileTagsCategoryExpansionStates = dict.Count == 0 ? null : new Dictionary<string, bool>(dict, StringComparer.OrdinalIgnoreCase);
			}
		}

		public override bool ImportSettings(object import)
		{
			if (import is string importString)
			{
				var settings = GetJsonSettingsSerializer().DeserializeFromJson(importString, FileTagsCategoriesSettingsJsonSerializationContext.Default.DictionaryStringIListCategoryViewModel);
				if (settings is not null && settings.TryGetValue("FileCategoryList", out var importedCategories))
					FileCategoryList = importedCategories;
			}
			else if (import is IList<CategoryViewModel> importList)
			{
				FileCategoryList = importList;
			}

			FileCategoryList ??= DefaultFileCategories;

			if (FileCategoryList is not null)
			{
				FlushSettings();
				OnSettingImportedEvent?.Invoke(this, EventArgs.Empty);
				return true;
			}

			return false;
		}

		public override object ExportSettings()
		{
			var settings = new Dictionary<string, IList<CategoryViewModel>>
			{
				{ "FileCategoryList", FileCategoryList }
			};

			// Serialize settings to JSON format
			return GetJsonSettingsSerializer().SerializeToJson(settings, FileTagsCategoriesSettingsJsonSerializationContext.Default.DictionaryStringIListCategoryViewModel);
		}

		private int GetCategoryIndex(string uid)
		{
			for (int i = 0; i < FileCategoryList.Count; i++)
			{
				if (FileCategoryList[i].Uid == uid)
					return i;
			}

			return -1;
		}

		private IJsonSettingsSerializer GetJsonSettingsSerializer()
		{
			return JsonSettingsSerializer
				?? throw new InvalidOperationException("The JSON settings serializer has not been initialized.");
		}
	}
}
