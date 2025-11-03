using MediatR;
using SantaVibe.Api.Common;

namespace SantaVibe.Api.Features.Groups.GetRecipientWishlist;

/// <summary>
/// Query to retrieve the wishlist of the authenticated user's assigned gift recipient.
/// Only available after draw completion.
/// </summary>
public record GetRecipientWishlistQuery(Guid GroupId)
    : IRequest<Result<GetRecipientWishlistResponse>>;
