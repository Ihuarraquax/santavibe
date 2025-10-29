
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

        // Map error codes to appropriate HTTP status codes
        var statusCode = result.Error switch
        {
            "GroupNotFound" => StatusCodes.Status404NotFound,
            "Forbidden" => StatusCodes.Status403Forbidden,
            "Unauthorized" => StatusCodes.Status401Unauthorized,
            "ValidationError" => StatusCodes.Status400BadRequest,
            "InvalidInvitation" => StatusCodes.Status404NotFound,
            "AlreadyParticipant" => StatusCodes.Status409Conflict,
            "InvitationExpired" => StatusCodes.Status410Gone,
            "NotParticipant" => StatusCodes.Status403Forbidden,
            "DrawNotCompleted" => StatusCodes.Status403Forbidden,
            "DrawAlreadyCompleted" => StatusCodes.Status400BadRequest,
            "SameUser" => StatusCodes.Status400BadRequest,
            "DuplicateExclusionRule" => StatusCodes.Status409Conflict,
            "InvalidExclusionRule" => StatusCodes.Status400BadRequest,
            "InternalServerError" => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status400BadRequest // Default for unknown errors
        };

        var problemDetails = new ProblemDetails
        {
            Title = result.Error,
            Detail = result.Message,
            Status = statusCode
        };

        if (result.ValidationErrors != null)
        {
            problemDetails.Extensions["errors"] = result.ValidationErrors;
            problemDetails.Title = "Validation Error";
        }

        return Results.Problem(problemDetails);
    }
}
