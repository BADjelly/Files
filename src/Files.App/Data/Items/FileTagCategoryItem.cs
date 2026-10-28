// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using WinRT;

namespace Files.App.Data.Items
{
	/// <summary>
	/// Represents a category group in the sidebar Tags section.
	/// Contains FileTagItem children and is expandable/collapsible.
	/// </summary>
	public sealed partial class FileTagCategoryItem : ObservableObject, INavigationControlItem
	{
		private string _text = "";
		public string Text
		{
			get => _text;
			set => SetProperty(ref _text, value);
		}

		public string? Path { get; set; }

		public SectionType Section => SectionType.FileTag;

		public NavigationControlItemType ItemType => NavigationControlItemType.FileTag;

		public ContextMenuOptions? MenuOptions { get; set; }

		public string? ToolTipText { get; set; }

		private bool _isExpanded;
		public bool IsExpanded
		{
			get => _isExpanded;
			set => SetProperty(ref _isExpanded, value);
		}

		public object? Children => ChildItems;

		public BulkConcurrentObservableCollection<FileTagItem>? ChildItems { get; set; }

		public CategoryViewModel? Category { get; set; }

		public IconElement? IconElement => null;

		FrameworkElement? ISidebarItemPresentationModel.IconElement => null;

		public object? ToolTip => Text;

		FrameworkElement? ISidebarItemPresentationModel.ItemDecorator => null;

		public int CompareTo(INavigationControlItem? other)
		{
			var text = Text ?? throw new InvalidOperationException("The category name has not been initialized.");
			var otherText = other?.Text
				?? throw new ArgumentException("The compared item must have a name.", nameof(other));

			return text.CompareTo(otherText);
		}
	}
}
