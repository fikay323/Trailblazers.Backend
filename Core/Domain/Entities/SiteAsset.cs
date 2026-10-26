using System;

namespace Trailblazers.Backend.Core.Domain.Entities
{
	public class SiteAsset
	{
		public string Key { get; set; } = string.Empty;
		public string Group { get; set; } = string.Empty;
		public string ImageUrl { get; set; } = string.Empty;
		public string? PublicId { get; set; }
		public string? AltText { get; set; }
		public string? Title { get; set; }
		public string? Caption { get; set; }
		public string? MetadataJson { get; set; }
		public int SortOrder { get; set; } = 0;
		public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
	}
}
