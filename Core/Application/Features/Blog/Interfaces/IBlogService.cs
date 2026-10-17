using System;
using System.Threading;
using System.Threading.Tasks;
using Trailblazers.Backend.Core.Application.Features.Blog.Dtos;

namespace Trailblazers.Backend.Core.Application.Features.Blog.Interfaces
{
    public interface IBlogService
    {
        Task<BlogCatalogResponseDto> GetPublishedPostsAsync(
            int page,
            int pageSize,
            string? category,
            string? search,
            CancellationToken cancellationToken = default);

        Task<BlogPostDto?> GetPostBySlugAsync(
            string slug,
            bool incrementViews = true,
            CancellationToken cancellationToken = default);

        Task<BlogCatalogResponseDto> GetAdminPostsAsync(
            int page,
            int pageSize,
            bool? isPublished,
            string? category,
            string? search,
            CancellationToken cancellationToken = default);

        Task<BlogPostDto?> GetPostByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        Task<BlogPostDto> CreatePostAsync(
            CreateBlogPostRequestDto request,
            string authorName,
            Guid? authorId,
            CancellationToken cancellationToken = default);

        Task<BlogPostDto?> UpdatePostAsync(
            Guid id,
            UpdateBlogPostRequestDto request,
            CancellationToken cancellationToken = default);

        Task<bool> DeletePostAsync(
            Guid id,
            CancellationToken cancellationToken = default);
    }
}
