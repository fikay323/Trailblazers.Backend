using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Trailblazers.Backend.Core.Application.Interfaces
{
	public interface IImageStorageService
	{
		Task<(string Url, string PublicId)> UploadProfilePictureAsync(Stream stream, string fileName, string userId, CancellationToken cancellationToken = default);
		Task<(string Url, string PublicId)> UploadMediaAsync(Stream stream, string fileName, string folder = "trailblazers/site-assets", int? maxWidth = null, int? maxHeight = null, CancellationToken cancellationToken = default);
		Task<bool> DeleteImageAsync(string publicId, CancellationToken cancellationToken = default);
	}
}
