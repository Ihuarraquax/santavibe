namespace SantaVibe.Api.Features.Invitations.GetInvitationDetails;

/// <summary>
/// Response DTO for getting invitation details by token
/// </summary>
/// <param name="InvitationToken">The invitation token (UUID)</param>
/// <param name="GroupId">The group identifier</param>
/// <param name="GroupName">Name of the group</param>
/// <param name="OrganizerName">Full name of the group organizer</param>
/// <param name="ParticipantCount">Current number of participants in the group</param>
/// <param name="DrawCompleted">Indicates whether the Secret Santa draw has been completed</param>
/// <param name="IsValid">Indicates whether the invitation is valid and can be used</param>
public sealed record GetInvitationDetailsResponse(
    Guid InvitationToken,
    Guid GroupId,
    string GroupName,
    string OrganizerName,
    int ParticipantCount,
    bool DrawCompleted,
    bool IsValid
);
