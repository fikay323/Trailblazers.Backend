using System;
using System.Collections.Generic;
using Trailblazers.Backend.Core.Domain.Entities;

namespace Trailblazers.Backend.Core.Application.Features.Announcements.Dtos
{
    public class AnnouncementDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public AnnouncementPriority Priority { get; set; }
        public string PriorityName => Priority.ToString();
        public string TargetAudience { get; set; } = "All";
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public bool SentEmailBroadcast { get; set; }
        public bool SentSmsBroadcast { get; set; }
    }

    public class CreateAnnouncementRequestDto
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public AnnouncementPriority Priority { get; set; } = AnnouncementPriority.General;
        public string TargetAudience { get; set; } = "All";
        public DateTimeOffset? ExpiresAt { get; set; }
        public bool SendEmailBroadcast { get; set; }
        public bool SendSmsBroadcast { get; set; }
    }

    public class UpdateAnnouncementRequestDto
    {
        public string? Title { get; set; }
        public string? Content { get; set; }
        public AnnouncementPriority? Priority { get; set; }
        public string? TargetAudience { get; set; }
        public bool? IsActive { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
    }

    public class AnnouncementsListResponseDto
    {
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public List<AnnouncementDto> Items { get; set; } = [];
    }
}
