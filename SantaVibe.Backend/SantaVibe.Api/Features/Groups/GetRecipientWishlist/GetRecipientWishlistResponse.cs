namespace SantaVibe.Api.Features.Groups.GetRecipientWishlist;

/// <summary>
/// Response containing the recipient's wishlist information.
/// </summary>
public class GetRecipientWishlistResponse
{
    /// <summary>
    /// Group identifier.
    /// </summary>
    public required Guid GroupId { get; init; }

    /// <summary>
    /// Recipient user ID.
    /// </summary>
    public required string RecipientId { get; init; }

    /// <summary>
    /// Recipient's first name.
    /// </summary>
    public required string RecipientFirstName { get; init; }

    /// <summary>
    /// Recipient's last name.
    /// </summary>
    public required string RecipientLastName { get; init; }

    /// <summary>
    /// Wishlist content (null if recipient hasn't created a wishlist).
    /// </summary>
    public string? WishlistContent { get; init; }

    /// <summary>
    /// Last modification timestamp (null if wishlist empty).
    /// </summary>
    public DateTimeOffset? LastModified { get; init; }
}
