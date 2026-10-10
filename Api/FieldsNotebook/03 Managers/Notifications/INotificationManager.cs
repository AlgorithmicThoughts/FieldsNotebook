using DataContract.Notifications;

namespace Managers.Notifications
{
    public interface INotificationManager
    {
        /// <exception cref="Common.Errors.ProjectAccessDeniedException">The user has no active access to the project.</exception>
        Task<UnreadNotificationCountResponse> GetUnreadCountAsync(int userId, int projectId, CancellationToken cancellationToken);
    }
}
