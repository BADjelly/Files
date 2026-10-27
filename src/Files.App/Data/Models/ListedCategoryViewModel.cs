// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Models
{
	public sealed partial class ListedCategoryViewModel : ObservableObject
	{
		private CategoryViewModel _Category;
		public CategoryViewModel Category
		{
			get => _Category;
			set => SetProperty(ref _Category, value);
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

		public ListedCategoryViewModel(CategoryViewModel category)
		{
			_Category = category;
			_NewColor = category.Color;
		}
	}
}
