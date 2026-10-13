namespace Common.Errors;

/// <summary>Application failure categories translated to HTTP only by the API.</summary>
public static class ErrorCodes
{
    public const string InvalidRequest = "invalid_request";
    public const string Unauthorized = "unauthorized";
    public const string Forbidden = "forbidden";
    public const string NotFound = "not_found";
    public const string Conflict = "conflict";
    public const string InternalError = "internal_error";
    public const string RequestRejected = "request_rejected";
}
