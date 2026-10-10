using Accessors.Notifications;
using Accessors.Projects;
using Common.Errors;
using Common.Notifications;
using DataContract.Notifications;

namespace Managers.Notifications
{
    public class NotificationManager(
        IProjectAccessAccessor projectAccessAccessor,
        INotificationAccessor notificationAccessor) : INotificationManager
    {
        public async Task<UnreadNotificationCountResponse> GetUnreadCountAsync(int userId, int projectId, CancellationToken cancellationToken)
        {
            await EnsureProjectAccessAsync(userId, projectId, cancellationToken);

            return new UnreadNotificationCountResponse(
                await notificationAccessor.CountUnreadAsync(userId, projectId, cancellationToken));
        }

        public async Task<NotificationListResponse> GetNotificationsAsync(int userId, int projectId, CancellationToken cancellationToken)
        {
            await EnsureProjectAccessAsync(userId, projectId, cancellationToken);

            var notices = await notificationAccessor.GetNotificationsAsync(userId, projectId, cancellationToken);

            return new NotificationListResponse(
                notices.Count(n => n.ReadAt is null),
                notices.Select(ToResponse).ToList());
        }

        public async Task<NotificationReadResponse> MarkReadAsync(int userId, int projectId, int notificationId, CancellationToken cancellationToken)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(notificationId, 1);
            await EnsureProjectAccessAsync(userId, projectId, cancellationToken);

            // Sequential on purpose: every call shares this request's DbContext.
            // An already read notice changes nothing and keeps its first ReadAt.
            await notificationAccessor.MarkReadAsync(userId, projectId, notificationId, DateTimeOffset.UtcNow, cancellationToken);
            var notice = await notificationAccessor.GetNotificationAsync(userId, projectId, notificationId, cancellationToken)
                ?? throw new NotificationNotFoundException(notificationId);
            var unreadCount = await notificationAccessor.CountUnreadAsync(userId, projectId, cancellationToken);

            return new NotificationReadResponse(ToResponse(notice), unreadCount);
        }

        private async Task EnsureProjectAccessAsync(int userId, int projectId, CancellationToken cancellationToken)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(userId, 1);
            ArgumentOutOfRangeException.ThrowIfLessThan(projectId, 1);

            _ = await projectAccessAccessor.GetProjectAccessAsync(userId, projectId, cancellationToken)
                ?? throw new ProjectAccessDeniedException(projectId);
        }

        private static NotificationResponse ToResponse(NotificationRecord n) =>
            new(n.Id, n.DelayId, n.DelayNumber, n.DelayTitle, n.EventType, n.CreatedAt, n.ReadAt);
    }
}
