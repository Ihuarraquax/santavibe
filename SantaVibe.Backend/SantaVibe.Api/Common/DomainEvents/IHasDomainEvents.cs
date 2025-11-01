namespace SantaVibe.Api.Common.DomainEvents;

/// <summary>
/// Interface for entities that can raise domain events
/// </summary>
public interface IHasDomainEvents
{
    /// <summary>
    /// Collection of domain events raised by this entity
    /// </summary>
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    /// <summary>
    /// Adds a domain event to the entity
    /// </summary>
    void AddDomainEvent(IDomainEvent domainEvent);

    /// <summary>
    /// Clears all domain events from the entity
    /// </summary>
    void ClearDomainEvents();
}
