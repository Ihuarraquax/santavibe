namespace SantaVibe.Api.Models;

/// <summary>
/// Represents a participant in a Secret Santa group
/// </summary>
public class Participant
{
    public Guid Id { get; set; }

    public Guid GroupId { get; set; }

    public required Group Group { get; set; }

    public Guid UserId { get; set; }

    public required User User { get; set; }

    public decimal? BudgetSuggestion { get; set; }

    public Guid? AssignedRecipientId { get; set; }

    public Participant? AssignedRecipient { get; set; }

    public DateTimeOffset JoinedAt { get; set; }
}
