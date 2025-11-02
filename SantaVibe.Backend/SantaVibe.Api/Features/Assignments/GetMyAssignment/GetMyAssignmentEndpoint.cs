using MediatR;
using Microsoft.AspNetCore.Mvc;
using SantaVibe.Api.Common;
using SantaVibe.Api.Services;

namespace SantaVibe.Api.Features.Assignments.GetMyAssignment;

/// <summary>
/// Endpoint registration for GET /api/groups/{groupId}/my-assignment
/// Retrieves the authenticated user's Secret Santa assignment
/// </summary>
public static class GetMyAssignmentEndpoint
{
    public static void MapGetMyAssignmentEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/groups/{groupId:guid}/my-assignment", async (
                Guid groupId,
                ISender sender,
                IUserAccessor userAccessor,
                CancellationToken cancellationToken) =>
            {
                var userId = userAccessor.GetCurrentUserId().ToString();
                var query = new GetMyAssignmentQuery(groupId, userId);
                var result = await sender.Send(query, cancellationToken);

                return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
            })
            .RequireAuthorization()
            .WithTags("Assignments", "Groups")
            .WithName("GetMyAssignment")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Get my Secret Santa assignment";
                operation.Description = "Retrieves the authenticated user's Secret Santa assignment for a specific group. " +
                    "Shows who the user is buying a gift for (the recipient). " +
                    "This endpoint is only accessible after the draw has been completed. " +
                    "Users can only see their own assignment - privacy is strictly enforced.";
                return operation;
            })
            .Produces<GetMyAssignmentResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}
