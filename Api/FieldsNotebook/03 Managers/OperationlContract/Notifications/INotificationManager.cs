using Common.Results;
using DataContract.Notifications;

namespace Managers.Notifications;

public interface INotificationManager
{
    Task<Result<UnreadNotificationCountResponse>> GetUnreadCountAsync(int userId, int projectId, CancellationToken cancellationToken);

    /// <summary>Reads the recipient's notices and unread total without marking anything read.</summary>
    Task<Result<NotificationListResponse>> GetNotificationsAsync(int userId, int projectId, CancellationToken cancellationToken);

    /// <summary>Records the recipient's explicit open of one notice. The read grants no workflow permission.</summary>
    Task<Result<NotificationReadResponse>> MarkReadAsync(int userId, int projectId, int notificationId, CancellationToken cancellationToken);
}
