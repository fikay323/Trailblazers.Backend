namespace Trailblazers.Backend.Core.Application.Features.SiteAssets.Dtos
{
	public class MediaUploadResponseDto
	{
		public string Url { get; set; } = string.Empty;
		public string PublicId { get; set; } = string.Empty;
		public string FileName { get; set; } = string.Empty;
		public long FileSizeBytes { get; set; }
	}
}
