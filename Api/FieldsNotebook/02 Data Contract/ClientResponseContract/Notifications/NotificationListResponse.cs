namespace DataContract.Notifications;

/// <summary>GET /api/projects/{projectId}/notifications — the list and the badge total from one request.</summary>
/// <param name="UnreadCount">Notices in <paramref name="Notices"/> with no ReadAt.</param>
/// <param name="Notices">Every notice for the recipient in the project, newest first.</param>
public sealed record NotificationListResponse(
    int UnreadCount,
    IReadOnlyList<NotificationResponse> Notices);
