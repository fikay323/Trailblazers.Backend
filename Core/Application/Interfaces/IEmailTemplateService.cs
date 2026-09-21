namespace Trailblazers.Backend.Core.Application.Interfaces
{
    public interface IEmailTemplateService
    {
        string RenderStaffInvitationEmail(
            string recipientName,
            string role,
            string inviteUrl,
            string invitedByName,
            string? logoUrl = null);

        string RenderAdminNewRegistrationAlertEmail(
            string studentName,
            string studentEmail,
            string studentPhone,
            string targetExam,
            string? guardianName,
            string? guardianPhone,
            string? guardianEmail,
            string? guardianRelationship,
            string reviewUrl,
            string? logoUrl = null);

        string RenderStudentAccountActivationEmail(
            string studentName,
            string targetExam,
            string activationUrl,
            string? logoUrl = null);

        string RenderPasswordResetEmail(
            string recipientName,
            string resetUrl,
            string? logoUrl = null);

        string RenderGuardianProgressReportEmail(
            string studentName,
            string guardianName,
            DateTimeOffset startDate,
            DateTimeOffset endDate,
            int totalTests,
            double averagePercentage,
            double highestPercentage,
            double passRate,
            int attendancePresent,
            int attendanceLate,
            string? customRemarks,
            string? portalUrl = null,
            string? logoUrl = null);

        string RenderAnnouncementBroadcastEmail(
            string recipientName,
            string title,
            string content,
            string priorityName,
            string authorName,
            DateTimeOffset publishedAt,
            string portalUrl,
            string? logoUrl = null);

        string RenderGuardianInquiryAdminAlertEmail(
            string studentName,
            string studentEmail,
            string guardianName,
            string guardianContact,
            string subject,
            string message,
            string? logoUrl = null);

        string RenderGuardianInquiryConfirmationEmail(
            string guardianName,
            string studentName,
            string subject,
            string message,
            string? logoUrl = null);
    }
}

