using Microsoft.AspNetCore.Identity;

namespace SantaVibe.Api.Data.Entities;

/// <summary>
/// Extended user entity for ASP.NET Core Identity
/// Includes application-specific fields beyond the standard IdentityUser
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// User's first name (FR-003)
    /// </summary>
    public required string FirstName { get; set; }

    /// <summary>
    /// User's last name (FR-003)
    /// </summary>
    public required string LastName { get; set; }

    /// <summary>
    /// Account creation timestamp
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Last successful login timestamp
    /// </summary>
    public DateTimeOffset? LastLoginAt { get; set; }

    /// <summary>
    /// Soft delete flag - preserves foreign key integrity
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Soft delete timestamp
    /// </summary>
    public DateTimeOffset? DeletedAt { get; set; }

    // Navigation properties
    public ICollection<Group> OrganizedGroups { get; set; } = new List<Group>();
    public ICollection<GroupParticipant> GroupParticipants { get; set; } = new List<GroupParticipant>();
    public ICollection<Assignment> AssignmentsAsSanta { get; set; } = new List<Assignment>();
    public ICollection<Assignment> AssignmentsAsRecipient { get; set; } = new List<Assignment>();
    public ICollection<EmailNotification> EmailNotifications { get; set; } = new List<EmailNotification>();
    public ICollection<ExclusionRule> ExclusionRulesAsUser1 { get; set; } = new List<ExclusionRule>();
    public ICollection<ExclusionRule> ExclusionRulesAsUser2 { get; set; } = new List<ExclusionRule>();
    public ICollection<ExclusionRule> ExclusionRulesCreated { get; set; } = new List<ExclusionRule>();
}
