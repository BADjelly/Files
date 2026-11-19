using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using WinRT;

namespace Files.App.Helpers
{
	public static class TransferHelpers
	{
		[DynamicWindowsRuntimeCast(typeof(StorageFile))]
		[DynamicWindowsRuntimeCast(typeof(StorageFolder))]
		public static async Task ExecuteTransferAsync(IReadOnlyList<IStorable> itemsToTransfer, ShellViewModel shellViewModel, StatusCenterViewModel statusViewModel, DataPackageOperation type = DataPackageOperation.Copy)
		{
			ConcurrentBag<IStorageItem> items = [];
			var itemsCount = itemsToTransfer.Count;
			var statusCenterItem = itemsCount > 50 ? StatusCenterHelper.AddCard_Prepare() : null;
			var dataPackage = new DataPackage() { RequestedOperation = type };

			try
			{
				// Update the status to in-progress
				if (statusCenterItem is not null)
				{
					statusCenterItem.Progress.EnumerationCompleted = true;
					statusCenterItem.Progress.ItemsCount = items.Count;
					statusCenterItem.Progress.ReportStatus(FileSystemStatusCode.InProgress);
				}

				await itemsToTransfer.ParallelForEachAsync(async storable =>
				{
					// Update the status to increase processed count by one
					if (statusCenterItem is not null)
					{
						statusCenterItem.Progress.AddProcessedItemsCount(1);
						statusCenterItem.Progress.Report();
					}

					var result = storable switch
					{
						IFile => await shellViewModel.GetFileFromPathAsync(storable.Id).OnSuccess(x => items.Add(x
							?? throw new InvalidOperationException($"The file '{storable.Id}' could not be opened."))),
						IFolder => await shellViewModel.GetFolderFromPathAsync(storable.Id).OnSuccess(x => items.Add(x
							?? throw new InvalidOperationException($"The folder '{storable.Id}' could not be opened."))),
						_ => throw new ArgumentOutOfRangeException(nameof(storable)),
					};

					if (!result)
						throw new SystemIO.IOException($"Failed to process {storable.Id} in cutting/copying to the clipboard.", (int)result.ErrorCode);
				}, 10, statusCenterItem?.CancellationToken ?? CancellationToken.None);

				// Include .fileshistory sidecar directories when cutting
				if (type is DataPackageOperation.Move)
				{
					var parentPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
					foreach (var item in itemsToTransfer)
					{
						var parentPath = SystemIO.Path.GetDirectoryName(item.Id);
						if (!string.IsNullOrEmpty(parentPath))
							parentPaths.Add(parentPath);
					}

					foreach (var storable in itemsToTransfer)
					{
						var parentPath = SystemIO.Path.GetDirectoryName(storable.Id);
						if (string.IsNullOrEmpty(parentPath))
							continue;

						var sidecarPath = PathNormalization.Combine(parentPath, $"{SystemIO.Path.GetFileName(storable.Id)}.fileshistory");
						if (SystemIO.Directory.Exists(sidecarPath))
						{
							// Skip if the sidecar's parent is already being cut as a folder
							if (parentPaths.Contains(sidecarPath))
								continue;

							var result = await shellViewModel.GetFolderFromPathAsync(sidecarPath).OnSuccess(x =>
							{
								if (x is not null)
									items.Add(x);
							});

							if (!result)
							{
								App.Logger.LogWarning($"Failed to open sidecar directory '{sidecarPath}' for cutting.");
							}
						}
					}
				}

				var standardObjectsOnly = items.All(x => x is StorageFile or StorageFolder or SystemStorageFile or SystemStorageFolder);
				if (standardObjectsOnly)
					items = new(await items.ToStandardStorageItemsAsync());

				if (items.IsEmpty)
					return;

				dataPackage.Properties.PackageFamilyName = Windows.ApplicationModel.Package.Current.Id.FamilyName;
				dataPackage.SetStorageItems(items, false);

				Clipboard.SetContent(dataPackage);
			}
			catch (Exception ex)
			{
				if (ex is not SystemIO.IOException)
					App.Logger.LogWarning(ex, "Failed to process cutting/copying due to an unknown error.");

				if ((FileSystemStatusCode)ex.HResult is FileSystemStatusCode.Unauthorized)
				{
					var filePaths = itemsToTransfer.Select(x => x.Id).ToArray();
					if (type is DataPackageOperation.Move)
					{
						var parentPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
						foreach (var item in itemsToTransfer)
						{
							var parentPath = SystemIO.Path.GetDirectoryName(item.Id);
							if (!string.IsNullOrEmpty(parentPath))
								parentPaths.Add(parentPath);
						}

						foreach (var storable in itemsToTransfer)
						{
							var parentPath = SystemIO.Path.GetDirectoryName(storable.Id);
							if (string.IsNullOrEmpty(parentPath))
								continue;

							var sidecarPath = PathNormalization.Combine(parentPath, $"{SystemIO.Path.GetFileName(storable.Id)}.fileshistory");
							if (SystemIO.Directory.Exists(sidecarPath))
							{
								if (!parentPaths.Contains(sidecarPath))
								{
									filePaths = filePaths.Append(sidecarPath).ToArray();
								}
							}
						}
					}

					await FileOperationsHelpers.SetClipboard(filePaths, type);
				}
			}
			finally
			{
				if (statusCenterItem is not null)
					statusViewModel.RemoveItem(statusCenterItem);
			}
		}

