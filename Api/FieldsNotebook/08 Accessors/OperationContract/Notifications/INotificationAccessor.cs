using DataContract.Domain.Notifications;

namespace Accessors.Notifications;

public interface INotificationAccessor : IAccessor
{
    /// <summary>Counts the recipient's notices in the project with no ReadAt.</summary>
    Task<int> CountUnreadAsync(int userId, int projectId, CancellationToken cancellationToken);

    /// <summary>Every notice for the recipient in the project, newest first.</summary>
    Task<IReadOnlyList<NotificationRecord>> GetNotificationsAsync(int userId, int projectId, CancellationToken cancellationToken);

    /// <summary>One of the recipient's notices in the project, or null when it is not theirs.</summary>
    Task<NotificationRecord?> GetNotificationAsync(int userId, int projectId, int notificationId, CancellationToken cancellationToken);

    /// <summary>
    /// Sets ReadAt on the recipient's notice only while it is still unread, so a repeated read keeps the first time.
    /// Returns false when nothing changed.
    /// </summary>
    Task<bool> MarkReadAsync(int userId, int projectId, int notificationId, DateTimeOffset readAt, CancellationToken cancellationToken);
}
