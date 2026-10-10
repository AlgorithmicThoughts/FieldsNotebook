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

            if (exception is not ProjectAccessDeniedException)
            {
                return false;
            }

            httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
            return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "You do not have access to this project.",
                },
            });
        }
    }
}
