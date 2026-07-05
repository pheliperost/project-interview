using BlaInterview.Infrastructure.Services;

namespace BlaInterview.Unit.Tests.Infrastructure;

public class FakeEmailSenderTests
{
    [Fact(DisplayName = "SendPasswordResetAsync should capture email and link.")]
    [Trait("Category", "Fake Email Sender")]
    public async Task FakeEmailSender_Send_ShouldCapturePayload()
    {
        // Arrange
        var sender = new FakeEmailSender();

        // Act
        await sender.SendPasswordResetAsync("user@example.local", "http://localhost/reset", CancellationToken.None);

        // Assert
        Assert.Equal("user@example.local", sender.LastEmail);
        Assert.Equal("http://localhost/reset", sender.LastResetLink);
        Assert.Equal(1, sender.SendCount);
    }

    [Fact(DisplayName = "Clear should reset captured email state.")]
    [Trait("Category", "Fake Email Sender")]
    public async Task FakeEmailSender_Clear_ShouldResetState()
    {
        // Arrange
        var sender = new FakeEmailSender();
        await sender.SendPasswordResetAsync("user@example.local", "http://localhost/reset", CancellationToken.None);

        // Act
        sender.Clear();

        // Assert
        Assert.Null(sender.LastEmail);
        Assert.Null(sender.LastResetLink);
        Assert.Equal(0, sender.SendCount);
    }
}
