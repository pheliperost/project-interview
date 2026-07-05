using System.IdentityModel.Tokens.Jwt;
using BlaInterview.Infrastructure.Options;
using BlaInterview.Infrastructure.Services;
using Microsoft.Extensions.Options;

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

    private static JwtTokenService CreateService(int expiryMinutes = 60) =>
        new(Options.Create(new JwtSettings
        {
            Secret = TestSecret,
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpiryMinutes = expiryMinutes
        }));
}
