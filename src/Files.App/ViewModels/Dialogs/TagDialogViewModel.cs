// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Data.Models;
using Files.App.Services.Settings;
using System.Windows.Input;

namespace Files.App.ViewModels.Dialogs
{
	public sealed partial class TagDialogViewModel : ObservableObject
	{
		private readonly IFileTagsSettingsService _fileTagsSettingsService;
		private readonly IFileTagsCategoriesSettingsService _fileTagsCategoriesSettingsService;

		private readonly IList<CategoryViewModel> _allCategories;

		private bool _isEditMode;
		public bool IsEditMode
		{
			get => _isEditMode;
			private set => SetProperty(ref _isEditMode, value);
		}

		private string _title = string.Empty;
		public string Title
		{
			get => _title;
			set => SetProperty(ref _title, value);
		}

		private string _name = string.Empty;
		public string Name
		{
			get => _name;
			set
			{
				if (!SetProperty(ref _name, value))
					return;

				ValidateName();
				OnPropertyChanged(nameof(CanCommit));
			}
		}

		private void ValidateName()
		{
			if (string.IsNullOrWhiteSpace(Name))
			{
				_isNameValid = false;
				return;
			}

			var existingTags = _fileTagsSettingsService.FileTagList ?? [];
			if (!_isEditMode)
			{
				_isNameValid = !existingTags.Any(t => t.Name == Name);
			}
			else
			{
				_isNameValid = !existingTags.Any(t => t.Name == Name && t.Uid != EditingTagUid);
			}
		}

		private string? EditingTagUid
		{
			get
			{
				if (!_isEditMode)
					return null;

				var existingTags = _fileTagsSettingsService.FileTagList ?? [];
				return existingTags.FirstOrDefault(t => t.Name == Name)?.Uid;
			}
		}

		private string _color = "#FFFFFFFF";
		public string Color
		{
			get => _color;
			set
			{
				if (!SetProperty(ref _color, value))
					return;

				OnPropertyChanged(nameof(CanCommit));
			}
		}

		private string _selectedCategoryId = string.Empty;
		public string SelectedCategoryId
		{
			get => _selectedCategoryId;
			set
			{
				if (!SetProperty(ref _selectedCategoryId, value))
					return;

				OnPropertyChanged(nameof(CanCommit));
			}
		}

		public IList<CategoryViewModel> Categories => _allCategories;

		private bool _isNameValid = true;
		public bool IsNameValid
		{
			get => _isNameValid;
			private set
			{
				if (SetProperty(ref _isNameValid, value))
					OnPropertyChanged(nameof(CanCommit));
			}
		}

		public bool CanCommit => !string.IsNullOrWhiteSpace(Name) && IsNameValid;

		public ICommand PrimaryButtonCommand { get; }
		public ICommand SecondaryButtonCommand { get; }

		public TagDialogViewModel()
		{
			_fileTagsSettingsService = Ioc.Default.GetRequiredService<IFileTagsSettingsService>();
			_fileTagsCategoriesSettingsService = Ioc.Default.GetRequiredService<IFileTagsCategoriesSettingsService>();
			_allCategories = _fileTagsCategoriesSettingsService.FileCategoryList ?? [];

			PrimaryButtonCommand = new RelayCommand(OnPrimaryClick);
			SecondaryButtonCommand = new RelayCommand(OnSecondaryClick);
		}

		public void InitializeCreate()
		{
			_isEditMode = false;
			Title = "CreateNewTag";
			Name = string.Empty;
			Color = ColorHelpers.RandomColor();
			SelectedCategoryId = _allCategories.FirstOrDefault()?.Uid ?? string.Empty;
			_isNameValid = true;
		}

		public void InitializeEdit(ListedTagViewModel tag)
		{
			_isEditMode = true;
			Title = "EditTag";
			Name = tag.Tag.Name;
			Color = tag.Tag.Color;
			SelectedCategoryId = tag.Tag.CategoryId ?? (_allCategories.FirstOrDefault()?.Uid ?? string.Empty);
			_isNameValid = true;
		}

		public string GetResultName() => Name.TrimEnd('.');

		public string GetResultColor() => Color;

		public string GetResultCategoryId() => SelectedCategoryId;

		private void OnPrimaryClick()
		{
			// Validation already happens in Name setter
		}

		private void OnSecondaryClick()
		{
			// Handled by dialog closing
		}
	}
}
