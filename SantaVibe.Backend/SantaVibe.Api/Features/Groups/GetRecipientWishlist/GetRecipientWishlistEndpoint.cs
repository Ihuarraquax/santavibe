using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SantaVibe.Api.Common;

namespace SantaVibe.Api.Features.Groups.GetRecipientWishlist;

/// <summary>
/// Endpoint registration for GET /api/groups/{groupId}/my-assignment/wishlist.
/// Retrieves the wishlist of the authenticated user's assigned gift recipient.
/// </summary>
public static class GetRecipientWishlistEndpoint
{
    public static void MapGetRecipientWishlistEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/groups/{groupId:guid}/my-assignment/wishlist", async (
                Guid groupId,
                ISender sender) =>
            {
                var query = new GetRecipientWishlistQuery(groupId);
                var result = await sender.Send(query);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
            })
            .RequireAuthorization()
            .WithTags("Groups")
            .WithName("GetRecipientWishlist")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Get recipient's wishlist";
                operation.Description =
                    "Retrieves the wishlist of the authenticated user's assigned gift recipient. " +
                    "Only available after draw completion. " +
                    "Returns recipient's name, wishlist content, and last modification date.";
                return operation;
            })
            .Produces<GetRecipientWishlistResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}
