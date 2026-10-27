// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Models
{
	[Serializable]
	public sealed partial class CategoryViewModel : ObservableObject
	{
		[JsonPropertyName("CategoryName")]
		public string Name { get; set; }

		[JsonPropertyName("ColorString")]
		public string Color { get; set; }

		[JsonPropertyName("Uid")]
		public string Uid { get; set; }

		[JsonConstructor]
		public CategoryViewModel(string name, string color, string uid)
		{
			Name = name;
			Color = color;
			Uid = uid;
		}
	}
}
