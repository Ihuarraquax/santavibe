namespace SantaVibe.Api.Data.Entities;

/// <summary>
/// Represents a Secret Santa event/group
/// </summary>
public class Group
{
    /// <summary>
    /// Unique group identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Group name (FR-006)
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Organizer user reference (FK to AspNetUsers)
    /// </summary>
    public required string OrganizerUserId { get; set; }

    /// <summary>
    /// Final budget in PLN (FR-011)
    /// </summary>
    public decimal? Budget { get; set; }

    /// <summary>
    /// Shareable invitation token (FR-007)
    /// </summary>
    public Guid InvitationToken { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Draw completion timestamp (FR-011)
    /// NULL = draw not performed, NOT NULL = draw completed
    /// </summary>
    public DateTimeOffset? DrawCompletedAt { get; set; }

    /// <summary>
    /// Group creation timestamp
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Last modification timestamp
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    // Navigation properties
    public ApplicationUser Organizer { get; set; } = null!;
    public ICollection<GroupParticipant> GroupParticipants { get; set; } = new List<GroupParticipant>();
    public ICollection<ExclusionRule> ExclusionRules { get; set; } = new List<ExclusionRule>();
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
    public ICollection<EmailNotification> EmailNotifications { get; set; } = new List<EmailNotification>();
}
