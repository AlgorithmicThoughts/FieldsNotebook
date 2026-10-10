namespace DataContract.Notifications
{
    /// <summary>Notices for the user in the project that the user has not opened.</summary>
    public sealed record UnreadNotificationCountResponse(int UnreadCount);
}
