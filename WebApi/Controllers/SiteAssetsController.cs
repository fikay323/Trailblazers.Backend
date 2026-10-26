using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Trailblazers.Backend.Core.Application.Features.SiteAssets.Dtos;
using Trailblazers.Backend.Core.Application.Features.SiteAssets.Interfaces;
using Trailblazers.Backend.Core.Application.Interfaces;
using Trailblazers.Backend.WebApi.Authentication;

namespace Trailblazers.Backend.WebApi.Controllers
{
	[ApiController]
	public class SiteAssetsController : ControllerBase
	{
		private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif", ".svg"];
		private static readonly string[] AllowedContentTypes =
		[
			"image/jpeg", "image/png", "image/webp", "image/gif", "image/svg+xml"
		];
		private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB

		private readonly ISiteAssetService _siteAssetService;
		private readonly IImageStorageService _imageStorageService;
		private readonly ILogger<SiteAssetsController> _logger;

		public SiteAssetsController(
			ISiteAssetService siteAssetService,
			IImageStorageService imageStorageService,
			ILogger<SiteAssetsController> logger)
		{
			_siteAssetService = siteAssetService;
			_imageStorageService = imageStorageService;
			_logger = logger;
		}

		// ==========================================
		// PUBLIC ENDPOINTS
		// ==========================================

		/// <summary>
		/// Retrieves site assets with optional group filtering.
		/// </summary>
		[HttpGet("api/site-assets")]
		public async Task<IActionResult> GetAllAssets(
			[FromQuery] string? group = null,
			CancellationToken cancellationToken = default)
		{
			try
			{
				var assets = await _siteAssetService.GetAllAssetsAsync(group, cancellationToken);
				return Ok(assets);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to retrieve site assets");
				return StatusCode(500, new { error = "Failed to retrieve site assets." });
			}
		}

		/// <summary>
		/// Retrieves a single site asset by its unique key.
		/// </summary>
		[HttpGet("api/site-assets/{key}")]
		public async Task<IActionResult> GetAssetByKey(
			string key,
			CancellationToken cancellationToken = default)
		{
			try
			{
				var asset = await _siteAssetService.GetAssetByKeyAsync(key, cancellationToken);
				if (asset == null)
				{
					return NotFound(new { error = $"Site asset with key '{key}' was not found." });
				}

				return Ok(asset);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to retrieve site asset with key {Key}", key);
				return StatusCode(500, new { error = "Failed to retrieve site asset." });
			}
		}

		// ==========================================
		// ADMIN MEDIA & ASSET MANAGEMENT ENDPOINTS
		// ==========================================

		/// <summary>
		/// Admin: Uploads a media image directly to Cloudinary and returns its URL and PublicId.
		/// </summary>
		[HttpPost("api/admin/media/upload")]
		[ServiceFilter(typeof(ApiKeyAuthFilter))]
		[RequestSizeLimit(12 * 1024 * 1024)]
		public async Task<IActionResult> UploadMedia(
			[FromForm] IFormFile file,
			[FromForm] string? folder = null,
			[FromForm] int? maxWidth = null,
			[FromForm] int? maxHeight = null,
			CancellationToken cancellationToken = default)
		{
			if (file == null || file.Length == 0)
			{
				return BadRequest(new { error = "Please select a valid image file to upload." });
			}

			if (file.Length > MaxFileSizeBytes)
			{
				return BadRequest(new { error = "File size exceeds the 10MB limit." });
			}

			var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
			if (!AllowedExtensions.Contains(extension) || !AllowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
			{
				return BadRequest(new { error = "Only standard image formats (JPEG, PNG, WebP, GIF, SVG) are allowed." });
			}

			try
			{
				var targetFolder = string.IsNullOrWhiteSpace(folder)
					? "trailblazers/site-assets"
					: $"trailblazers/{folder.Trim().TrimStart('/')}";

				await using var stream = file.OpenReadStream();
				var (url, publicId) = await _imageStorageService.UploadMediaAsync(
					stream,
					file.FileName,
					targetFolder,
					maxWidth,
					maxHeight,
					cancellationToken);

				_logger.LogInformation("Admin successfully uploaded image {FileName} to {Folder}. URL: {Url}", file.FileName, targetFolder, url);

				return Ok(new MediaUploadResponseDto
				{
					Url = url,
					PublicId = publicId,
					FileName = file.FileName,
					FileSizeBytes = file.Length
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to upload image {FileName}", file.FileName);
				return StatusCode(500, new { error = $"Image upload failed: {ex.Message}" });
			}
		}

		/// <summary>
		/// Admin: Upserts a site asset slot or item.
		/// </summary>
		[HttpPut("api/admin/site-assets/{key}")]
		[ServiceFilter(typeof(ApiKeyAuthFilter))]
		public async Task<IActionResult> UpsertAsset(
			string key,
			[FromBody] UpdateSiteAssetRequestDto request,
			CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrWhiteSpace(key))
			{
				return BadRequest(new { error = "Asset key is required." });
			}

			if (string.IsNullOrWhiteSpace(request?.ImageUrl))
			{
				return BadRequest(new { error = "ImageUrl is required." });
			}

			try
			{
				var asset = await _siteAssetService.UpsertAssetAsync(key, request, cancellationToken);
				return Ok(asset);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to update site asset {Key}", key);
				return StatusCode(500, new { error = $"Failed to update site asset: {ex.Message}" });
			}
		}

		/// <summary>
		/// Admin: Deletes a site asset.
		/// </summary>
		[HttpDelete("api/admin/site-assets/{key}")]
		[ServiceFilter(typeof(ApiKeyAuthFilter))]
		public async Task<IActionResult> DeleteAsset(
			string key,
			CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrWhiteSpace(key))
			{
				return BadRequest(new { error = "Asset key is required." });
			}

			try
			{
				var deleted = await _siteAssetService.DeleteAssetAsync(key, cancellationToken);
				if (!deleted)
				{
					return NotFound(new { error = $"Asset with key '{key}' not found." });
				}

				return NoContent();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to delete site asset {Key}", key);
				return StatusCode(500, new { error = $"Failed to delete site asset: {ex.Message}" });
			}
		}

		/// <summary>
		/// Admin: Batch reorders site assets (e.g. hero slides or gallery items).
		/// </summary>
		[HttpPost("api/admin/site-assets/reorder")]
		[ServiceFilter(typeof(ApiKeyAuthFilter))]
		public async Task<IActionResult> ReorderAssets(
			[FromBody] ReorderSiteAssetsRequestDto request,
			CancellationToken cancellationToken = default)
		{
			if (request == null || request.Items == null || request.Items.Count == 0)
			{
				return BadRequest(new { error = "Items list cannot be empty." });
			}

			try
			{
				await _siteAssetService.ReorderAssetsAsync(request, cancellationToken);
				return Ok(new { success = true });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to reorder site assets");
				return StatusCode(500, new { error = "Failed to reorder site assets." });
			}
		}
	}
}
