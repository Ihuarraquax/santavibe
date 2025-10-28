namespace SantaVibe.Api.Features.Wishlists.GetMyWishlist;

/// <summary>
/// Response containing user's wishlist content for a Secret Santa group
/// </summary>
public class GetMyWishlistResponse
{
    public required Guid GroupId { get; init; }
    public string? WishlistContent { get; init; }
    public DateTimeOffset? LastModified { get; init; }
}
