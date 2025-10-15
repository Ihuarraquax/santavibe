namespace SantaVibe.Api.Data.Entities;

/// <summary>
/// Queue system for delayed email delivery
/// Supports retry logic and tracking (FR-018, FR-019)
/// </summary>
public class EmailNotification
{
    /// <summary>
    /// Unique notification identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Type of email notification
    /// </summary>
    public EmailNotificationType Type { get; set; }

    /// <summary>
    /// Email recipient reference
    /// </summary>
    public required string RecipientUserId { get; set; }

    /// <summary>
    /// Related group reference
    /// </summary>
    public Guid GroupId { get; set; }

    /// <summary>
    /// Scheduled send time (for delayed notifications)
    /// </summary>
    public DateTimeOffset ScheduledAt { get; set; }

    /// <summary>
    /// Actual send timestamp (NULL = not sent yet)
    /// </summary>
    public DateTimeOffset? SentAt { get; set; }

    /// <summary>
    /// First delivery attempt timestamp
    /// </summary>
    public DateTimeOffset? FirstAttemptAt { get; set; }

    /// <summary>
    /// Most recent attempt timestamp
    /// </summary>
    public DateTimeOffset? LastAttemptAt { get; set; }

    /// <summary>
    /// Number of delivery attempts
    /// </summary>
    public int AttemptCount { get; set; } = 0;

    /// <summary>
    /// Last error message if failed
    /// </summary>
    public string? LastError { get; set; }

    // Navigation properties
    public ApplicationUser Recipient { get; set; } = null!;
    public Group Group { get; set; } = null!;
}
