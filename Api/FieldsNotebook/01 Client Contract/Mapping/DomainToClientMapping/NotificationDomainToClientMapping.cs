using DataContract.Domain.Notifications;
using DataContract.Notifications;

namespace DataContract.Mapping.DomainToClientMapping;

public static class NotificationDomainToClientMapping
{
    public static NotificationResponse ToResponse(this NotificationRecord notice)
    {
        return new NotificationResponse(notice.Id, notice.DelayId, notice.DelayNumber, notice.DelayTitle, notice.EventType,
            notice.CreatedAt, notice.ReadAt);
    }

    /// <summary>The list and its badge total; a notice is unread while it has no ReadAt.</summary>
    public static NotificationListResponse ToListResponse(this IEnumerable<NotificationRecord>? notices)
    {
        var responses = notices.MapAll(ToResponse);
        return new NotificationListResponse(responses.Count(n => n.ReadAt is null), responses);
    }

    public static NotificationReadResponse ToReadResponse(this NotificationRecord notice, int unreadCount)
    {
        return new NotificationReadResponse(notice.ToResponse(), unreadCount);
    }

    public static UnreadNotificationCountResponse ToUnreadCountResponse(int unreadCount)
    {
        return new UnreadNotificationCountResponse(unreadCount);
    }
}
