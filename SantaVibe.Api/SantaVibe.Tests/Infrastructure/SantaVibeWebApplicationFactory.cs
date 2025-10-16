using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SantaVibe.Api.Data;
using Testcontainers.PostgreSql;

namespace SantaVibe.Tests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory for integration testing with TestContainers
/// </summary>
public class SantaVibeWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("santavibe_test")
        .WithUsername("test_user")
        .WithPassword("test_password")
        .Build();

    /// <summary>
    /// PostgreSQL connection string for tests
    /// </summary>
    public string ConnectionString => _postgresContainer.GetConnectionString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set environment to Testing to allow conditional configuration
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Override configuration for testing
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = ConnectionString,
                ["Jwt:Secret"] = "test-secret-key-with-at-least-256-bits-length-for-jwt-token-signing-in-tests",
                ["Jwt:Issuer"] = "SantaVibe.Api.Tests",
                ["Jwt:Audience"] = "SantaVibe.Web.Tests",
                ["Jwt:ExpirationInDays"] = "7",
                ["DisableRateLimiting"] = "true" // Flag to disable rate limiting in tests
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext configuration
            services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));

            // Add DbContext with TestContainers PostgreSQL connection
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(ConnectionString);
            });

            // Build service provider and apply migrations
            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Apply migrations to ensure database schema is created
            dbContext.Database.Migrate();
        });
    }

    /// <summary>
    /// Start PostgreSQL container before tests
    /// </summary>
    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
    }

    /// <summary>
    /// Stop PostgreSQL container after tests
    /// </summary>
    public new async Task DisposeAsync()
    {
        await _postgresContainer.StopAsync();
        await _postgresContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}
