using System;
using System.Collections.Generic;

namespace SantaVibe.Api.Features.Groups.GetUserGroups;

// The main response object
public class GetUserGroupsResponse
{
    public List<GroupDto> Groups { get; set; } = new();
    public int TotalCount { get; set; }
}

// Represents a single group in the list
public class GroupDto
{
    public Guid GroupId { get; set; }
    public required string Name { get; set; }
    public required string OrganizerId { get; set; }
    public required string OrganizerName { get; set; }
    public bool IsOrganizer { get; set; }
    public int ParticipantCount { get; set; }
    public decimal? Budget { get; set; }
    public bool DrawCompleted { get; set; }
    public DateTimeOffset JoinedAt { get; set; }
    public DateTimeOffset? DrawCompletedAt { get; set; }
}