namespace SantaVibe.Api.Features.Invitations.AcceptInvitation;

/// <summary>
/// Response DTO for successful invitation acceptance
/// </summary>
/// <param name="GroupId">Group unique identifier</param>
/// <param name="GroupName">Name of the joined group</param>
/// <param name="OrganizerName">Full name of group organizer</param>
/// <param name="ParticipantCount">Updated participant count after join</param>
/// <param name="Budget">Final budget in PLN (null if not set by organizer)</param>
/// <param name="DrawCompleted">Indicates if draw has been completed</param>
/// <param name="JoinedAt">Timestamp when user joined the group</param>
public record AcceptInvitationResponse(
    Guid GroupId,
    string GroupName,
    string OrganizerName,
    int ParticipantCount,
    decimal? Budget,
    bool DrawCompleted,
    DateTimeOffset JoinedAt
);
