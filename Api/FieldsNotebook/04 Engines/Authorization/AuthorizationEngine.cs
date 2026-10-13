using Accessors;
using Accessors.Projects;
using Common.Errors;
using DataContract.Domain.Projects;

namespace Engines.Authorization;

public sealed class AuthorizationEngine(AccessorFactory accessorFactory) : IAuthorizationEngine
{
    private readonly IProjectAccessAccessor _projectAccessAccessor = accessorFactory.CreateAccessor<IProjectAccessAccessor>();

    public async Task<ProjectAccessRecord> AuthorizeProjectAsync(int userId, int projectId, CancellationToken cancellationToken)
    {
        if (userId < 1)
        {
            throw new AuthorizationException(AuthorizationErrors.Unauthenticated());
        }

        var access = await _projectAccessAccessor.GetProjectAccessAsync(userId, projectId, cancellationToken);
        if (access is null)
        {
            throw new AuthorizationException(AuthorizationErrors.ProjectAccessDenied(projectId));
        }

        return access;
    }
}
