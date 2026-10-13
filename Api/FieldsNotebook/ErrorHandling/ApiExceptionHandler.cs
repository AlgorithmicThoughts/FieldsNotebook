using Common.Errors;
using Microsoft.AspNetCore.Diagnostics;

namespace FieldsNotebook.ErrorHandling;

/// <summary>Writes the shared error shape for rejected requests and failures no Manager expected; expected ones, refused access included, arrive as results.</summary>
public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, error) = Classify(exception);

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            // .NET 10 suppresses middleware diagnostics for exceptions handled here.
            logger.LogError(exception, "API request failed. TraceId: {TraceId}", httpContext.TraceIdentifier);
        }

        var problem = ApiErrorResponse.CreateProblem(statusCode, error, httpContext.TraceIdentifier);
        httpContext.Response.StatusCode = statusCode;

        // APIs always emit JSON errors, even if the client's Accept header excludes JSON.
        // Do not let a rejected ProblemDetails writer turn a known 404 into an unhandled exception.
        await httpContext.Response.WriteAsJsonAsync(
            problem,
            options: null,
            contentType: "application/json",
            cancellationToken: cancellationToken);

        return true;
    }

    private static (int StatusCode, RequestError Error) Classify(Exception exception)
    {
        return exception switch
        {
            BadHttpRequestException rejected => (rejected.StatusCode,
                new RequestError(ErrorCodes.RequestRejected, "The HTTP request was rejected.")),
            _ => (StatusCodes.Status500InternalServerError,
                new RequestError(ErrorCodes.InternalError, "An unexpected error occurred.")),
        };
    }
}
