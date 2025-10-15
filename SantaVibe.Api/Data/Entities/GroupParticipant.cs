namespace SantaVibe.Api.Data.Entities;

/// <summary>
/// Junction table for many-to-many relationship between Users and Groups
/// Represents a participant's membership in a group
/// </summary>
public class GroupParticipant
{
    /// <summary>
    /// Group reference (part of composite PK)
    /// </summary>
    public Guid GroupId { get; set; }

    /// <summary>
    /// User reference (part of composite PK)
    /// </summary>
    public required string UserId { get; set; }

    /// <summary>
    /// Anonymous budget suggestion in PLN (FR-009)
    /// </summary>
    public decimal? BudgetSuggestion { get; set; }

    /// <summary>
    /// Join timestamp
    /// </summary>
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// User's wishlist content (FR-004)
    /// </summary>
    public string? WishlistContent { get; set; }

    /// <summary>
    /// Last wishlist update timestamp
    /// </summary>
    public DateTimeOffset? WishlistLastModified { get; set; }

    // Navigation properties
    public Group Group { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}
