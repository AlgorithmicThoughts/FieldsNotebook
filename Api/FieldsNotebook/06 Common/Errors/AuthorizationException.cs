namespace Common.Errors;

/// <summary>
/// Thrown rather than returned, so a request without identity or access fails closed even where a
/// caller forgets to check a result. The API answers 401 or 403 from the error's code.
/// </summary>
public sealed class AuthorizationException(RequestError error) : Exception(error.Message)
{
    public RequestError Error { get; } = error;
}
