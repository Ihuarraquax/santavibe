namespace SantaVibe.Api.Models;

/// <summary>
/// Represents an exclusion rule preventing two participants from being assigned to each other
/// </summary>
public class ExclusionRule
{
    public Guid Id { get; set; }

    public Guid GroupId { get; set; }

    public required Group Group { get; set; }

    public Guid Participant1Id { get; set; }

    public required Participant Participant1 { get; set; }

    public Guid Participant2Id { get; set; }

    public required Participant Participant2 { get; set; }
}
