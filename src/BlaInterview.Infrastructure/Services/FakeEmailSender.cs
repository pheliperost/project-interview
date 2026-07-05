using BlaInterview.Application.Interfaces;

namespace BlaInterview.Infrastructure.Services;

/// <summary>
/// Captures password-reset "emails" for tests and demo diagnostics. No real mail is sent.
/// </summary>
public class FakeEmailSender : IEmailSender
{
    public string? LastEmail { get; private set; }
    public string? LastResetLink { get; private set; }
    public int SendCount { get; private set; }

    public Task SendPasswordResetAsync(string email, string resetLink, CancellationToken cancellationToken = default)
    {
        LastEmail = email;
        LastResetLink = resetLink;
        SendCount++;
        return Task.CompletedTask;
    }

    public void Clear()
    {
        LastEmail = null;
        LastResetLink = null;
        SendCount = 0;
    }
}
