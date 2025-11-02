using MediatR;
using Microsoft.AspNetCore.Mvc;
using SantaVibe.Api.Common;
using SantaVibe.Api.Services;

namespace SantaVibe.Api.Features.Groups.ValidateDraw;

/// <summary>
/// Endpoint registration for GET /api/groups/{groupId}/draw/validate
/// Validates whether a Secret Santa draw can be executed for the group
/// </summary>
public static class ValidateDrawEndpoint
{
    public static void MapValidateDrawEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/groups/{groupId:guid}/draw/validate", async (
                Guid groupId,
                ISender sender,
                IUserAccessor userAccessor,
                CancellationToken cancellationToken) =>
            {
                var userId = userAccessor.GetCurrentUserId().ToString();
                var query = new ValidateDrawQuery(groupId, userId);
                var result = await sender.Send(query, cancellationToken);

                return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
            })
            .RequireAuthorization()
            .WithTags("Draw")
            .WithName("ValidateDraw")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Validate draw feasibility";
                operation.Description = "Validates whether the Secret Santa draw can be executed for the group. " +
                    "Checks participant count, exclusion rules, and draw completion status. " +
                    "Only the group organizer can validate the draw.";
                return operation;
            })
            .Produces<ValidateDrawResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}
