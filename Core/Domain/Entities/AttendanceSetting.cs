namespace Trailblazers.Backend.Core.Domain.Entities
{
    public class AttendanceSetting
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public double CenterLatitude { get; set; } = 6.5244; // Default: Lagos, Nigeria (Configurable by Admin)
        public double CenterLongitude { get; set; } = 3.3792;
        public double AllowedRadiusMeters { get; set; } = 100.0;
        public double MaxAllowedAccuracyMeters { get; set; } = 80.0;
        public TimeOnly EarliestClockInTime { get; set; } = new(7, 0); // 7:00 AM
        public TimeOnly LateCutoffTime { get; set; } = new(8, 30); // 8:30 AM
        public TimeOnly LatestClockInTime { get; set; } = new(13, 0); // 1:00 PM
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
        public string? UpdatedBy { get; set; }
    }
}
