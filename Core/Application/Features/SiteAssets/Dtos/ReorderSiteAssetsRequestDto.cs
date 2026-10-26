using System.Collections.Generic;

namespace Trailblazers.Backend.Core.Application.Features.SiteAssets.Dtos
{
	public class ReorderItemDto
	{
		public string Key { get; set; } = string.Empty;
		public int SortOrder { get; set; }
	}

	public class ReorderSiteAssetsRequestDto
	{
		public List<ReorderItemDto> Items { get; set; } = [];
	}
}
