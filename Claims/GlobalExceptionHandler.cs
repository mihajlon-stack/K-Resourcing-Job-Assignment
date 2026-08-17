using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Claims;

/// <summary>
/// Central translation point for exceptions escaping controllers. FluentValidation's
/// <see cref="ValidationException"/> is matched explicitly and projected to a 400
/// <see cref="ValidationProblemDetails"/> — because validation runs in the service layer
/// rather than through MVC's automatic model-validation pipeline, nothing wires this
/// translation for free. Anything unmatched falls through to a generic 500.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not ValidationException validationException)
        {
            return false;
        }

        var modelState = new ModelStateDictionary();
        foreach (var error in validationException.Errors)
        {
            modelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }

        var problemDetails = new ValidationProblemDetails(modelState)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred."
        };

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
