using Common.Errors;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace FieldsNotebook.Controllers
{
    /// <summary>Maps known failures to ProblemDetails; anything else falls through to the default 500 handler.</summary>
    public sealed class ApiExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            // The client went away (a superseded browser request); there is no one to answer and nothing failed.
            if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
            {
                httpContext.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
                return true;
            }

            if (ToProblem(exception) is not { } problem)
            {
                return false;
            }

            httpContext.Response.StatusCode = problem.Status!.Value;
            return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = problem,
            });
        }

        private static ProblemDetails? ToProblem(Exception exception) => exception switch
        {
            DelayReportValidationException => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = exception.Message,
            },
            DelayReportForbiddenException => new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = exception.Message,
            },
            ProjectAccessDeniedException => new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "You do not have access to this project.",
            },
            NotificationNotFoundException => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "That notice is not available in Field Notebook.",
            },
            InvalidRequestException => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = exception.Message,
            },
            DelayNotFoundException => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "That delay is not available in Field Notebook.",
            },
            DelayActionNotPermittedException => new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Your role and crew do not carry authority for this delay action.",
            },
            // The delay moved on, or it never stood where this action requires; nothing was written.
            DelayStateConflictException conflict => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "This delay cannot take that action right now.",
                Detail = conflict.Reason,
            },
            // ArgumentOutOfRangeException derives from ArgumentException, so it must match first.
            ArgumentOutOfRangeException => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Request IDs must be greater than zero.",
            },
            ArgumentException => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "The request is missing a required value.",
            },
            _ => null,
        };
    }
}
