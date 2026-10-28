// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Services.Settings;

namespace Files.App.Data.Models
{
	public sealed partial class ListedTagViewModel : ObservableObject
	{
		private const string UncategorizedCategoryId = "00000000-0000-0000-0000-000000000000";

		private readonly IFileTagsCategoriesSettingsService _categoriesService;

		private TagViewModel _Tag;
		public TagViewModel Tag
		{
			get => _Tag;
			set => SetProperty(ref _Tag, value);
		}

		private bool _IsEditing;
		public bool IsEditing
		{
			get => _IsEditing;
			set => SetProperty(ref _IsEditing, value);
		}

		private bool _IsNameValid = true;
		public bool IsNameValid
		{
			get => _IsNameValid;
			set => SetProperty(ref _IsNameValid, value);
		}

		private bool _CanCommit = false;
		public bool CanCommit
		{
			get => _CanCommit;
			set => SetProperty(ref _CanCommit, value);
		}

		private string? _NewName;
		public string? NewName
		{
			get => _NewName;
			set => SetProperty(ref _NewName, value);
		}

		private string _NewColor;
		public string NewColor
		{
			get => _NewColor;
			set => SetProperty(ref _NewColor, value);
		}

		private string? _categoryName;
		public string? CategoryName
		{
			get => _categoryName;
			private set
			{
				if (SetProperty(ref _categoryName, value))
				{
					OnPropertyChanged(nameof(DisplayText));
				}
			}
		}

		public string DisplayText
		{
			get
			{
				if (_categoryName is null)
					return _Tag?.Name ?? string.Empty;

				return $"{_Tag?.Name} : {_categoryName}";
			}
		}

		private void UpdateCategoryName()
		{
			if (_Tag?.CategoryId is null or UncategorizedCategoryId)
			{
				_categoryName = null;
			}
			else
			{
				var categories = _categoriesService.FileCategoryList ?? [];
				_categoryName = categories.FirstOrDefault(c => c.Uid == _Tag.CategoryId)?.Name;
			}
		}

		public ListedTagViewModel(TagViewModel tag, IFileTagsCategoriesSettingsService categoriesService)
		{
			_categoriesService = categoriesService;
			_Tag = tag;
			_NewColor = tag.Color;
			UpdateCategoryName();
			_Tag.PropertyChanged += Tag_PropertyChanged;
		}

		private void Tag_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(TagViewModel.CategoryId))
				UpdateCategoryName();
		}
	}
}
