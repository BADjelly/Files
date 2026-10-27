// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Contracts
{
	public interface IFileTagsCategoriesSettingsService : IBaseSettingsService
	{
		event EventHandler? OnSettingImportedEvent;

		event EventHandler? OnCategoriesUpdated;

		IList<CategoryViewModel> FileCategoryList { get; set; }

		CategoryViewModel? GetCategoryById(string uid);

		void CreateNewCategory(string newCategoryName);

		void EditCategory(string uid, string name);

		void DeleteCategory(string uid);

		object ExportSettings();

		bool ImportSettings(object import);
	}
}
