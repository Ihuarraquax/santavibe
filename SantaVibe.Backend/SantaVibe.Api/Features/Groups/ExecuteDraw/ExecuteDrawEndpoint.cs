using MediatR;
using Microsoft.AspNetCore.Mvc;
using SantaVibe.Api.Common;
using SantaVibe.Api.Services;

namespace SantaVibe.Api.Features.Groups.ExecuteDraw;

/// <summary>
/// Endpoint registration for POST /api/groups/{groupId}/draw
/// Executes the Secret Santa draw algorithm and creates assignments
/// </summary>
public static class ExecuteDrawEndpoint
{
    public static void MapExecuteDrawEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/groups/{groupId:guid}/draw", async (
                Guid groupId,
                [FromBody] ExecuteDrawRequest request,
                ISender sender,
                IUserAccessor userAccessor,
                CancellationToken cancellationToken) =>
            {
                var userId = userAccessor.GetCurrentUserId().ToString();
                var command = new ExecuteDrawCommand(groupId, userId, request.Budget);
                var result = await sender.Send(command, cancellationToken);

                return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
            })
            .RequireAuthorization()
            .AddEndpointFilter<ValidationFilter<ExecuteDrawRequest>>()
            .WithTags("Draw")
            .WithName("ExecuteDraw")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Execute Secret Santa draw";
                operation.Description = "Executes the irreversible Secret Santa draw algorithm for the group. " +
                    "Creates assignments for all participants while respecting exclusion rules, " +
                    "sets the final budget, and schedules email notifications. " +
                    "Only the group organizer can execute the draw. " +
                    "This operation is transactional and cannot be undone.";
                return operation;
            })
            .Produces<ExecuteDrawResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}
