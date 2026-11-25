// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Models
{
	public sealed partial class TagChipViewModel : ObservableObject
	{
		public TagViewModel Tag { get; }

		public bool IsSelected
		{
			get => _IsSelected;
			set => SetProperty(ref _IsSelected, value);
		}
		private bool _IsSelected;

		public TagChipViewModel(TagViewModel tag, bool isSelected)
		{
			Tag = tag;
			IsSelected = isSelected;
		}
	}
}
