namespace Common.Errors;

/// <summary>A stable failure category from <see cref="ErrorCodes"/> and a message that is safe to show the user.</summary>
public sealed record RequestError(string Code, string Message)
{
    public static RequestError InvalidRequest(string message) => new(ErrorCodes.InvalidRequest, message);

    public static RequestError Forbidden(string message) => new(ErrorCodes.Forbidden, message);

    public static RequestError NotFound(string message) => new(ErrorCodes.NotFound, message);

    public static RequestError Conflict(string message) => new(ErrorCodes.Conflict, message);
}
