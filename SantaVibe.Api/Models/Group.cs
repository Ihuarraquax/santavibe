namespace SantaVibe.Api.Models;

/// <summary>
/// Represents a Secret Santa group
/// </summary>
public class Group
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public Guid OrganizerId { get; set; }

    public required User Organizer { get; set; }

    public string? InvitationCode { get; set; }

    public decimal? Budget { get; set; }

    public bool IsDrawPerformed { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? DrawPerformedAt { get; set; }

    public ICollection<Participant> Participants { get; set; } = new List<Participant>();

    public ICollection<ExclusionRule> ExclusionRules { get; set; } = new List<ExclusionRule>();
}
