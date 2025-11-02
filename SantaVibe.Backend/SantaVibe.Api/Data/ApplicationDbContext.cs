using MediatR;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SantaVibe.Api.Common.DomainEvents;
using SantaVibe.Api.Data.Entities;

namespace SantaVibe.Api.Data;

/// <summary>
/// Application database context for SantaVibe
/// Extends IdentityDbContext to include ASP.NET Core Identity tables
/// </summary>
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IMediator? mediator) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupParticipant> GroupParticipants => Set<GroupParticipant>();
    public DbSet<ExclusionRule> ExclusionRules => Set<ExclusionRule>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<EmailNotification> EmailNotifications => Set<EmailNotification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        ConfigurePostgreSqlConventions(modelBuilder);
    }

    private static void ConfigurePostgreSqlConventions(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTimeOffset) || property.ClrType == typeof(DateTimeOffset?))
                {
                    property.SetColumnType("timestamp with time zone");
                }
            }
        }
    }

    public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        // Dispatch Domain Events collection. 
        // Choices:
        // A) Right BEFORE committing data (EF SaveChanges) into the DB will make a single transaction including  
        // side effects from the domain event handlers which are using the same DbContext with "InstancePerLifetimeScope" or "scoped" lifetime
        // B) Right AFTER committing data (EF SaveChanges) into the DB will make multiple transactions. 
        // You will need to handle eventual consistency and compensatory actions in case of failures in any of the Handlers. 
        var aggregateRoots = ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(x => x.Entity.DomainEvents != null && x.Entity.DomainEvents.Any())
            .ToList();

        var domainEvents = aggregateRoots
            .SelectMany(x => x.Entity.DomainEvents!)
            .ToList();

        aggregateRoots
            .ForEach(entity => entity.Entity.ClearDomainEvents());

        foreach (var @event in domainEvents)
        {
            await mediator!.Publish((object)@event, cancellationToken);
        }

        // After executing this line all the changes (from the Command Handler and Domain CalendarEvent Handlers) 
        // performed throught the DbContext will be committed
        await SaveChangesAsync(cancellationToken);

        return true;
    }
}
