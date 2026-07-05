namespace BlaInterview.Infrastructure.Options;

public class PasswordResetOptions
{
    public const string SectionName = "PasswordReset";
    public string ClientBaseUrl { get; set; } = "http://localhost:5173";
}
