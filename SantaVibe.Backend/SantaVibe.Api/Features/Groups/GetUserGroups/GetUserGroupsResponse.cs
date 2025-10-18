namespace SantaVibe.Api.Features.Groups.GetUserGroups;

/// <summary>
/// Response containing a list of groups the user is a participant in
/// </summary>
public record GetUserGroupsResponse
{
    public required List<GroupDto> Groups { get; init; }
    public required int TotalCount { get; init; }
}

/// <summary>
/// Represents a single group in the user's group list
/// </summary>
public record GroupDto
{
    public required Guid GroupId { get; init; }
    public required string Name { get; init; }
    public required string OrganizerId { get; init; }
    public required string OrganizerName { get; init; }
    public required bool IsOrganizer { get; init; }
    public required int ParticipantCount { get; init; }
    public decimal? Budget { get; init; }
    public required bool DrawCompleted { get; init; }
    public required DateTimeOffset JoinedAt { get; init; }
    public DateTimeOffset? DrawCompletedAt { get; init; }
}