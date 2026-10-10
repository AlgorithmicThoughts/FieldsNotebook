namespace DataContract.Notifications
{
    /// <summary>One notice for the signed-in recipient. Clients build the title from the event type.</summary>
    /// <param name="EventType">ActivityEvents.EventType, such as delay.raised or delay.acknowledged.</param>
    /// <param name="ReadAt">UTC time the recipient opened the notice, or null while unread.</param>
    public sealed record NotificationResponse(
        int Id,
        int DelayId,
        string DelayNumber,
        string DelayTitle,
        string EventType,
        DateTimeOffset CreatedAt,
        DateTimeOffset? ReadAt);
}
