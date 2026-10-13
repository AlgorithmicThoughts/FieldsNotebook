namespace Common.Errors;

public static class UserErrors
{
    /// <summary>The user does not exist, or is no longer active.</summary>
    public static RequestError NotFound(int userId) =>
        RequestError.NotFound($"User {userId} is not available.");
}