		[DynamicWindowsRuntimeCast(typeof(StorageFile))]
		[DynamicWindowsRuntimeCast(typeof(StorageFolder))]
		public static async Task ExecuteTransferAsync(IContentPageContext context, StatusCenterViewModel statusViewModel, DataPackageOperation type = DataPackageOperation.Copy)
		{
			if (context.ShellPage?.SlimContentPage is not { } contentPage ||
				contentPage.IsItemSelected is false)
				return;

			// Reset cut mode
			contentPage.ItemManipulationModel.RefreshItemsOpacity();

			ConcurrentBag<IStorageItem> items = [];
			var itemsCount = context.SelectedItems.Count;
			var statusCenterItem = itemsCount > 50 ? StatusCenterHelper.AddCard_Prepare() : null;
			var dataPackage = new DataPackage() { RequestedOperation = type };

			try
			{
				// Update the status to in-progress
				if (statusCenterItem is not null)
				{
					statusCenterItem.Progress.EnumerationCompleted = true;
					statusCenterItem.Progress.ItemsCount = items.Count;
					statusCenterItem.Progress.ReportStatus(FileSystemStatusCode.InProgress);
				}

				await context.SelectedItems.ToList().ParallelForEachAsync(async listedItem =>
				{
					// Update the status to increase processed count by one
					if (statusCenterItem is not null)
					{
						statusCenterItem.Progress.AddProcessedItemsCount(1);
						statusCenterItem.Progress.Report();
					}

					if (listedItem is FtpItem ftpItem)
					{
						// Don't dim selected items here since FTP doesn't support cut
						if (ftpItem.PrimaryItemAttribute is StorageItemTypes.File or StorageItemTypes.Folder)
							items.Add(await ftpItem.ToStorageItem());
					}
					else
					{
						if (type is DataPackageOperation.Move)
						{
							// Dim opacities accordingly
							await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() =>
							{
								listedItem.Opacity = Constants.UI.DimItemOpacity;
							});
						}

						var shellViewModel = context.ShellPage.GetRequiredShellViewModel();
						var itemPath = listedItem.GetRequiredPath();
						var result = listedItem.PrimaryItemAttribute == StorageItemTypes.File || listedItem is ZipItem
								? await shellViewModel.GetFileFromPathAsync(itemPath).OnSuccess(t => items.Add(t
									?? throw new InvalidOperationException($"The file '{itemPath}' could not be opened.")))
								: await shellViewModel.GetFolderFromPathAsync(itemPath).OnSuccess(t => items.Add(t
									?? throw new InvalidOperationException($"The folder '{itemPath}' could not be opened.")));

						if (!result)
							throw new SystemIO.IOException($"Failed to process {listedItem.ItemPath} in cutting/copying to the clipboard.", (int)result.ErrorCode);
					}
				}, 10, statusCenterItem?.CancellationToken ?? CancellationToken.None);

				// Include .fileshistory sidecar directories when cutting
				if (type is DataPackageOperation.Move)
				{
					var selectedPaths = context.SelectedItems.Select(x => x.ItemPath!).ToList();
					var parentPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
					foreach (var path in selectedPaths)
					{
						var parentPath = SystemIO.Path.GetDirectoryName(path);
						if (!string.IsNullOrEmpty(parentPath))
							parentPaths.Add(parentPath);
					}

					foreach (var listedItem in context.SelectedItems)
					{
						var itemPath = listedItem.ItemPath!;
						var parentPath = SystemIO.Path.GetDirectoryName(itemPath);
						if (string.IsNullOrEmpty(parentPath))
							continue;

						var sidecarPath = PathNormalization.Combine(parentPath, $"{SystemIO.Path.GetFileName(itemPath)}.fileshistory");
						if (SystemIO.Directory.Exists(sidecarPath))
						{
							// Skip if the sidecar's parent is already being cut as a folder
							if (parentPaths.Contains(sidecarPath))
								continue;

							var shellViewModel = context.ShellPage.GetRequiredShellViewModel();
							var result = await shellViewModel.GetFolderFromPathAsync(sidecarPath).OnSuccess(x =>
							{
								if (x is not null)
									items.Add(x);
							});

							if (!result)
							{
								App.Logger.LogWarning($"Failed to open sidecar directory '{sidecarPath}' for cutting.");
							}
						}
					}
				}

				var standardObjectsOnly = items.All(x => x is StorageFile or StorageFolder or SystemStorageFile or SystemStorageFolder);
				if (standardObjectsOnly)
					items = new(await items.ToStandardStorageItemsAsync());

				if (items.IsEmpty)
					return;

				dataPackage.Properties.PackageFamilyName = Windows.ApplicationModel.Package.Current.Id.FamilyName;
				dataPackage.SetStorageItems(items, false);

				Clipboard.SetContent(dataPackage);
			}
			catch (Exception ex)
			{
				if (ex is not SystemIO.IOException)
					App.Logger.LogWarning(ex, "Failed to process cutting/copying due to an unknown error.");

				if ((FileSystemStatusCode)ex.HResult is FileSystemStatusCode.Unauthorized)
				{
					var filePaths = context.SelectedItems.Select(x => x.ItemPath!).ToArray();
					if (type is DataPackageOperation.Move)
					{
						var parentPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
						foreach (var item in context.SelectedItems)
						{
							var parentPath = SystemIO.Path.GetDirectoryName(item.ItemPath!);
							if (!string.IsNullOrEmpty(parentPath))
								parentPaths.Add(parentPath);
						}

						foreach (var listedItem in context.SelectedItems)
						{
							var itemPath = listedItem.ItemPath!;
							var parentPath = SystemIO.Path.GetDirectoryName(itemPath);
							if (string.IsNullOrEmpty(parentPath))
								continue;

							var sidecarPath = PathNormalization.Combine(parentPath, $"{SystemIO.Path.GetFileName(itemPath)}.fileshistory");
							if (SystemIO.Directory.Exists(sidecarPath))
							{
								if (!parentPaths.Contains(sidecarPath))
								{
									filePaths = filePaths.Append(sidecarPath).ToArray();
								}
							}
						}
					}

					await FileOperationsHelpers.SetClipboard(filePaths, type);

					return;
				}

				// Reset cut mode
				context.ShellPage.SlimContentPage.ItemManipulationModel.RefreshItemsOpacity();
			}
			finally
			{
				if (statusCenterItem is not null)
					statusViewModel.RemoveItem(statusCenterItem);
			}
		}
	}
}
