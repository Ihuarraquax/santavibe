using MediatR;
using Microsoft.AspNetCore.Mvc;
using SantaVibe.Api.Common;

namespace SantaVibe.Api.Features.ExclusionRules.CreateExclusionRule;

/// <summary>
/// Endpoint for creating a new exclusion rule
/// </summary>
public static class CreateExclusionRuleEndpoint
{
    public static void MapCreateExclusionRuleEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/groups/{groupId:guid}/exclusion-rules",
            async (
                Guid groupId,
                CreateExclusionRuleRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateExclusionRuleCommand(
                    groupId,
                    request.UserId1,
                    request.UserId2);

                var result = await sender.Send(command, cancellationToken);

                return result.IsSuccess
                    ? Results.Created(
                        $"/api/groups/{groupId}/exclusion-rules/{result.Value!.RuleId}",
                        result.Value)
                    : result.ToProblem();
            })
            .RequireAuthorization()
            .WithName("CreateExclusionRule")
            .WithTags("ExclusionRules")
            .Produces<CreateExclusionRuleResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
    }
}
