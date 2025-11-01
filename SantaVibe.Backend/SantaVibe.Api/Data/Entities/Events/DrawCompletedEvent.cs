using SantaVibe.Api.Common.DomainEvents;

namespace SantaVibe.Api.Data.Entities.Events;

/// <summary>
/// Domain event raised when a Secret Santa draw is completed
/// </summary>
public sealed record DrawCompletedEvent(
    Guid GroupId,
    List<string> ParticipantIds,
    Dictionary<string, string> Assignments,
    DateTimeOffset OccurredAt) : IDomainEvent;
