using Common.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace FieldsNotebook.ErrorHandling;

/// <summary>The one error shape every endpoint returns, whether a Manager or model binding refused the request.</summary>
internal static class ApiErrorResponse
{
    private static readonly IReadOnlyDictionary<string, int> StatusCodesByError = new Dictionary<string, int>
    {
        [ErrorCodes.InvalidRequest] = StatusCodes.Status400BadRequest,
        [ErrorCodes.Unauthorized] = StatusCodes.Status401Unauthorized,
        [ErrorCodes.Forbidden] = StatusCodes.Status403Forbidden,
        [ErrorCodes.NotFound] = StatusCodes.Status404NotFound,
        [ErrorCodes.Conflict] = StatusCodes.Status409Conflict,
        [ErrorCodes.InternalError] = StatusCodes.Status500InternalServerError,
    };

    public static int GetStatusCode(RequestError error)
    {
        return StatusCodesByError.TryGetValue(error.Code, out var statusCode)
            ? statusCode
            : StatusCodes.Status500InternalServerError;
    }

    public static ProblemDetails CreateProblem(int statusCode, RequestError error, string traceId)
    {
        return new ProblemDetails
        {
            Status = statusCode,
            Title = ReasonPhrases.GetReasonPhrase(statusCode),
            Detail = error.Message,
            Extensions =
            {
                ["error"] = error,
                ["traceId"] = traceId,
            },
        };
    }

    /// <summary>
    /// Replaces the default ValidationProblemDetails, so a request that fails its contract's attributes
    /// reads like one a Manager refused: the first message as the detail, every field's messages under "errors".
    /// </summary>
    public static IActionResult FromInvalidModelState(ActionContext context)
    {
        var errors = context.ModelState
            .Where(entry => entry.Value is { Errors.Count: > 0 })
            .ToDictionary(entry => entry.Key, entry => entry.Value!.Errors
                .Select(e => string.IsNullOrEmpty(e.ErrorMessage) ? "The value is not valid." : e.ErrorMessage)
                .ToArray());
        var message = errors.Values.SelectMany(messages => messages).FirstOrDefault() ?? "The request is not valid.";
        var problem = CreateProblem(StatusCodes.Status400BadRequest, new RequestError(ErrorCodes.InvalidRequest, message),
            context.HttpContext.TraceIdentifier);
        problem.Extensions["errors"] = errors;

        return new ObjectResult(problem)
        {
            StatusCode = StatusCodes.Status400BadRequest,
            ContentTypes = { "application/json" },
        };
    }
}
