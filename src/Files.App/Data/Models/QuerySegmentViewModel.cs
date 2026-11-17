// Copyright (c) Files Community. Licensed under the MIT License.

namespace Files.App.Data.Models
{
	public enum QuerySegmentType
	{
		Tag,
		Connector,
	}

	[ObservableObject]
	public partial class QuerySegmentViewModel
	{
		public QuerySegmentType Type { get; }

		public string Text { get; }

		public string? Connector { get; set; }

		public bool IsConnector => Type == QuerySegmentType.Connector;

		public QuerySegmentViewModel(string text, QuerySegmentType type, string? connector = null)
		{
			Text = text;
			Type = type;
			Connector = connector;
		}
	}
}
