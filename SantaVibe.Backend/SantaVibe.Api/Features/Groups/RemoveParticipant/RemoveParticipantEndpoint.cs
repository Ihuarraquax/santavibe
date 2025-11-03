using MediatR;
using Microsoft.AspNetCore.Mvc;
using SantaVibe.Api.Common;
using SantaVibe.Api.Services;

namespace SantaVibe.Api.Features.Groups.RemoveParticipant;

/// <summary>
/// Endpoint registration for DELETE /api/groups/{groupId}/participants/{userId}
/// Removes a participant from a Secret Santa group
/// </summary>
public static class RemoveParticipantEndpoint
{
    public static void MapRemoveParticipantEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/groups/{groupId:guid}/participants/{userId}", async (
                Guid groupId,
                string userId,
                ISender sender,
                IUserAccessor userAccessor,
                CancellationToken cancellationToken) =>
            {
                var requestingUserId = userAccessor.GetCurrentUserId().ToString();
                var command = new RemoveParticipantCommand(groupId, userId, requestingUserId);
                var result = await sender.Send(command, cancellationToken);

                return result.IsSuccess ? Results.NoContent() : result.ToProblem();
            })
            .RequireAuthorization()
            .WithTags("Groups")
            .WithName("RemoveParticipant")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Remove participant from group";
                operation.Description = "Removes a participant from a Secret Santa group. " +
                    "Only the group organizer can remove participants. " +
                    "Participants cannot be removed after the draw has been completed. " +
                    "The organizer cannot remove themselves. " +
                    "Related exclusion rules are automatically cleaned up.";
                return operation;
            })
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}
