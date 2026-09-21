using System;
using System.Collections.Generic;

namespace Trailblazers.Backend.Core.Application.Features.GuardianPortal.Dtos
{
    public class GuardianAccessRequestDto
    {
        public string StudentEmail { get; set; } = string.Empty;
        public string GuardianContact { get; set; } = string.Empty; // Phone number or email
    }

    public class GuardianAccessResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? AccessToken { get; set; }
        public GuardianWardOverviewDto? WardOverview { get; set; }
    }

    public class GuardianWardOverviewDto
    {
        // Student Profile
        public string StudentName { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public string StudentPhone { get; set; } = string.Empty;
        public string TargetExam { get; set; } = string.Empty;
        public string Status { get; set; } = "Enrolled";
        public DateTimeOffset? EnrolledAt { get; set; }

        // Guardian Profile
        public string GuardianName { get; set; } = string.Empty;
        public string GuardianRelationship { get; set; } = string.Empty;
        public string GuardianPhone { get; set; } = string.Empty;
        public string GuardianEmail { get; set; } = string.Empty;

        // Academic Summary
        public int TotalExamsTaken { get; set; }
        public double AverageScorePercentage { get; set; }
        public double HighestScorePercentage { get; set; }
        public double PassRatePercentage { get; set; }
        public List<GuardianWardExamDto> RecentExams { get; set; } = [];

        // Physical Attendance Summary
        public int TotalAttendanceRecorded { get; set; }
        public int PresentDays { get; set; }
        public int LateDays { get; set; }
        public double AttendanceRatePercentage { get; set; }
        public List<GuardianWardAttendanceDto> RecentAttendance { get; set; } = [];

        // Active Academy Announcements
        public List<GuardianWardAnnouncementDto> Announcements { get; set; } = [];
    }

    public class GuardianWardExamDto
    {
        public Guid SessionId { get; set; }
        public int TargetYear { get; set; }
        public int TotalScore { get; set; }
        public int TotalQuestions { get; set; }
        public double Percentage { get; set; }
        public bool Passed { get; set; }
        public DateTimeOffset CompletedAt { get; set; }
    }

    public class GuardianWardAttendanceDto
    {
        public Guid Id { get; set; }
        public DateOnly Date { get; set; }
        public DateTimeOffset ClockInTime { get; set; }
        public DateTimeOffset? ClockOutTime { get; set; }
        public string Status { get; set; } = "Present";
        public string? Remarks { get; set; }
    }

    public class GuardianWardAnnouncementDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Priority { get; set; } = "General";
        public string TargetAudience { get; set; } = "All";
        public string AuthorName { get; set; } = "Academy Administration";
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class GuardianInquiryRequestDto
    {
        public string StudentEmail { get; set; } = string.Empty;
        public string? GuardianName { get; set; }
        public string? GuardianPhone { get; set; }
        public string? GuardianEmail { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class GuardianInquiryResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
