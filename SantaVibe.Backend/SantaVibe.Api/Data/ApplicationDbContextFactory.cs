using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SantaVibe.Api.Data;

/// <summary>
/// Design-time factory for ApplicationDbContext
/// Used by EF Core tools to create DbContext instances for migrations
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        // Use a placeholder connection string for design-time operations
        // The actual connection string will be loaded from configuration at runtime
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=santavibe_dev;Username=postgres;Password=example",
            b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));

        return new ApplicationDbContext(optionsBuilder.Options, null);
    }
}
