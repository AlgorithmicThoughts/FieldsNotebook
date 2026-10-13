namespace Common.Errors;

public static class NotificationErrors
{
    /// <summary>The notice does not exist, or it belongs to another recipient or project.</summary>
    public static RequestError NotFound(int notificationId) =>
        RequestError.NotFound($"Notification {notificationId} is not available to the user in this project.");
}
