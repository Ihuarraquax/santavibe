using MediatR;
using Microsoft.AspNetCore.Mvc;
using SantaVibe.Api.Common;

namespace SantaVibe.Api.Features.Groups.UpdateBudgetSuggestion;

public static class UpdateBudgetSuggestionEndpoint
{
    public static void MapUpdateBudgetSuggestionEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPut(
            "/api/groups/{groupId}/participants/me/budget-suggestion",
            async (
                Guid groupId,
                UpdateBudgetSuggestionRequest request,
                ISender sender) =>
            {
                var command = new UpdateBudgetSuggestionCommand(
                    groupId,
                    request.BudgetSuggestion);

                var result = await sender.Send(command);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.ToProblem();
            })
            .RequireAuthorization()
            .WithTags("Groups", "Budget")
            .WithName("UpdateBudgetSuggestion")
            .WithDescription("Update or set the authenticated user's budget suggestion for a group")
            .Produces<UpdateBudgetSuggestionResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}
