using MediatR;
using SantaVibe.Api.Common;

namespace SantaVibe.Api.Features.Wishlists.UpdateWishlist;

/// <summary>
/// Command to update authenticated user's wishlist for a group
/// </summary>
/// <param name="GroupId">The group identifier</param>
/// <param name="WishlistContent">The new wishlist content (nullable)</param>
public record UpdateWishlistCommand(
    Guid GroupId,
    string? WishlistContent) : IRequest<Result<UpdateWishlistResponse>>, ITransactionalCommand;
