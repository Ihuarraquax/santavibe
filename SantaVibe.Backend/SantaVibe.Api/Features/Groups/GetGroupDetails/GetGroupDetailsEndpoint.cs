using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SantaVibe.Api.Common;

namespace SantaVibe.Api.Features.Groups.GetGroupDetails;

/// <summary>
/// Endpoint registration for GET /api/groups/{groupId}
/// Retrieves detailed information about a specific Secret Santa group
/// </summary>
public static class GetGroupDetailsEndpoint
{
    public static void MapGetGroupDetailsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/groups/{groupId:guid}", async (
                Guid groupId,
                ISender sender) =>
            {
                var query = new GetGroupDetailsQuery(groupId);
                var result = await sender.Send(query);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
            })
            .RequireAuthorization()
            .WithTags("Groups")
            .WithName("GetGroupDetails")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Get group details";
                operation.Description = "Retrieves detailed information about a Secret Santa group. " +
                    "Response structure varies based on draw status: " +
                    "before draw includes full participant list and validation, " +
                    "after draw includes only the current user's assignment.";
                return operation;
            })
            .Produces<GetGroupDetailsResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}
