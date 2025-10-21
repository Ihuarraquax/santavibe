using MediatR;
using Microsoft.AspNetCore.Mvc;
using SantaVibe.Api.Common;
using SantaVibe.Api.Services;

namespace SantaVibe.Api.Features.Invitations.AcceptInvitation;

/// <summary>
/// Minimal API endpoint for accepting an invitation and joining a group
/// </summary>
public static class AcceptInvitationEndpoint
{
    public static void MapAcceptInvitationEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/invitations/{token:guid}/accept", async (
                [FromRoute] Guid token,
                [FromBody] AcceptInvitationRequest request,
                ISender sender,
                IUserAccessor userAccessor,
                CancellationToken cancellationToken) =>
            {
                var userId = userAccessor.GetCurrentUserId();
                var command = new AcceptInvitationCommand(
                    token,
                    userId.ToString(),
                    request.BudgetSuggestion
                );

                var result = await sender.Send(command, cancellationToken);

                if (result.IsSuccess)
                {
                    return Results.Created(
                        $"/api/groups/{result.Value!.GroupId}",
                        result.Value
                    );
                }

                return result.ToProblem();
            })
            .RequireAuthorization()
            .WithName("AcceptInvitation")
            .WithTags("Invitations")
            .Produces<AcceptInvitationResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status410Gone)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .AddEndpointFilter<ValidationFilter<AcceptInvitationRequest>>();
    }
}
