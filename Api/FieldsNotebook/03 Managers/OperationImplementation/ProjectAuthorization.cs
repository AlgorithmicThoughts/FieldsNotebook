using Accessors.Projects;
using Common.Errors;
using Common.Results;
using DataContract.Domain.Projects;
using Microsoft.Extensions.Logging;

namespace Managers;

/// <summary>
/// The project membership check every Manager runs first. Membership is a Resource read, so each Manager
/// asks the project Accessor and returns the refusal in its result like any other expected failure.
/// </summary>
internal static class ProjectAuthorization
{
    /// <summary>The user's access to the project, or a 403 when the user or project is inactive or missing, or the user holds no active role there.</summary>
    public static async Task<Result<ProjectAccessRecord>> AuthorizeProjectAsync(this IProjectAccessor projectAccessor, int userId, int projectId,
        ILogger logger, CancellationToken cancellationToken)
    {
        var access = await projectAccessor.GetProjectAccessAsync(userId, projectId, cancellationToken);
        if (access is null)
        {
            // The accessor joins user, project, assignment, and role, so null cannot say which one failed.
            logger.LogError(
                "Project access denied for user {UserId} on project {ProjectId}: the user or project is inactive or does not exist, or the user has no active assignment or role on the project.",
                userId, projectId);
            return AuthorizationErrors.ProjectAccessDenied(projectId);
        }

        return access;
    }
}
