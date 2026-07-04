using BlaInterview.Application.Notifications;

namespace BlaInterview.Unit.Tests.Application;

public class NotificationTests
{
    [Fact(DisplayName = "Notifyer should report notifications after handle.")]
    [Trait("Category", "Notifications")]
    public void Notifyer_Handle_ShouldSetHasNotification()
    {
        var notifyer = new Notifyer();
        notifyer.Handle(new Notification("Test message"));
        Assert.True(notifyer.HasNotification());
    }

    [Fact(DisplayName = "Notifyer should collect multiple notifications.")]
    [Trait("Category", "Notifications")]
    public void Notifyer_MultipleHandles_ShouldCollectAll()
    {
        var notifyer = new Notifyer();
        notifyer.Handle(new Notification("First"));
        notifyer.Handle(new Notification("Second"));
        Assert.Equal(2, notifyer.GetNotifications().Count);
    }

    [Fact(DisplayName = "Notification should preserve status code.")]
    [Trait("Category", "Notifications")]
    public void Notification_StatusCode_ShouldBePreserved()
    {
        var notification = new Notification("Forbidden", 403);
        Assert.Equal(403, notification.StatusCode);
        Assert.Equal("Forbidden", notification.Message);
    }
}
