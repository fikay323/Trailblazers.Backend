namespace Trailblazers.Backend.Core.Application.Interfaces
{
    public interface IMailService
    {
        Task SendEmailAsync(string to, string subject, string body);
    }
}
