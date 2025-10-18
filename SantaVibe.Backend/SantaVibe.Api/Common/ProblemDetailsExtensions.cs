
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SantaVibe.Api.Common;

public static class ProblemDetailsExtensions
{
    public static IResult ToProblem<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            throw new System.InvalidOperationException("Cannot convert a success result to a problem.");
        }

        var problemDetails = new ProblemDetails
        {
            Title = result.Error,
            Detail = result.Message,
            Status = StatusCodes.Status400BadRequest // Default status
        };

        if (result.ValidationErrors != null)
        {
            problemDetails.Extensions["errors"] = result.ValidationErrors;
            problemDetails.Title = "Validation Error";
        }

        return Results.Problem(problemDetails);
    }
}
