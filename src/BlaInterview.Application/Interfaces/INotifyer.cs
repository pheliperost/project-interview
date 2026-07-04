using BlaInterview.Application.Notifications;

namespace BlaInterview.Application.Interfaces;

public interface INotifyer
{
    bool HasNotification();
    List<Notification> GetNotifications();
    void Handle(Notification notification);
}
