namespace Trailblazers.Backend.Core.Domain.Entities
{
    public enum AttendanceStatus
    {
        Present = 1,
        Late = 2,
        Absent = 3,
        Excused = 4
    }

    public enum AttendanceVerificationType
    {
        Geolocated = 1,
        ManualStaff = 2
    }

    public class AttendanceRecord
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid StudentId { get; set; }
        public ApplicationUser Student { get; set; } = null!;
        public DateOnly Date { get; set; }
        public DateTimeOffset ClockInTime { get; set; } = DateTimeOffset.UtcNow;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? AccuracyMeters { get; set; }
        public double? DistanceMeters { get; set; }
        public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;
        public AttendanceVerificationType VerificationType { get; set; } = AttendanceVerificationType.Geolocated;
        public Guid? MarkedByUserId { get; set; }
        public string? MarkedByUserName { get; set; }
        public string? Remarks { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
