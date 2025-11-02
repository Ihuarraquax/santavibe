namespace SantaVibe.Api.Features.Assignments.GetMyAssignment;

/// <summary>
/// Response DTO for getting the authenticated user's Secret Santa assignment
/// </summary>
public sealed record GetMyAssignmentResponse(
    Guid GroupId,
    string GroupName,
    decimal Budget,
    DateTimeOffset DrawCompletedAt,
    RecipientDto Recipient);

/// <summary>
/// DTO representing the recipient (who the user is buying a gift for)
/// </summary>
public sealed record RecipientDto(
    string UserId,
    string FirstName,
    string LastName,
    bool HasWishlist,
    DateTimeOffset? WishlistLastModified);
