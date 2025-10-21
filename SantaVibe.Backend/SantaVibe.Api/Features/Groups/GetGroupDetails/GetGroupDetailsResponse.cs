namespace SantaVibe.Api.Features.Groups.GetGroupDetails;

/// <summary>
/// Response for GET /api/groups/{groupId} endpoint
/// Structure varies based on whether the draw has been completed
/// </summary>
public record GetGroupDetailsResponse
{
    // Common fields (always present)

    /// <summary>
    /// Unique group identifier
    /// </summary>
    public required Guid GroupId { get; init; }

    /// <summary>
    /// Group name
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Organizer's user ID
    /// </summary>
    public required string OrganizerId { get; init; }

    /// <summary>
    /// Organizer's full name
    /// </summary>
    public required string OrganizerName { get; init; }

    /// <summary>
    /// Whether the current user is the organizer
    /// </summary>
    public required bool IsOrganizer { get; init; }

    /// <summary>
    /// Final budget in PLN (null if not set)
    /// </summary>
    public decimal? Budget { get; init; }

    /// <summary>
    /// Whether the draw has been completed
    /// </summary>
    public required bool DrawCompleted { get; init; }

    /// <summary>
    /// Timestamp when the draw was completed (null if not completed)
    /// </summary>
    public DateTimeOffset? DrawCompletedAt { get; init; }

    /// <summary>
    /// Group creation timestamp
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Total number of participants in the group
    /// </summary>
    public required int ParticipantCount { get; init; }

    // Before draw only (null if draw completed)

    /// <summary>
    /// Full participant list (only present before draw)
    /// </summary>
    public List<ParticipantDto>? Participants { get; init; }

    /// <summary>
    /// Number of exclusion rules defined (only present before draw)
    /// </summary>
    public int? ExclusionRuleCount { get; init; }

    /// <summary>
    /// Whether the draw can be performed (only present before draw)
    /// </summary>
    public bool? CanDraw { get; init; }

    /// <summary>
    /// Draw validation result (only present before draw)
    /// </summary>
    public DrawValidationDto? DrawValidation { get; init; }

    // After draw only (null if draw not completed)

    /// <summary>
    /// Current user's assignment - who they are buying a gift for (only present after draw)
    /// </summary>
    public AssignmentDto? MyAssignment { get; init; }
}

/// <summary>
/// Participant information DTO
/// </summary>
public record ParticipantDto
{
    /// <summary>
    /// User ID
    /// </summary>
    public required string UserId { get; init; }

    /// <summary>
    /// User's first name
    /// </summary>
    public required string FirstName { get; init; }

    /// <summary>
    /// User's last name
    /// </summary>
    public required string LastName { get; init; }

    /// <summary>
    /// Timestamp when the user joined the group
    /// </summary>
    public required DateTimeOffset JoinedAt { get; init; }

    /// <summary>
    /// Whether the user has provided a budget suggestion
    /// </summary>
    public required bool HasBudgetSuggestion { get; init; }

    /// <summary>
    /// Whether the user has created a wishlist
    /// </summary>
    public required bool HasWishlist { get; init; }
}

/// <summary>
/// Assignment information DTO (user's Secret Santa assignment)
/// </summary>
public record AssignmentDto
{
    /// <summary>
    /// Recipient's user ID (who the current user is buying a gift for)
    /// </summary>
    public required string RecipientId { get; init; }

    /// <summary>
    /// Recipient's first name
    /// </summary>
    public required string RecipientFirstName { get; init; }

    /// <summary>
    /// Recipient's last name
    /// </summary>
    public required string RecipientLastName { get; init; }

    /// <summary>
    /// Whether the recipient has created a wishlist
    /// </summary>
    public required bool HasWishlist { get; init; }
}

/// <summary>
/// Draw validation result DTO
/// </summary>
public record DrawValidationDto
{
    /// <summary>
    /// Whether the draw can be performed (all validation rules passed)
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// List of validation errors (empty if IsValid is true)
    /// </summary>
    public required List<string> Errors { get; init; }
}
