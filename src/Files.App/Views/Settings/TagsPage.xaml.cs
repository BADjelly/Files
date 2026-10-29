// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Files.App.Data.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using WinRT;

namespace Files.App.Views.Settings
{
	public sealed partial class TagsPage : Page
	{
		private readonly IWindowContext WindowContext = Ioc.Default.GetRequiredService<IWindowContext>();

		private string oldCategoryName = string.Empty;

		// Will be null unless the user has edited any category
		private ListedCategoryViewModel? editingCategory;

		private FlyoutBase? deleteCategoryItemFlyout;

		public bool AllowItemsDrag
			=> WindowContext.CanDragAndDrop;

		public bool CanDragCategories
			=> WindowContext.CanDragAndDrop;

		public TagsPage()
		{
			InitializeComponent();
		}

		private void KeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
		{
			if (args.KeyboardAccelerator.Key is VirtualKey.Escape && editingCategory is not null)
			{
				CloseCategoryEdit();
				args.Handled = true;
			}
		}

		[DynamicWindowsRuntimeCast(typeof(Button))]
		private void EditTag_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button { DataContext: ListedTagViewModel tag })
				return;

			_ = ViewModel.ShowTagDialogAsync(tag);
		}

		private FlyoutBase? deleteTagItemFlyout;

		[DynamicWindowsRuntimeCast(typeof(Button))]
		private void PreRemoveTag_Click(object sender, RoutedEventArgs e)
		{
			deleteTagItemFlyout = ((Button)sender).Flyout;
		}

		private void CancelRemoveTag_Click(object sender, RoutedEventArgs e)
		{
			deleteTagItemFlyout?.Hide();
		}

		[DynamicWindowsRuntimeCast(typeof(Microsoft.UI.Xaml.Controls.Button))]
		private void RemoveTag_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.DeleteExistingTag((ListedTagViewModel)((Button)sender).DataContext);
		}

		private bool IsNameValid(string name)
		{
			return !(
				string.IsNullOrWhiteSpace(name) ||
				name.StartsWith('.') ||
				name.EndsWith('.')
			);
		}

		[DynamicWindowsRuntimeCast(typeof(TextBox))]
		private void RenameCategoryTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			var textBox = (TextBox)sender;
			switch (e.Key)
			{
				case VirtualKey.Enter:
					if (!editingCategory!.CanCommit)
						return;

					CommitCategoryChanges(textBox);
					e.Handled = true;
					break;
			}
		}

		[DynamicWindowsRuntimeCast(typeof(ListViewItem))]
		[DynamicWindowsRuntimeCast(typeof(TextBlock))]
		[DynamicWindowsRuntimeCast(typeof(TextBox))]
		private void EditCategory_Click(object sender, RoutedEventArgs e)
		{
			if (editingCategory is not null)
			{
				editingCategory.IsEditing = false;
				editingCategory.NewName = editingCategory.Category.Name;
			}

			if (sender is not Button { DataContext: ListedCategoryViewModel category })
				return;

			editingCategory = category;
			editingCategory.NewName = editingCategory.Category.Name;
			editingCategory.IsEditing = true;

			var item = (CategoriesList.ContainerFromItem(editingCategory) as ListViewItem)!;
			var textBlock = (item.FindDescendant("CategoryName") as TextBlock)!;
			var textBox = (item.FindDescendant("CategoryNameTextBox") as TextBox)!;

			textBox.TextChanged += RenameCategoryTextBox_TextChanged;

			textBox.Text = textBlock.Text;
			oldCategoryName = textBlock.Text;
		}

		[DynamicWindowsRuntimeCast(typeof(ListViewItem))]
		[DynamicWindowsRuntimeCast(typeof(TextBox))]
		private void CommitCategoryRename_Click(object sender, RoutedEventArgs e)
		{
			var item = (CategoriesList.ContainerFromItem(editingCategory) as ListViewItem)!;

			CommitCategoryChanges((item.FindDescendant("CategoryNameTextBox") as TextBox)!);
		}

		private void CancelCategoryRename_Click(object sender, RoutedEventArgs e)
		{
			CloseCategoryEdit();
		}

		[DynamicWindowsRuntimeCast(typeof(Button))]
		private void PreRemoveCategory_Click(object sender, RoutedEventArgs e)
		{
			deleteCategoryItemFlyout = ((Button)sender).Flyout;
		}

		private void CancelRemoveCategory_Click(object sender, RoutedEventArgs e)
		{
			deleteCategoryItemFlyout?.Hide();
		}

		[DynamicWindowsRuntimeCast(typeof(Button))]
		private void RemoveCategory_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.DeleteExistingCategory((ListedCategoryViewModel)((Button)sender).DataContext);
		}

		[DynamicWindowsRuntimeCast(typeof(TextBox))]
		private void RenameCategoryTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			var category = editingCategory!;
			var text = ((TextBox)sender).Text;
			var isNullOrEmpty = string.IsNullOrEmpty(text);
			category.IsNameValid = isNullOrEmpty || (IsNameValid(text) && !ViewModel.Categories.Any(item => item.Category.Name == text && category.Category.Name != text));
			category.CanCommit = !isNullOrEmpty && category.IsNameValid && text != category.Category.Name;
		}

		[DynamicWindowsRuntimeCast(typeof(TextBox))]
		private void NewCategoryTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			var text = ((TextBox)sender).Text;
			ViewModel.NewCategory.Name = text;
			ViewModel.NewCategory.IsNameValid = string.IsNullOrEmpty(text) || (IsNameValid(text) && !ViewModel.Categories.Any(cat => text == cat.Category.Name));
		}

		private void CommitCategoryChanges(TextBox textBox)
		{
			var category = editingCategory!;
			EndCategoryEditing(textBox);
			string newCategoryName = textBox.Text.Trim().TrimEnd('.');
			if (newCategoryName != oldCategoryName)
				ViewModel.EditExistingCategory(category, newCategoryName);
		}

		private void EndCategoryEditing(TextBox textBox)
		{
			textBox.TextChanged -= RenameCategoryTextBox_TextChanged;
			editingCategory!.IsEditing = false;
		}

		[DynamicWindowsRuntimeCast(typeof(ListViewItem))]
		[DynamicWindowsRuntimeCast(typeof(TextBox))]
		private void CloseCategoryEdit()
		{
			var category = editingCategory!;
			var item = (CategoriesList.ContainerFromItem(category) as ListViewItem)!;
			var textBox = (item.FindDescendant("CategoryNameTextBox") as TextBox)!;

			category.IsNameValid = true;
			category.CanCommit = false;

			EndCategoryEditing(textBox);
		}
	}
}
