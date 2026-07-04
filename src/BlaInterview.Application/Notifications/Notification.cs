namespace BlaInterview.Application.Notifications;

public class Notification
{
    public Notification(string message, int statusCode = 400)
    {
        Message = message;
        StatusCode = statusCode;
    }

    public string Message { get; }
    public int StatusCode { get; }
}
