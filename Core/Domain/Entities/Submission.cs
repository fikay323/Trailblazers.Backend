namespace Trailblazers.Backend.Core.Domain.Entities
{
    public class Submission
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public SubmissionType Type { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public string Metadata { get; set; } = "{}";
    }
}
