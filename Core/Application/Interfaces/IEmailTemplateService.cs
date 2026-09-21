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
            string? logoUrl = null);
    }
}
