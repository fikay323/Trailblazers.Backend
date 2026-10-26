namespace Trailblazers.Backend.Core.Application.Features.SiteAssets.Dtos
{
	public class UpdateSiteAssetRequestDto
	{
		public string? Group { get; set; }
		public string ImageUrl { get; set; } = string.Empty;
		public string? PublicId { get; set; }
		public string? AltText { get; set; }
		public string? Title { get; set; }
		public string? Caption { get; set; }
		public string? MetadataJson { get; set; }
		public int? SortOrder { get; set; }
	}
}
