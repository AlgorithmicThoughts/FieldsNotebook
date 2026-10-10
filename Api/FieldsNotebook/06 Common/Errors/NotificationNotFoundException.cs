namespace Common.Errors
{
    /// <summary>The notice does not exist, or it belongs to another recipient or project.</summary>
    public sealed class NotificationNotFoundException(int notificationId)
        : Exception($"Notification {notificationId} is not available to the user in this project.")
    {
        public int NotificationId { get; } = notificationId;
    }
}
