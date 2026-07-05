using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BlaInterview.Infrastructure.Options;
using BlaInterview.Infrastructure.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BlaInterview.Unit.Tests.Infrastructure;

public class JwtTokenServiceTests
{
    private const string TestSecret = "test-secret-key-at-least-32-chars-long!";
    private static readonly JwtTokenService Service = CreateService();

    [Fact(DisplayName = "CreateToken should return a non-empty token.")]
    [Trait("Category", "Jwt Token Service")]
    public void JwtTokenService_CreateToken_ShouldReturnNonEmptyToken()
    {
        // Act
        var response = Service.CreateToken("user-1", "user@example.local");

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(response.Token));
        Assert.Equal("user@example.local", response.Email);
    }

    [Fact(DisplayName = "CreateToken should include sub and email claims.")]
    [Trait("Category", "Jwt Token Service")]
    public void JwtTokenService_CreateToken_ShouldIncludeSubAndEmailClaims()
    {
        // Act
        var response = Service.CreateToken("user-1", "user@example.local");
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(response.Token);

        // Assert
        Assert.Equal("user-1", jwt.Subject);
        Assert.Equal("user@example.local", jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
    }

    [Fact(DisplayName = "CreateToken should set expiry from settings.")]
    [Trait("Category", "Jwt Token Service")]
    public void JwtTokenService_CreateToken_ShouldSetExpiryFromSettings()
    {
        // Arrange
        var service = CreateService(expiryMinutes: 30);
        var before = DateTimeOffset.UtcNow.AddMinutes(30);

        // Act
        var response = service.CreateToken("user-1", "user@example.local");

        // Assert
        Assert.True(response.ExpiresAt >= before.AddSeconds(-5));
        Assert.True(response.ExpiresAt <= DateTimeOffset.UtcNow.AddMinutes(30).AddSeconds(5));
    }

    [Fact(DisplayName = "CreateToken should use configured issuer and audience.")]
    [Trait("Category", "Jwt Token Service")]
    public void JwtTokenService_CreateToken_ShouldUseConfiguredIssuerAndAudience()
    {
        // Act
        var response = Service.CreateToken("user-1", "user@example.local");
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(response.Token);

        // Assert
        Assert.Equal("TestIssuer", jwt.Issuer);
        Assert.Contains("TestAudience", jwt.Audiences);
    }

    [Fact(DisplayName = "CreateToken should produce unique jti per call.")]
    [Trait("Category", "Jwt Token Service")]
    public void JwtTokenService_CreateToken_TwoCalls_ShouldProduceDifferentJti()
    {
        // Act
        var first = Service.CreateToken("user-1", "user@example.local");
        var second = Service.CreateToken("user-1", "user@example.local");
        var firstJti = new JwtSecurityTokenHandler().ReadJwtToken(first.Token)
            .Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        var secondJti = new JwtSecurityTokenHandler().ReadJwtToken(second.Token)
            .Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

        // Assert
        Assert.NotEqual(firstJti, secondJti);
    }

    [Fact(DisplayName = "CreateToken should validate with the same signing key.")]
    [Trait("Category", "Jwt Token Service")]
    public void JwtTokenService_CreateToken_ShouldValidateWithSameSecret()
    {
        // Arrange
        var response = Service.CreateToken("user-1", "user@example.local");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret));
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "TestIssuer",
            ValidateAudience = true,
            ValidAudience = "TestAudience",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        // Act
        var principal = new JwtSecurityTokenHandler().ValidateToken(response.Token, parameters, out _);

        // Assert
        var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Assert.Equal("user-1", sub);
    }

    [Fact(DisplayName = "CreateToken should not validate with a different signing key.")]
    [Trait("Category", "Jwt Token Service")]
    public void JwtTokenService_CreateToken_WrongSecret_ShouldFailValidation()
    {
        // Arrange
        var response = Service.CreateToken("user-1", "user@example.local");
        var wrongKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("wrong-secret-key-at-least-32-chars!!"));
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "TestIssuer",
            ValidateAudience = true,
            ValidAudience = "TestAudience",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = wrongKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        // Act & Assert
        Assert.ThrowsAny<Exception>(() =>
            new JwtSecurityTokenHandler().ValidateToken(response.Token, parameters, out _));
    }

    [Fact(DisplayName = "CreateToken with short secret should throw.")]
    [Trait("Category", "Jwt Token Service")]
    public void JwtTokenService_CreateToken_ShortSecret_ShouldThrow()
    {
        // Arrange
        var service = CreateService(secret: "short");

        // Act & Assert
        Assert.ThrowsAny<Exception>(() => service.CreateToken("user-1", "user@example.local"));
    }

    private static JwtTokenService CreateService(int expiryMinutes = 60, string secret = TestSecret) =>
        new(Options.Create(new JwtSettings
        {
            Secret = secret,
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpiryMinutes = expiryMinutes
        }));
}
