using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Trailblazers.Backend.Core.Application.Features.Blog.Dtos;
using Trailblazers.Backend.Core.Application.Features.Blog.Interfaces;
using Trailblazers.Backend.Core.Domain.Entities;
using Trailblazers.Backend.Infrastructure.Persistence;

namespace Trailblazers.Backend.Infrastructure.Services
{
    public class BlogService : IBlogService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<BlogService> _logger;

        public BlogService(ApplicationDbContext dbContext, ILogger<BlogService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<BlogCatalogResponseDto> GetPublishedPostsAsync(
            int page,
            int pageSize,
            string? category,
            string? search,
            CancellationToken cancellationToken = default)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);

            var query = _dbContext.BlogPosts
                .AsNoTracking()
                .Where(p => p.IsPublished);

            if (!string.IsNullOrWhiteSpace(category) && !category.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p => p.Category.ToLower() == category.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(p => p.Title.ToLower().Contains(s) || p.Excerpt.ToLower().Contains(s));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new BlogPostCardDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Slug = p.Slug,
                    Excerpt = p.Excerpt,
                    CoverImageUrl = p.CoverImageUrl,
                    Category = p.Category,
                    Tags = p.Tags,
                    AuthorName = p.AuthorName,
                    IsPublished = p.IsPublished,
                    PublishedAt = p.PublishedAt,
                    CreatedAt = p.CreatedAt,
                    ViewCount = p.ViewCount
                })
                .ToListAsync(cancellationToken);

            var categories = await _dbContext.BlogPosts
                .AsNoTracking()
                .Where(p => p.IsPublished)
                .Select(p => p.Category)
                .Distinct()
                .ToListAsync(cancellationToken);

            return new BlogCatalogResponseDto
            {
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize,
                Items = items,
                Categories = categories
            };
        }

        public async Task<BlogPostDto?> GetPostBySlugAsync(
            string slug,
            bool incrementViews = true,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(slug)) return null;

            var normalizedSlug = slug.Trim().ToLowerInvariant();
            var post = await _dbContext.BlogPosts
                .FirstOrDefaultAsync(p => p.Slug == normalizedSlug, cancellationToken);

            if (post == null) return null;

            if (incrementViews)
            {
                post.ViewCount += 1;
                try
                {
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to increment view count for blog post {Slug}", slug);
                }
            }

            return MapToDto(post);
        }

        public async Task<BlogCatalogResponseDto> GetAdminPostsAsync(
            int page,
            int pageSize,
            bool? isPublished,
            string? category,
            string? search,
            CancellationToken cancellationToken = default)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = _dbContext.BlogPosts.AsNoTracking().AsQueryable();

            if (isPublished.HasValue)
            {
                query = query.Where(p => p.IsPublished == isPublished.Value);
            }

            if (!string.IsNullOrWhiteSpace(category) && !category.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p => p.Category.ToLower() == category.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(p => p.Title.ToLower().Contains(s)
                                      || p.Excerpt.ToLower().Contains(s)
                                      || p.AuthorName.ToLower().Contains(s)
                                      || p.Slug.ToLower().Contains(s));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new BlogPostCardDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Slug = p.Slug,
                    Excerpt = p.Excerpt,
                    CoverImageUrl = p.CoverImageUrl,
                    Category = p.Category,
                    Tags = p.Tags,
                    AuthorName = p.AuthorName,
                    IsPublished = p.IsPublished,
                    PublishedAt = p.PublishedAt,
                    CreatedAt = p.CreatedAt,
                    ViewCount = p.ViewCount
                })
                .ToListAsync(cancellationToken);

            var categories = await _dbContext.BlogPosts
                .AsNoTracking()
                .Select(p => p.Category)
                .Distinct()
                .ToListAsync(cancellationToken);

            return new BlogCatalogResponseDto
            {
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize,
                Items = items,
                Categories = categories
            };
        }

        public async Task<BlogPostDto?> GetPostByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var post = await _dbContext.BlogPosts
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

            return post == null ? null : MapToDto(post);
        }

        public async Task<BlogPostDto> CreatePostAsync(
            CreateBlogPostRequestDto request,
            string authorName,
            Guid? authorId,
            CancellationToken cancellationToken = default)
        {
            var baseSlug = !string.IsNullOrWhiteSpace(request.Slug)
                ? GenerateSlug(request.Slug)
                : GenerateSlug(request.Title);

            var uniqueSlug = await EnsureUniqueSlugAsync(baseSlug, null, cancellationToken);

            var now = DateTimeOffset.UtcNow;
            var post = new BlogPost
            {
                Id = Guid.NewGuid(),
                Title = request.Title.Trim(),
                Slug = uniqueSlug,
                Excerpt = request.Excerpt.Trim(),
                Content = request.Content,
                CoverImageUrl = string.IsNullOrWhiteSpace(request.CoverImageUrl) ? null : request.CoverImageUrl.Trim(),
                Category = string.IsNullOrWhiteSpace(request.Category) ? "JAMB & UTME Tips" : request.Category.Trim(),
                Tags = request.Tags ?? [],
                AuthorName = string.IsNullOrWhiteSpace(authorName) ? "Academy Administration" : authorName,
                AuthorId = authorId,
                IsPublished = request.IsPublished,
                PublishedAt = request.IsPublished ? now : null,
                CreatedAt = now,
                UpdatedAt = null,
                ViewCount = 0
            };

            _dbContext.BlogPosts.Add(post);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Blog post created: {Title} ({Slug}) by {Author}", post.Title, post.Slug, post.AuthorName);

            return MapToDto(post);
        }

        public async Task<BlogPostDto?> UpdatePostAsync(
            Guid id,
            UpdateBlogPostRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var post = await _dbContext.BlogPosts.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
            if (post == null) return null;

            if (!string.IsNullOrWhiteSpace(request.Title))
            {
                post.Title = request.Title.Trim();
            }

            if (!string.IsNullOrWhiteSpace(request.Slug))
            {
                var baseSlug = GenerateSlug(request.Slug);
                post.Slug = await EnsureUniqueSlugAsync(baseSlug, id, cancellationToken);
            }
            else if (!string.IsNullOrWhiteSpace(request.Title) && string.IsNullOrWhiteSpace(post.Slug))
            {
                var baseSlug = GenerateSlug(request.Title);
                post.Slug = await EnsureUniqueSlugAsync(baseSlug, id, cancellationToken);
            }

            if (request.Excerpt != null)
            {
                post.Excerpt = request.Excerpt.Trim();
            }

            if (request.Content != null)
            {
                post.Content = request.Content;
            }

            if (request.CoverImageUrl != null)
            {
                post.CoverImageUrl = string.IsNullOrWhiteSpace(request.CoverImageUrl) ? null : request.CoverImageUrl.Trim();
            }

            if (!string.IsNullOrWhiteSpace(request.Category))
            {
                post.Category = request.Category.Trim();
            }

            if (request.Tags != null)
            {
                post.Tags = request.Tags;
            }

            if (request.IsPublished.HasValue)
            {
                if (request.IsPublished.Value && !post.IsPublished)
                {
                    post.PublishedAt = DateTimeOffset.UtcNow;
                }
                else if (!request.IsPublished.Value && post.IsPublished)
                {
                    post.PublishedAt = null;
                }
                post.IsPublished = request.IsPublished.Value;
            }

            post.UpdatedAt = DateTimeOffset.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Blog post updated: {Title} ({Slug})", post.Title, post.Slug);

            return MapToDto(post);
        }

        public async Task<bool> DeletePostAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var post = await _dbContext.BlogPosts.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
            if (post == null) return false;

            _dbContext.BlogPosts.Remove(post);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Blog post deleted: {Title} ({Id})", post.Title, id);
            return true;
        }

        private static BlogPostDto MapToDto(BlogPost post)
        {
            return new BlogPostDto
            {
                Id = post.Id,
                Title = post.Title,
                Slug = post.Slug,
                Excerpt = post.Excerpt,
                Content = post.Content,
                CoverImageUrl = post.CoverImageUrl,
                Category = post.Category,
                Tags = post.Tags,
                AuthorName = post.AuthorName,
                AuthorId = post.AuthorId,
                IsPublished = post.IsPublished,
                PublishedAt = post.PublishedAt,
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt,
                ViewCount = post.ViewCount
            };
        }

        private static string GenerateSlug(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return Guid.NewGuid().ToString("N")[..8];

            var slug = text.ToLowerInvariant().Trim();
            // Remove all accents/diacritics or non-alphanumeric chars (except dash/space)
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
            // Replace multiple spaces or hyphens with a single hyphen
            slug = Regex.Replace(slug, @"[\s-]+", "-").Trim('-');

            return string.IsNullOrWhiteSpace(slug) ? Guid.NewGuid().ToString("N")[..8] : slug;
        }

        private async Task<string> EnsureUniqueSlugAsync(string baseSlug, Guid? existingId, CancellationToken cancellationToken)
        {
            var slug = baseSlug;
            var counter = 1;

            while (await _dbContext.BlogPosts.AnyAsync(p => p.Slug == slug && (existingId == null || p.Id != existingId), cancellationToken))
            {
                slug = $"{baseSlug}-{counter}";
                counter++;
            }

            return slug;
        }
    }
}
