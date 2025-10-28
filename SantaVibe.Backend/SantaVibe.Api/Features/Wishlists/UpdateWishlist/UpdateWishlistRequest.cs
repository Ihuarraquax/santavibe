namespace SantaVibe.Api.Features.Wishlists.UpdateWishlist;

/// <summary>
/// Request to update user's wishlist for a group
/// </summary>
public class UpdateWishlistRequest
{
    /// <summary>
    /// Wishlist content (nullable to support clearing wishlist)
    /// </summary>
    public string? WishlistContent { get; init; }
}
