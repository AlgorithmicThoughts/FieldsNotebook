namespace DataContract.Notifications
{
    /// <summary>POST .../notifications/{notificationId}/read — the opened notice and the recipient's new unread total.</summary>
    public sealed record NotificationReadResponse(NotificationResponse Notice, int UnreadCount);
}
