using MediatR;
using SantaVibe.Api.Common;

namespace SantaVibe.Api.Features.Wishlists.GetMyWishlist;

/// <summary>
/// Query to retrieve the authenticated user's wishlist for a specific Secret Santa group
/// </summary>
public record GetMyWishlistQuery(
    Guid GroupId
) : IRequest<Result<GetMyWishlistResponse>>;
