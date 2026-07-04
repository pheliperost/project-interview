namespace BlaInterview.Infrastructure.Options;

public class JwtSettings
{
    public const string SectionName = "Jwt";
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "SimpleTasks";
    public string Audience { get; set; } = "SimpleTasks";
    public int ExpiryMinutes { get; set; } = 60;
}
