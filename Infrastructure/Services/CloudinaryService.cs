using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Trailblazers.Backend.Core.Application.Interfaces;

namespace Trailblazers.Backend.Infrastructure.Services
{
	public class CloudinaryService : IImageStorageService
	{
		private readonly Cloudinary _cloudinary;
		private readonly ILogger<CloudinaryService> _logger;

		public CloudinaryService(IConfiguration configuration, ILogger<CloudinaryService> logger)
		{
			_logger = logger;

			var cloudName = configuration["Cloudinary:CloudName"]
				?? configuration["Cloudinary__CloudName"]
				?? Environment.GetEnvironmentVariable("Cloudinary__CloudName")
				?? Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME")
				?? string.Empty;

			var apiKey = configuration["Cloudinary:ApiKey"]
				?? configuration["Cloudinary__ApiKey"]
				?? Environment.GetEnvironmentVariable("Cloudinary__ApiKey")
				?? Environment.GetEnvironmentVariable("CLOUDINARY_API_KEY")
				?? string.Empty;

			var apiSecret = configuration["Cloudinary:ApiSecret"]
				?? configuration["Cloudinary__ApiSecret"]
				?? Environment.GetEnvironmentVariable("Cloudinary__ApiSecret")
				?? Environment.GetEnvironmentVariable("CLOUDINARY_API_SECRET")
				?? string.Empty;

			if (string.IsNullOrWhiteSpace(cloudName) || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiSecret))
			{
				_logger.LogWarning("Cloudinary credentials are not fully configured. CloudName: {CloudName}, HasApiKey: {HasApiKey}",
					cloudName, !string.IsNullOrWhiteSpace(apiKey));
			}

			var account = new Account(cloudName, apiKey, apiSecret);
			_cloudinary = new Cloudinary(account);
			_cloudinary.Api.Secure = true;
		}

		public async Task<(string Url, string PublicId)> UploadProfilePictureAsync(Stream stream, string fileName, string userId, CancellationToken cancellationToken = default)
		{
			if (stream == null || stream.Length == 0)
			{
				throw new ArgumentException("Image stream cannot be null or empty", nameof(stream));
			}

			var uploadParams = new ImageUploadParams
			{
				File = new FileDescription(fileName, stream),
				Folder = "trailblazers/profiles",
				PublicId = $"student_{userId}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
				Overwrite = true,
				Transformation = new Transformation()
					.Width(500)
					.Height(500)
					.Crop("fill")
					.Gravity("face")
					.Quality("auto")
					.FetchFormat("auto")
			};

			var uploadResult = await _cloudinary.UploadAsync(uploadParams, cancellationToken);

			if (uploadResult.Error != null)
			{
				_logger.LogError("Cloudinary upload failed: {Error}", uploadResult.Error.Message);
				throw new InvalidOperationException($"Image upload failed: {uploadResult.Error.Message}");
			}

			_logger.LogInformation("Profile picture uploaded successfully for user {UserId}. PublicId: {PublicId}", userId, uploadResult.PublicId);
			return (uploadResult.SecureUrl?.ToString() ?? uploadResult.Url?.ToString() ?? string.Empty, uploadResult.PublicId);
		}

		public async Task<(string Url, string PublicId)> UploadMediaAsync(
			Stream stream,
			string fileName,
			string folder = "trailblazers/site-assets",
			int? maxWidth = null,
			int? maxHeight = null,
			CancellationToken cancellationToken = default)
		{
			if (stream == null || stream.Length == 0)
			{
				throw new ArgumentException("Image stream cannot be null or empty", nameof(stream));
			}

			var cleanFolder = string.IsNullOrWhiteSpace(folder) ? "trailblazers/site-assets" : folder.Trim();
			var rawName = Path.GetFileNameWithoutExtension(fileName) ?? "asset";
			var safeFileName = string.Join("_", rawName.Split(Path.GetInvalidFileNameChars()))
				.Replace(" ", "_")
				.ToLowerInvariant();
			if (string.IsNullOrWhiteSpace(safeFileName))
			{
				safeFileName = "asset";
			}

			var transformation = new Transformation()
				.Quality("auto")
				.FetchFormat("auto");

			if (maxWidth.HasValue || maxHeight.HasValue)
			{
				if (maxWidth.HasValue) transformation.Width(maxWidth.Value);
				if (maxHeight.HasValue) transformation.Height(maxHeight.Value);
				transformation.Crop("limit");
			}

			var uploadParams = new ImageUploadParams
			{
				File = new FileDescription(fileName, stream),
				Folder = cleanFolder,
				PublicId = $"{safeFileName}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
				Overwrite = false,
				Transformation = transformation
			};

			var uploadResult = await _cloudinary.UploadAsync(uploadParams, cancellationToken);

			if (uploadResult.Error != null)
			{
				_logger.LogError("Cloudinary media upload failed: {Error}", uploadResult.Error.Message);
				throw new InvalidOperationException($"Media upload failed: {uploadResult.Error.Message}");
			}

			_logger.LogInformation("Media uploaded successfully to {Folder}. PublicId: {PublicId}", cleanFolder, uploadResult.PublicId);
			return (uploadResult.SecureUrl?.ToString() ?? uploadResult.Url?.ToString() ?? string.Empty, uploadResult.PublicId);
		}

		public async Task<bool> DeleteImageAsync(string publicId, CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrWhiteSpace(publicId))
			{
				return false;
			}

			try
			{
				var deletionParams = new DeletionParams(publicId)
				{
					ResourceType = ResourceType.Image
				};

				var result = await _cloudinary.DestroyAsync(deletionParams);

				if (result.Result == "ok" || result.Result == "not found")
				{
					_logger.LogInformation("Cloudinary asset {PublicId} deleted successfully or was not found. Result: {Result}", publicId, result.Result);
					return true;
				}

				_logger.LogWarning("Cloudinary asset deletion returned unexpected result: {Result}", result.Result);
				return false;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to delete Cloudinary asset {PublicId}", publicId);
				return false;
			}
		}
	}
}
