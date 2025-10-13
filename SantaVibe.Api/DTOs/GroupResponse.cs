namespace SantaVibe.Api.DTOs;

/// <summary>
/// Response DTO for group information
/// </summary>
public class GroupResponse
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public Guid OrganizerId { get; set; }
    public required string OrganizerName { get; set; }
    public required string InvitationCode { get; set; }
    public decimal? Budget { get; set; }
    public bool IsDrawPerformed { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? DrawPerformedAt { get; set; }
    public int ParticipantCount { get; set; }
}
