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

            var problem = ToProblem(exception);
            if (problem is null)
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
            ProjectAccessDeniedException => new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "You do not have access to this project.",
            },
            ArgumentOutOfRangeException => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Project and user IDs must be greater than zero.",
            },
            _ => null,
        };
    }
}
