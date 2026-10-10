using Common.Projects;

namespace Accessors.Projects
{
    public interface IProjectAccessAccessor
    {
        /// <summary>Returns null unless the user, project, assignment, and role are all active.</summary>
        Task<ProjectAccessRecord?> GetProjectAccessAsync(int userId, int projectId, CancellationToken cancellationToken);
    }
}
