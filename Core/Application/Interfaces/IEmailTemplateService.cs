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
    }
}
