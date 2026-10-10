using DataContract.Upcoming;

namespace Managers.Upcoming
{
    public interface IUpcomingManager
    {
        /// <exception cref="Common.Errors.ProjectAccessDeniedException">The user has no active access to the project.</exception>
        Task<UpcomingResponse> GetUpcomingAsync(int userId, int projectId, CancellationToken cancellationToken);
    }
}
