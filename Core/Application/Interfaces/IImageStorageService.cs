namespace Trailblazers.Backend.Core.Application.Interfaces
{
	public interface IImageStorageService
	{
		Task<(string Url, string PublicId)> UploadProfilePictureAsync(Stream stream, string fileName, string userId, CancellationToken cancellationToken = default);
		Task<bool> DeleteImageAsync(string publicId, CancellationToken cancellationToken = default);
	}
}
