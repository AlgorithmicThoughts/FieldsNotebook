using DataContract.Home;

namespace Managers.Home
{
    public interface IHomeManager
    {
        /// <exception cref="Common.Errors.ProjectAccessDeniedException">The user has no active access to the project.</exception>
        Task<HomeResponse> GetHomeAsync(int userId, int projectId, CancellationToken cancellationToken);
    }
}
