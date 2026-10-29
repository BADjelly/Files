// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Models
{
	[Serializable]
	public sealed partial class TagViewModel : ObservableObject
	{
		[JsonPropertyName("TagName")]
		public string Name { get; set; }

		[JsonPropertyName("ColorString")]
		public string Color { get; set; }

		[JsonPropertyName("Uid")]
		public string Uid { get; set; }

		[JsonPropertyName("CategoryId")]
		public string CategoryId { get; set; }

		[JsonConstructor]
		public TagViewModel(string name, string color, string uid, string categoryId = "00000000-0000-0000-0000-000000000000")
		{
			Name = name;
			Color = color;
			Uid = uid;
			CategoryId = categoryId;
		}
	}
}
