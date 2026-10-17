using System;
using System.Collections.Generic;

namespace Trailblazers.Backend.Core.Domain.Entities
{
    public class BlogPost
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Excerpt { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? CoverImageUrl { get; set; }
        public string Category { get; set; } = "JAMB & UTME Tips";
        public List<string> Tags { get; set; } = [];
        public string AuthorName { get; set; } = "Academy Administration";
        public Guid? AuthorId { get; set; }
        public bool IsPublished { get; set; } = false;
        public DateTimeOffset? PublishedAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public int ViewCount { get; set; } = 0;
    }
}
