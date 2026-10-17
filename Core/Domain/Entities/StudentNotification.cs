using System;

namespace Trailblazers.Backend.Core.Domain.Entities
{
    public class StudentNotification
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid StudentId { get; set; }
        public string StudentEmail { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "General";
        public bool IsRead { get; set; } = false;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public string? CreatedBy { get; set; }
    }
}
