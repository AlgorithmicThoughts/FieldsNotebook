namespace Common.Notifications
{
    /// <summary>One Notifications row with the delay and event it concerns.</summary>
    /// <param name="EventType">ActivityEvents.EventType of the event that caused the notice.</param>
    /// <param name="ReadAt">Set only when the recipient explicitly opened the notice; null means unread.</param>
    public sealed record NotificationRecord(
        int Id,
        int DelayId,
        string DelayNumber,
        string DelayTitle,
        string EventType,
        DateTimeOffset CreatedAt,
        DateTimeOffset? ReadAt);
}
