using MediatR;
using Microsoft.AspNetCore.Mvc;
using SantaVibe.Api.Common;

namespace SantaVibe.Api.Features.ExclusionRules.GetExclusionRules;

/// <summary>
/// Endpoint for retrieving all exclusion rules for a group
/// </summary>
public static class GetExclusionRulesEndpoint
{
    public static void MapGetExclusionRulesEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/groups/{groupId:guid}/exclusion-rules",
            async (
                Guid groupId,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var query = new GetExclusionRulesQuery(groupId);
                var result = await sender.Send(query, cancellationToken);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.ToProblem();
            })
            .RequireAuthorization()
            .WithName("GetExclusionRules")
            .WithTags("ExclusionRules")
            .Produces<GetExclusionRulesResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }
}
