using Common.Errors;
using Common.Results;
using FieldsNotebook.ErrorHandling;
using Microsoft.AspNetCore.Mvc;

namespace FieldsNotebook.Controllers;

/// <summary>The one place a Manager's result becomes an HTTP response.</summary>
public static class ControllerResultExtensions
{
    /// <summary>200 with the data, or the shared error shape with the status the error's category maps to.</summary>
    public static ActionResult<T> ToActionResult<T>(this ControllerBase controller, Result<T> result)
    {
        if (result.IsFailure)
        {
            return controller.ToProblem(result.Error);
        }

        return controller.Ok(result.Data);
    }

    public static ObjectResult ToProblem(this ControllerBase controller, RequestError error)
    {
        var statusCode = ApiErrorResponse.GetStatusCode(error);
        var problem = ApiErrorResponse.CreateProblem(statusCode, error, controller.HttpContext.TraceIdentifier);

        return new ObjectResult(problem)
        {
            StatusCode = statusCode,
            ContentTypes = { "application/json" },
        };
    }
}
