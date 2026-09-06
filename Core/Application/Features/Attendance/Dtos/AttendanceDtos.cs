using Trailblazers.Backend.Core.Domain.Entities;

namespace Trailblazers.Backend.Core.Application.Features.Attendance.Dtos
{
    public class ClockInRequestDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double AccuracyMeters { get; set; }
        public DateTimeOffset ClientTimestamp { get; set; }
    }

    public class AttendanceRecordDto
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public string? StudentPhone { get; set; }
        public DateOnly Date { get; set; }
        public DateTimeOffset ClockInTime { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? AccuracyMeters { get; set; }
        public double? DistanceMeters { get; set; }
        public AttendanceStatus Status { get; set; }
        public AttendanceVerificationType VerificationType { get; set; }
        public string? MarkedByUserName { get; set; }
        public string? Remarks { get; set; }
    }

    public class RosterStudentItemDto
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; }
        public AttendanceStatus Status { get; set; }
        public DateTimeOffset? ClockInTime { get; set; }
        public double? DistanceMeters { get; set; }
        public double? AccuracyMeters { get; set; }
        public AttendanceVerificationType? VerificationType { get; set; }
        public string? MarkedByUserName { get; set; }
        public string? Remarks { get; set; }
        public Guid? AttendanceRecordId { get; set; }
    }

    public class DailyRosterResponseDto
    {
        public DateOnly Date { get; set; }
        public int TotalEnrolled { get; set; }
        public int PresentCount { get; set; }
        public int LateCount { get; set; }
        public int AbsentCount { get; set; }
        public int ExcusedCount { get; set; }
        public List<RosterStudentItemDto> Students { get; set; } = [];
    }

    public class AttendanceOverrideRequestDto
    {
        public Guid StudentId { get; set; }
        public DateOnly Date { get; set; }
        public AttendanceStatus Status { get; set; }
        public string? Remarks { get; set; }
    }

    public class AttendanceSettingDto
    {
        public double CenterLatitude { get; set; }
        public double CenterLongitude { get; set; }
        public double AllowedRadiusMeters { get; set; }
        public double MaxAllowedAccuracyMeters { get; set; }
        public string EarliestClockInTime { get; set; } = "07:00";
        public string LateCutoffTime { get; set; } = "08:30";
        public string LatestClockInTime { get; set; } = "13:00";
        public DateTimeOffset UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }

    public class StudentAttendanceStatsDto
    {
        public DateOnly Date { get; set; }
        public bool HasClockedInToday { get; set; }
        public AttendanceRecordDto? TodayRecord { get; set; }
        public int TotalDays { get; set; }
        public int PresentDays { get; set; }
        public int LateDays { get; set; }
        public int AbsentDays { get; set; }
        public double AttendanceRate { get; set; }
        public int PunctualStreak { get; set; }
    }
}
