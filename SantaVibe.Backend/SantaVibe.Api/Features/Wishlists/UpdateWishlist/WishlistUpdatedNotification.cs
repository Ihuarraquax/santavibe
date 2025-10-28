using MediatR;

namespace SantaVibe.Api.Features.Wishlists.UpdateWishlist;

/// <summary>
/// Domain event published when a participant updates their wishlist after draw completion
/// </summary>
/// <param name="GroupId">The group identifier</param>
/// <param name="RecipientUserId">The user who updated their wishlist (the gift recipient)</param>
public record WishlistUpdatedNotification(
    Guid GroupId,
    string RecipientUserId) : INotification;
