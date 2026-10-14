using DataContract.Domain.Projects;

namespace Engines.Authorization;

public interface IAuthorizationEngine : IEngine
{
    /// <summary>
    /// The user's access to the project. Logs and throws <see cref="Common.Errors.AuthorizationException"/> with 403
    /// when the user or project is inactive or does not exist, or the user has no active role on the project.
    /// Callers pass identifiers already validated as positive.
    /// </summary>
    Task<ProjectAccessRecord> AuthorizeProjectAsync(int userId, int projectId, CancellationToken cancellationToken);
}
