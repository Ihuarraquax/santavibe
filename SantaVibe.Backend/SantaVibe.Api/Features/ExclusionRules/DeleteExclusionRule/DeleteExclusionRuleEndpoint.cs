using MediatR;
using Microsoft.AspNetCore.Mvc;
using SantaVibe.Api.Common;

namespace SantaVibe.Api.Features.ExclusionRules.DeleteExclusionRule;

/// <summary>
/// Endpoint for deleting an exclusion rule
/// </summary>
public static class DeleteExclusionRuleEndpoint
{
    public static void MapDeleteExclusionRuleEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/groups/{groupId:guid}/exclusion-rules/{ruleId:guid}",
            async (
                Guid groupId,
                Guid ruleId,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new DeleteExclusionRuleCommand(groupId, ruleId);
                var result = await sender.Send(command, cancellationToken);

                return result.IsSuccess
                    ? Results.NoContent()
                    : result.ToProblem();
            })
            .RequireAuthorization()
            .WithName("DeleteExclusionRule")
            .WithTags("ExclusionRules")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }
}
