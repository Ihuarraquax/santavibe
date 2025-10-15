namespace SantaVibe.Api.Data.Entities;

/// <summary>
/// Defines bidirectional exclusion pairs for the draw algorithm
/// Prevents specific participants from drawing each other (FR-010)
/// </summary>
public class ExclusionRule
{
    /// <summary>
    /// Unique rule identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Group reference
    /// </summary>
    public Guid GroupId { get; set; }

    /// <summary>
    /// First user in exclusion pair
    /// </summary>
    public required string UserId1 { get; set; }

    /// <summary>
    /// Second user in exclusion pair
    /// </summary>
    public required string UserId2 { get; set; }

    /// <summary>
    /// Organizer who created the rule
    /// </summary>
    public required string CreatedByUserId { get; set; }

    /// <summary>
    /// Rule creation timestamp
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public Group Group { get; set; } = null!;
    public ApplicationUser User1 { get; set; } = null!;
    public ApplicationUser User2 { get; set; } = null!;
    public ApplicationUser CreatedBy { get; set; } = null!;
}
