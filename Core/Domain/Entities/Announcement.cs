using System;

namespace Trailblazers.Backend.Core.Domain.Entities
{
    public enum AnnouncementPriority
    {
        General = 0,
        Urgent = 1,
        FeeReminder = 2,
        Holiday = 3,
        MockExamSchedule = 4
    }

    public class Announcement
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public AnnouncementPriority Priority { get; set; } = AnnouncementPriority.General;
        public string TargetAudience { get; set; } = "All"; // "All", "JAMB", "WAEC"
        public bool IsActive { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? ExpiresAt { get; set; }
        public string AuthorName { get; set; } = "Academy Administration";
        public Guid? AuthorId { get; set; }
        public bool SentEmailBroadcast { get; set; }
        public bool SentSmsBroadcast { get; set; }
    }
}
