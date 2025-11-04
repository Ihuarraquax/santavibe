using MediatR;
using Microsoft.AspNetCore.Mvc;
using SantaVibe.Api.Common;

namespace SantaVibe.Api.Features.Groups.GetMyBudgetSuggestion;

public static class GetMyBudgetSuggestionEndpoint
{
    public static void MapGetMyBudgetSuggestionEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/api/groups/{groupId}/participants/me/budget-suggestion",
            async (
                Guid groupId,
                ISender sender) =>
            {
                var query = new GetMyBudgetSuggestionQuery(groupId);

                var result = await sender.Send(query);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.ToProblem();
            })
            .RequireAuthorization()
            .WithTags("Budget")
            .WithName("GetMyBudgetSuggestion")
            .WithDescription("Retrieve the authenticated user's budget suggestion for a group")
            .Produces<GetMyBudgetSuggestionResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}
