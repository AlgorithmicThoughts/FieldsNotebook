using Accessors.Notifications;
using Accessors.Projects;
using Common.Errors;
using DataContract.Notifications;

namespace Managers.Notifications
{
    public class NotificationManager(
        IProjectAccessAccessor projectAccessAccessor,
        INotificationAccessor notificationAccessor) : INotificationManager
    {
        public async Task<UnreadNotificationCountResponse> GetUnreadCountAsync(int userId, int projectId, CancellationToken cancellationToken)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(userId, 1);
            ArgumentOutOfRangeException.ThrowIfLessThan(projectId, 1);

            _ = await projectAccessAccessor.GetProjectAccessAsync(userId, projectId, cancellationToken)
                ?? throw new ProjectAccessDeniedException(projectId);

            return new UnreadNotificationCountResponse(
                await notificationAccessor.CountUnreadAsync(userId, projectId, cancellationToken));
        }
    }
}
