using DataContract.Notifications;

namespace Managers.Notifications
{
    public interface INotificationManager
    {
        /// <exception cref="Common.Errors.ProjectAccessDeniedException">The user has no active access to the project.</exception>
        Task<UnreadNotificationCountResponse> GetUnreadCountAsync(int userId, int projectId, CancellationToken cancellationToken);

        /// <summary>Reads the recipient's notices and unread total without marking anything read.</summary>
        /// <exception cref="Common.Errors.ProjectAccessDeniedException">The user has no active access to the project.</exception>
        Task<NotificationListResponse> GetNotificationsAsync(int userId, int projectId, CancellationToken cancellationToken);

        /// <summary>Records the recipient's explicit open of one notice. The read grants no workflow permission.</summary>
        /// <exception cref="Common.Errors.ProjectAccessDeniedException">The user has no active access to the project.</exception>
        /// <exception cref="Common.Errors.NotificationNotFoundException">The notice is not the user's in this project.</exception>
        Task<NotificationReadResponse> MarkReadAsync(int userId, int projectId, int notificationId, CancellationToken cancellationToken);
    }
}
