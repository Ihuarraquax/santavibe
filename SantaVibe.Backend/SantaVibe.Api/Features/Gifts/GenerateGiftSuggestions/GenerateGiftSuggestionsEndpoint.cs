using MediatR;
using Microsoft.AspNetCore.Mvc;
using SantaVibe.Api.Common;

namespace SantaVibe.Api.Features.Gifts.GenerateGiftSuggestions;

public static class GenerateGiftSuggestionsEndpoint
{
    public static void MapGenerateGiftSuggestionsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/groups/{groupId}/my-assignment/gift-suggestions",
            async (Guid groupId, ISender sender) =>
            {
                var command = new GenerateGiftSuggestionsCommand(groupId);
                var result = await sender.Send(command);

                return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
            })
            .RequireAuthorization()
            .RequireRateLimiting("gift-suggestions")
            .WithTags("Gifts")
            .WithName("GenerateGiftSuggestions")
            .WithDescription("Generate AI-powered gift suggestions for your assigned Secret Santa recipient")
            .Produces<GiftSuggestionsResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status429TooManyRequests)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}
