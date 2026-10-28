// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Dialogs;
using System.Windows.Input;

namespace Files.App.ViewModels.Settings
{
	public sealed partial class TagsViewModel : ObservableObject
	{
		private readonly IFileTagsSettingsService fileTagsSettingsService = Ioc.Default.GetRequiredService<IFileTagsSettingsService>();
		private readonly IFileTagsCategoriesSettingsService fileTagsCategoriesSettingsService = Ioc.Default.GetRequiredService<IFileTagsCategoriesSettingsService>();

		private bool isBulkOperation = true;

		private bool isDragStarting = true;

		public ObservableCollection<ListedTagViewModel> Tags { get; set; }

		public ICommand AddTagCommand { get; }

		// Categories
		private const string UncategorizedCategoryId = "00000000-0000-0000-0000-000000000000";

		private bool isCreatingNewCategory;
		public bool IsCreatingNewCategory
		{
			get => isCreatingNewCategory;
			set => SetProperty(ref isCreatingNewCategory, value);
		}

		public ObservableCollection<ListedCategoryViewModel> Categories { get; set; }

		public ICommand AddCategoryCommand { get; }
		public ICommand SaveNewCategoryCommand { get; }
		public ICommand CancelNewCategoryCommand { get; }

		public NewCategoryViewModel NewCategory = new();

		public TagsViewModel()
		{
			// Tags Commands
			AddTagCommand = new RelayCommand(DoAddNewTag);

			Tags = [];
			Tags.CollectionChanged += Tags_CollectionChanged;
			fileTagsSettingsService.FileTagList?.ForEach(tag => Tags.Add(new ListedTagViewModel(tag)));

			// Categories Commands
			AddCategoryCommand = new RelayCommand(DoAddNewCategory);
			SaveNewCategoryCommand = new RelayCommand(DoSaveNewCategory);
			CancelNewCategoryCommand = new RelayCommand(DoCancelNewCategory);

			Categories = [];
			Categories.CollectionChanged += Categories_CollectionChanged;
			fileTagsCategoriesSettingsService.FileCategoryList
				?.Where(c => c.Uid != UncategorizedCategoryId)
				.ForEach(category => Categories.Add(new ListedCategoryViewModel(category)));

			isBulkOperation = false;
		}

		private void Tags_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			if (isBulkOperation)
				return;

			// Reaordering ListView has no events, but its collection is updated twice,
			// first to remove the selected item, and second to add the item at the selected position.
			if (isDragStarting)
			{
				isDragStarting = false;
				return;
			}
			isDragStarting = true;

			fileTagsSettingsService.FileTagList = Tags.Select(tagVM => tagVM.Tag).ToList();
		}

		private void DoAddNewTag()
		{
			_ = ShowTagDialogAsync(null);
		}

		public async Task ShowTagDialogAsync(ListedTagViewModel? existingTag)
		{
			var dialog = new TagDialog
			{
				ViewModel = new TagDialogViewModel()
			};

			if (existingTag is null)
			{
				dialog.ViewModel.InitializeCreate();
			}
			else
			{
				dialog.ViewModel.InitializeEdit(existingTag);
			}

			var result = await dialog.ShowAsync();
			if (result != DialogResult.Primary)
				return;

			var vm = dialog.ViewModel;
			var newName = vm.GetResultName();
			var newColor = vm.GetResultColor();
			var newCategoryId = vm.GetResultCategoryId();

			if (existingTag is null)
			{
				// Create new tag
				var newTag = new TagViewModel(newName, newColor, Guid.NewGuid().ToString())
				{
					CategoryId = newCategoryId
				};
				var oldTags = fileTagsSettingsService.FileTagList?.ToList() ?? [];
				oldTags.Add(newTag);
				fileTagsSettingsService.FileTagList = oldTags;

				isBulkOperation = true;
				Tags.Add(new ListedTagViewModel(newTag));
				isBulkOperation = false;
			}
			else
			{
				// Edit existing tag
				fileTagsSettingsService.EditTag(existingTag.Tag.Uid, newName, newColor, newCategoryId);

				isBulkOperation = true;
				Tags.Clear();
				fileTagsSettingsService.FileTagList?.ForEach(tag => Tags.Add(new ListedTagViewModel(tag)));
				isBulkOperation = false;
			}
		}

		public void EditExistingTag(ListedTagViewModel item)
		{
			_ = ShowTagDialogAsync(item);
		}

		public void DeleteExistingTag(ListedTagViewModel item)
		{
			isBulkOperation = true;
			Tags.Remove(item);
			isBulkOperation = false;

			fileTagsSettingsService.DeleteTag(item.Tag.Uid);
		}

		// Categories
		private void Categories_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			if (isBulkOperation)
				return;

			if (isDragStarting)
			{
				isDragStarting = false;
				return;
			}
			isDragStarting = true;

			fileTagsCategoriesSettingsService.FileCategoryList = Categories.Select(categoryVM => categoryVM.Category).ToList();
		}

		private void DoAddNewCategory()
		{
			NewCategory.Reset();
			IsCreatingNewCategory = true;
		}

		private void DoSaveNewCategory()
		{
			IsCreatingNewCategory = false;

			fileTagsCategoriesSettingsService.CreateNewCategory(NewCategory.Name);

			isBulkOperation = true;
			Categories.Clear();
			fileTagsCategoriesSettingsService.FileCategoryList
				?.Where(c => c.Uid != UncategorizedCategoryId)
				.ForEach(category => Categories.Add(new ListedCategoryViewModel(category)));
			isBulkOperation = false;
		}

		private void DoCancelNewCategory()
		{
			IsCreatingNewCategory = false;
		}

		public void EditExistingCategory(ListedCategoryViewModel item, string newName)
		{
			fileTagsCategoriesSettingsService.EditCategory(item.Category.Uid, newName);

			isBulkOperation = true;
			Categories.Clear();
			fileTagsCategoriesSettingsService.FileCategoryList
				?.Where(c => c.Uid != UncategorizedCategoryId)
				.ForEach(category => Categories.Add(new ListedCategoryViewModel(category)));
			isBulkOperation = false;
		}

		public void DeleteExistingCategory(ListedCategoryViewModel item)
		{
			isBulkOperation = true;
			Categories.Remove(item);
			isBulkOperation = false;

			fileTagsCategoriesSettingsService.DeleteCategory(item.Category.Uid);
		}
	}
}
