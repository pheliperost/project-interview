using BlaInterview.Application.Interfaces;

namespace BlaInterview.Application.Notifications;

public class Notifyer : INotifyer
{
    private readonly List<Notification> _notifications = [];

    public void Handle(Notification notification)
    {
        _notifications.Add(notification);
    }

    public List<Notification> GetNotifications()
    {
        return _notifications;
    }

    public bool HasNotification()
    {
        return _notifications.Count > 0;
    }
}
