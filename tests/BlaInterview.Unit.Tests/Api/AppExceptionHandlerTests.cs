using System.Text.Json;
using BlaInterview.Api.Shared.Middleware;
using BlaInterview.Application.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace BlaInterview.Unit.Tests.Api;

public class AppExceptionHandlerTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact(DisplayName = "AppException should map to configured status code and message.")]
    [Trait("Category", "Exception Handler")]
    public async Task AppExceptionHandler_AppException_ShouldReturnConfiguredStatus()
    {
        // Arrange
        var handler = CreateHandler();
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new AppException("Email is already registered.", 409);

        // Act
        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        Assert.True(handled);
        Assert.Equal(409, context.Response.StatusCode);
        var body = await ReadErrorBodyAsync(context);
        Assert.Equal("Email is already registered.", body?["error"]);
    }

    [Fact(DisplayName = "UnauthorizedAccessException should return 401.")]
    [Trait("Category", "Exception Handler")]
    public async Task AppExceptionHandler_UnauthorizedAccess_ShouldReturn401()
    {
        // Arrange
        var handler = CreateHandler();
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        var handled = await handler.TryHandleAsync(context, new UnauthorizedAccessException(), CancellationToken.None);

        // Assert
        Assert.True(handled);
        Assert.Equal(401, context.Response.StatusCode);
        var body = await ReadErrorBodyAsync(context);
        Assert.Equal("Unauthorized.", body?["error"]);
    }

    [Fact(DisplayName = "Unknown exception should return 500.")]
    [Trait("Category", "Exception Handler")]
    public async Task AppExceptionHandler_UnknownException_ShouldReturn500()
    {
        // Arrange
        var handler = CreateHandler();
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        var handled = await handler.TryHandleAsync(context, new InvalidOperationException("boom"), CancellationToken.None);

        // Assert
        Assert.True(handled);
        Assert.Equal(500, context.Response.StatusCode);
        var body = await ReadErrorBodyAsync(context);
        Assert.Equal("An unexpected error occurred.", body?["error"]);
    }

    private static AppExceptionHandler CreateHandler()
    {
        var logger = new Mock<ILogger<AppExceptionHandler>>();
        return new AppExceptionHandler(logger.Object);
    }

    private static async Task<Dictionary<string, string>?> ReadErrorBodyAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        return await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(context.Response.Body, JsonOptions);
    }
}
