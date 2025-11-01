using MediatR;

namespace SantaVibe.Api.Common.DomainEvents;

/// <summary>
/// Marker interface for domain events
/// Domain events are published when important state changes occur in aggregates
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>
    /// Timestamp when the event occurred
    /// </summary>
    DateTimeOffset OccurredAt { get; }
}
