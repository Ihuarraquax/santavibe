using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SantaVibe.Api.Common;

namespace SantaVibe.Api.Features.Groups.GetBudgetSuggestions;

/// <summary>
/// Endpoint registration for GET /api/groups/{groupId}/budget/suggestions
/// Retrieves anonymous budget suggestions from group participants
/// Only accessible by the group organizer
/// </summary>
public static class GetBudgetSuggestionsEndpoint
{
    public static void MapGetBudgetSuggestionsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/groups/{groupId:guid}/budget/suggestions", async (
                Guid groupId,
                ISender sender) =>
            {
                var query = new GetBudgetSuggestionsQuery(groupId);
                var result = await sender.Send(query);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
            })
            .RequireAuthorization()
            .WithTags("Groups")
            .WithName("GetBudgetSuggestions")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Get anonymous budget suggestions";
                operation.Description = "Retrieves an anonymous, sorted list of budget suggestions from all participants in a Secret Santa group. " +
                    "Only the group organizer can access this endpoint. " +
                    "The response maintains participant anonymity by only returning the suggestion amounts without any user identification. " +
                    "Suggestions are sorted in ascending order.";
                return operation;
            })
            .Produces<BudgetSuggestionsResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}
