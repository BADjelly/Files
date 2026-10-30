// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Models
{
	public sealed partial class NewCategoryViewModel : ObservableObject
	{
		private string name = string.Empty;
		public string Name
		{
			get => name;
			set
			{
				SetProperty(ref name, value);
				{
					OnPropertyChanged(nameof(CanCommit));
					OnPropertyChanged(nameof(IsNameValid));
				}
			}
		}

		private bool isNameValid = true;
		public bool IsNameValid
		{
			get => isNameValid;
			set
			{
				if (SetProperty(ref isNameValid, value))
					OnPropertyChanged(nameof(CanCommit));
			}
		}

		public bool CanCommit => !string.IsNullOrEmpty(name) && IsNameValid;

		public void Reset()
		{
			Name = string.Empty;
			IsNameValid = true;
		}
	}
}
