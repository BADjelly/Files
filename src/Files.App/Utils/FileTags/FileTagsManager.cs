// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Data.Items;
using Microsoft.Extensions.Logging;
using System.Collections.Specialized;

namespace Files.App.Utils.FileTags
{
	public sealed class FileTagsManager
	{
		private const string UncategorizedCategoryId = "00000000-0000-0000-0000-000000000000";

		private readonly ILogger logger = Ioc.Default.GetRequiredService<ILogger<App>>();
		private readonly IFileTagsSettingsService fileTagsSettingsService = Ioc.Default.GetRequiredService<IFileTagsSettingsService>();
		private readonly IFileTagsCategoriesSettingsService fileTagsCategoriesSettingsService = Ioc.Default.GetRequiredService<IFileTagsCategoriesSettingsService>();

		public EventHandler<NotifyCollectionChangedEventArgs>? DataChanged;

		private readonly List<FileTagCategoryItem> _categoryGroups = [];
		public IReadOnlyList<FileTagCategoryItem> CategoryGroups
		{
			get
			{
				lock (_categoryGroups)
				{
					return _categoryGroups.ToList().AsReadOnly();
				}
			}
		}

		public FileTagsManager()
		{
			fileTagsSettingsService.OnTagsUpdated += TagsUpdatedAsync;
			fileTagsCategoriesSettingsService.OnCategoriesUpdated += CategoriesUpdatedAsync;
		}

		private async void TagsUpdatedAsync(object? _, EventArgs e)
		{
			lock (_categoryGroups)
				_categoryGroups.Clear();
			DataChanged?.Invoke(SectionType.FileTag, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

			await UpdateFileTagsAsync();
		}

		private async void CategoriesUpdatedAsync(object? _, EventArgs e)
		{
			lock (_categoryGroups)
				_categoryGroups.Clear();
			DataChanged?.Invoke(SectionType.FileTag, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

			await UpdateFileTagsAsync();
		}

		public Task UpdateFileTagsAsync()
		{
			try
			{
				lock (_categoryGroups)
					_categoryGroups.Clear();

				var tags = fileTagsSettingsService.FileTagList ?? [];
				var categories = fileTagsCategoriesSettingsService.FileCategoryList ?? [];

				// Iterate categories in their defined order, then add tags for each category
				foreach (var category in categories)
				{
					if (string.Equals(category.Uid, UncategorizedCategoryId, StringComparison.OrdinalIgnoreCase))
						continue;

					var categoryTags = tags.Where(t => string.Equals(t.CategoryId, category.Uid, StringComparison.OrdinalIgnoreCase)).ToList();
					if (categoryTags.Count == 0)
						continue;

					var categoryItem = new FileTagCategoryItem
					{
						Text = category.Name,
						Category = category,
						ChildItems = [],
						MenuOptions = new ContextMenuOptions { IsLocationItem = true },
					};

					foreach (var tag in categoryTags)
					{
						var tagItem = new FileTagItem
						{
							Text = tag.Name,
							Path = FolderSearch.FormatTagQuery(tag.Name),
							FileTag = tag,
							MenuOptions = new ContextMenuOptions { IsLocationItem = true },
						};

						categoryItem.ChildItems!.Add(tagItem);
					}

					lock (_categoryGroups)
					{
						_categoryGroups.Add(categoryItem);
					}
					DataChanged?.Invoke(SectionType.FileTag, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, categoryItem));
				}

				// Add Uncategorized tags as direct children (flat list, no category group)
				var uncategorizedTags = tags.Where(t => t.CategoryId is null or UncategorizedCategoryId);
				foreach (var tag in uncategorizedTags)
				{
					var tagItem = new FileTagItem
					{
						Text = tag.Name,
						Path = FolderSearch.FormatTagQuery(tag.Name),
						FileTag = tag,
						MenuOptions = new ContextMenuOptions { IsLocationItem = true },
					};

					lock (_categoryGroups)
					{
						if (_categoryGroups.Any(x => x.ChildItems?.Any(y => y.Path == tagItem.Path) == true))
						{
							continue;
						}
					}

					// Find or create Uncategorized group
					FileTagCategoryItem? uncategorizedGroup = null;
					lock (_categoryGroups)
					{
						uncategorizedGroup = _categoryGroups.FirstOrDefault(g => g.Category?.Uid == UncategorizedCategoryId);
						if (uncategorizedGroup is null)
						{
							uncategorizedGroup = new FileTagCategoryItem
							{
								Text = "Uncategorized",
								Category = new CategoryViewModel("Uncategorized", UncategorizedCategoryId),
								ChildItems = [],
								MenuOptions = new ContextMenuOptions { IsLocationItem = true },
							};
							_categoryGroups.Add(uncategorizedGroup);
						}
					}

					uncategorizedGroup.ChildItems!.Add(tagItem);
					DataChanged?.Invoke(SectionType.FileTag, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, tagItem));
				}
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Error loading tags section.");
			}

			return Task.CompletedTask;
		}
	}
}
