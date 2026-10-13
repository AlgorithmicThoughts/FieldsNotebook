namespace Common.Errors;

public static class AuthorizationErrors
{
    /// <summary>The user has no active access to the project, or the project does not exist.</summary>
    public static RequestError ProjectAccessDenied(int projectId) =>
        RequestError.Forbidden($"The user has no active access to project {projectId}.");

    /// <summary>The request names a user who does not exist or is no longer active.</summary>
    public static RequestError UserNotActive() =>
        RequestError.Forbidden("The user is not active.");
}
