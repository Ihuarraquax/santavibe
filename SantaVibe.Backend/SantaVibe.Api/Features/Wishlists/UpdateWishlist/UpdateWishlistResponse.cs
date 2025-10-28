namespace SantaVibe.Api.Features.Wishlists.UpdateWishlist;

/// <summary>
/// Response after updating wishlist
/// </summary>
public class UpdateWishlistResponse
{
    /// <summary>
    /// Group identifier
    /// </summary>
    public required Guid GroupId { get; init; }

    /// <summary>
    /// Updated wishlist content (null if cleared)
    /// </summary>
    public string? WishlistContent { get; init; }

    /// <summary>
    /// Timestamp of last modification
    /// </summary>
    public required DateTimeOffset LastModified { get; init; }
}
