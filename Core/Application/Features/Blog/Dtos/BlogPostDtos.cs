using System;
using System.Collections.Generic;

namespace Trailblazers.Backend.Core.Application.Features.Blog.Dtos
{
    public class BlogPostDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Excerpt { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? CoverImageUrl { get; set; }
        public string Category { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = [];
        public string AuthorName { get; set; } = string.Empty;
        public Guid? AuthorId { get; set; }
        public bool IsPublished { get; set; }
        public DateTimeOffset? PublishedAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public int ViewCount { get; set; }
    }

    public class BlogPostCardDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Excerpt { get; set; } = string.Empty;
        public string? CoverImageUrl { get; set; }
        public string Category { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = [];
        public string AuthorName { get; set; } = string.Empty;
        public bool IsPublished { get; set; }
        public DateTimeOffset? PublishedAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public int ViewCount { get; set; }
    }

    public class CreateBlogPostRequestDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public string Excerpt { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? CoverImageUrl { get; set; }
        public string Category { get; set; } = "JAMB & UTME Tips";
        public List<string>? Tags { get; set; }
        public bool IsPublished { get; set; } = false;
    }

    public class UpdateBlogPostRequestDto
    {
        public string? Title { get; set; }
        public string? Slug { get; set; }
        public string? Excerpt { get; set; }
        public string? Content { get; set; }
        public string? CoverImageUrl { get; set; }
        public string? Category { get; set; }
        public List<string>? Tags { get; set; }
        public bool? IsPublished { get; set; }
    }

    public class BlogCatalogResponseDto
    {
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public List<BlogPostCardDto> Items { get; set; } = [];
        public List<string> Categories { get; set; } = [];
    }
}
