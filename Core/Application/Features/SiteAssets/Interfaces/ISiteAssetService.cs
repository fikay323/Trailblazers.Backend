using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Trailblazers.Backend.Core.Application.Features.SiteAssets.Dtos;

namespace Trailblazers.Backend.Core.Application.Features.SiteAssets.Interfaces
{
	public interface ISiteAssetService
	{
		Task<IReadOnlyList<SiteAssetDto>> GetAllAssetsAsync(string? group = null, CancellationToken cancellationToken = default);
		Task<SiteAssetDto?> GetAssetByKeyAsync(string key, CancellationToken cancellationToken = default);
		Task<SiteAssetDto> UpsertAssetAsync(string key, UpdateSiteAssetRequestDto request, CancellationToken cancellationToken = default);
		Task<bool> DeleteAssetAsync(string key, CancellationToken cancellationToken = default);
		Task<bool> ReorderAssetsAsync(ReorderSiteAssetsRequestDto request, CancellationToken cancellationToken = default);
	}
}
