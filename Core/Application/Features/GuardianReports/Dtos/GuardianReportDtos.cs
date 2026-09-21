using System;
using System.Collections.Generic;

namespace Trailblazers.Backend.Core.Application.Features.GuardianReports.Dtos
{
    public class GuardianReportPreviewDto
    {
        public string StudentName { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public string GuardianName { get; set; } = string.Empty;
        public string GuardianPhone { get; set; } = string.Empty;
        public string GuardianEmail { get; set; } = string.Empty;
        public string GuardianRelationship { get; set; } = string.Empty;
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
        public int TotalTestsTaken { get; set; }
        public double AveragePercentage { get; set; }
        public double HighestPercentage { get; set; }
        public double PassRatePercentage { get; set; }
        public int AttendancePresentDays { get; set; }
        public int AttendanceLateDays { get; set; }
        public List<GuardianReportAttemptDto> Attempts { get; set; } = [];
    }

    public class GuardianReportAttemptDto
    {
        public Guid SessionId { get; set; }
        public int TargetYear { get; set; }
        public int TotalScore { get; set; }
        public int TotalQuestions { get; set; }
        public double Percentage { get; set; }
        public DateTimeOffset CompletedAt { get; set; }
    }

    public class SendGuardianReportRequestDto
    {
        public string StudentEmail { get; set; } = string.Empty;
        public string? GuardianName { get; set; }
        public string? GuardianPhone { get; set; }
        public string? GuardianEmail { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
        public string Channel { get; set; } = "Email"; // "Email" | "Sms" | "Both"
        public string? CustomRemarks { get; set; }
    }

    public class SendGuardianReportResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool EmailSent { get; set; }
        public bool SmsSent { get; set; }
        public string DeliveredTo { get; set; } = string.Empty;
        public GuardianReportPreviewDto? ReportSummary { get; set; }
    }
}
