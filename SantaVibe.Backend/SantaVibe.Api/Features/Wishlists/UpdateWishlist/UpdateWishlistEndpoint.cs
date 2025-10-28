using MediatR;
using Microsoft.AspNetCore.Mvc;
using SantaVibe.Api.Common;

namespace SantaVibe.Api.Features.Wishlists.UpdateWishlist;

/// <summary>
/// Minimal API endpoint for updating user's wishlist in a group
/// </summary>
public static class UpdateWishlistEndpoint
{
    public static void MapUpdateWishlistEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPut("/api/groups/{groupId}/participants/me/wishlist", async (
                [FromRoute] Guid groupId,
                [FromBody] UpdateWishlistRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateWishlistCommand(
                    groupId,
                    request.WishlistContent);

                var result = await sender.Send(command, cancellationToken);

                if (result.IsSuccess)
                {
                    return Results.Ok(result.Value);
                }

                return result.ToProblem();
            })
            .RequireAuthorization()
            .WithName("UpdateWishlist")
            .WithTags("Wishlists")
            .Produces<UpdateWishlistResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}
