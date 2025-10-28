using MediatR;
using Microsoft.AspNetCore.Mvc;
using SantaVibe.Api.Common;

namespace SantaVibe.Api.Features.Wishlists.GetMyWishlist;

/// <summary>
/// Minimal API endpoint for retrieving user's wishlist in a group
/// </summary>
public static class GetMyWishlistEndpoint
{
    public static void MapGetMyWishlistEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/groups/{groupId}/participants/me/wishlist", async (
                [FromRoute] Guid groupId,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var query = new GetMyWishlistQuery(groupId);
                var result = await sender.Send(query, cancellationToken);

                if (result.IsSuccess)
                {
                    return Results.Ok(result.Value);
                }

                return result.ToProblem();
            })
            .RequireAuthorization()
            .WithName("GetMyWishlist")
            .WithTags("Wishlists")
            .Produces<GetMyWishlistResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}
