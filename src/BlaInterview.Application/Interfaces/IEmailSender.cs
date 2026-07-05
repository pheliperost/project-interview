using BlaInterview.Application.DTOs;

namespace BlaInterview.Application.Interfaces;

public interface IEmailSender
{
    Task SendPasswordResetAsync(string email, string resetLink, CancellationToken cancellationToken = default);
}
