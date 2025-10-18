namespace SantaVibe.Api.Features.Groups.Create;

/// <summary>
/// Response returned after creating a new Secret Santa group
/// </summary>
public record CreateGroupResponse
{
    /// <summary>
    /// Unique identifier for the created group
    /// </summary>
    public required Guid GroupId { get; init; }

    /// <summary>
    /// Group name
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// User ID of the group organizer
    /// </summary>
    public required string OrganizerId { get; init; }

    /// <summary>
    /// Full name of the organizer
    /// </summary>
    public required string OrganizerName { get; init; }

    /// <summary>
    /// UUID that can be used to join the group
    /// </summary>
    public required Guid InvitationToken { get; init; }

    /// <summary>
    /// Complete shareable URL for invitation
    /// </summary>
    public required string InvitationLink { get; init; }

    /// <summary>
    /// Number of participants in the group (always 1 for newly created groups)
    /// </summary>
    public required int ParticipantCount { get; init; }

    /// <summary>
    /// Budget in PLN (always null for newly created groups)
    /// </summary>
    public required decimal? Budget { get; init; }

    /// <summary>
    /// Whether the Secret Santa draw has been completed (always false for new groups)
    /// </summary>
    public required bool DrawCompleted { get; init; }

    /// <summary>
    /// ISO 8601 timestamp when the group was created (UTC)
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }
}
