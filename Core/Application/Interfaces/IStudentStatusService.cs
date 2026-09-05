namespace Trailblazers.Backend.Core.Application.Interfaces
{
    public interface IStudentStatusService
    {
        Task<(bool IsAllowed, string? Reason)> ValidateStudentAccessAsync(string email, CancellationToken cancellationToken = default);
    }
}
