using DataContract.Domain.Projects;

namespace Engines.Authorization;

public interface IAuthorizationEngine
{
    /// <summary>
    /// The user's access to the project. Throws <see cref="Common.Errors.AuthorizationException"/> with 401 when
    /// the request identifies no user, and with 403 when the user has no active access to the project.
    /// </summary>
    Task<ProjectAccessRecord> AuthorizeProjectAsync(int userId, int projectId, CancellationToken cancellationToken);
}
