using MediatR;
using SantaVibe.Api.Common.DomainEvents;
using SantaVibe.Api.Data;

namespace SantaVibe.Api.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that dispatches domain events after command execution
/// Domain events are collected from entities and published through MediatR
/// </summary>
public class DomainEventDispatcherBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ApplicationDbContext _context;
    private readonly IPublisher _publisher;
    private readonly ILogger<DomainEventDispatcherBehavior<TRequest, TResponse>> _logger;

    public DomainEventDispatcherBehavior(
        ApplicationDbContext context,
        IPublisher publisher,
        ILogger<DomainEventDispatcherBehavior<TRequest, TResponse>> logger)
    {
        _context = context;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Execute the command handler
        var response = await next();

        // Collect domain events from tracked entities
        var entitiesWithEvents = _context.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        if (!entitiesWithEvents.Any())
        {
            return response;
        }

        // Get all domain events
        var domainEvents = entitiesWithEvents
            .SelectMany(e => e.DomainEvents)
            .ToList();

        // Clear events from entities
        foreach (var entity in entitiesWithEvents)
        {
            entity.ClearDomainEvents();
        }

        // Publish events
        foreach (var domainEvent in domainEvents)
        {
            _logger.LogDebug(
                "Publishing domain event {EventType} for command {CommandType}",
                domainEvent.GetType().Name,
                typeof(TRequest).Name);

            await _publisher.Publish(domainEvent, cancellationToken);
        }

        _logger.LogInformation(
            "Published {EventCount} domain events for command {CommandType}",
            domainEvents.Count,
            typeof(TRequest).Name);

        return response;
    }
}
