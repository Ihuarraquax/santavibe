namespace SantaVibe.Api.Data.Entities;

/// <summary>
/// Stores Secret Santa draw results
/// Each assignment represents one Santa → Recipient pairing (FR-016, FR-017)
/// </summary>
public class Assignment
{
    /// <summary>
    /// Unique assignment identifier
    /// </summary>
    public required Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Group reference
    /// </summary>
    public Guid GroupId { get; set; }

    /// <summary>
    /// User buying the gift (the "Santa")
    /// </summary>
    public required string SantaUserId { get; set; }

    /// <summary>
    /// User receiving the gift (the "Recipient")
    /// </summary>
    public required string RecipientUserId { get; set; }

    /// <summary>
    /// Assignment creation timestamp
    /// </summary>
    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public Group Group { get; set; } = null!;
    public ApplicationUser Santa { get; set; } = null!;
    public ApplicationUser Recipient { get; set; } = null!;
}
