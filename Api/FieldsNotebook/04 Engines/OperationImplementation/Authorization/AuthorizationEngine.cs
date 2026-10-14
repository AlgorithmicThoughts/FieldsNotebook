using Accessors;
using Accessors.Projects;
using Common.Errors;
using DataContract.Domain.Projects;
using Microsoft.Extensions.Logging;

namespace Engines.Authorization;

public sealed class AuthorizationEngine(AccessorFactory accessorFactory, ILogger<AuthorizationEngine> logger) : IAuthorizationEngine
{
    private readonly IProjectAccessor _projectAccessor = accessorFactory.CreateAccessor<IProjectAccessor>();

    public async Task<ProjectAccessRecord> AuthorizeProjectAsync(int userId, int projectId, CancellationToken cancellationToken)
    {
        var access = await _projectAccessor.GetProjectAccessAsync(userId, projectId, cancellationToken);
        if (access is null)
        {
            // The accessor joins user, project, assignment, and role, so null cannot say which one failed.
            logger.LogError(
                "Project access denied for user {UserId} on project {ProjectId}: the user or project is inactive or does not exist, or the user has no active assignment or role on the project.",
                userId, projectId);
            throw new AuthorizationException(AuthorizationErrors.ProjectAccessDenied(projectId));
        }

        return access;
    }
}
