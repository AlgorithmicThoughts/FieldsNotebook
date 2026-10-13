namespace Common.Errors;

public static class AuthorizationErrors
{
    /// <summary>The request carries no user identity.</summary>
    public static RequestError Unauthenticated() =>
        new(ErrorCodes.Unauthorized, "The request does not identify a user.");

    /// <summary>The user has no active access to the project, or the project does not exist.</summary>
    public static RequestError ProjectAccessDenied(int projectId) =>
        RequestError.Forbidden($"The user has no active access to project {projectId}.");
}
