using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using SantaVibe.Api.BackgroundServices;
using SantaVibe.Api.Data;
using SantaVibe.Api.Data.Entities;
using SantaVibe.Api.Services.Email;
using SantaVibe.Tests.Infrastructure;
using System.Net;

namespace SantaVibe.Tests.BackgroundServices;

/// <summary>
/// Integration tests for EmailNotificationWorker background service
/// Tests the complete email notification processing pipeline using event-driven approach
/// </summary>
public class EmailNotificationWorkerIntegrationTests : IClassFixture<SantaVibeWebApplicationFactory>
{
    private readonly SantaVibeWebApplicationFactory _factory;

    public EmailNotificationWorkerIntegrationTests(SantaVibeWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProcessPendingNotifications_WithDrawCompletedNotification_SendsEmailSuccessfully()
    {
        // Arrange
        var emailService = Substitute.For<IEmailService>();
        emailService
            .SendDrawCompletedEmailAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(EmailResult.Success());

        var customFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["EmailNotificationWorker:Enabled"] = "false" // Disable background service
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IEmailService>();
                services.AddScoped<IEmailService>(_ => emailService);
            });
        });

        using var scope = customFactory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var processor = scope.ServiceProvider.GetRequiredService<IEmailNotificationProcessor>();

        // Create test data
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            UserName = "test@example.com"
        };

        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "Test Group",
            OrganizerUserId = user.Id,
            DrawCompletedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var notification = new EmailNotification
        {
            Id = Guid.NewGuid(),
            Type = EmailNotificationType.DrawCompleted,
            RecipientUserId = user.Id,
            GroupId = group.Id,
            ScheduledAt = DateTimeOffset.UtcNow.AddSeconds(-1),
            SentAt = null,
            FirstAttemptAt = null,
            LastAttemptAt = null,
            AttemptCount = 0,
            LastError = null
        };

        await dbContext.Users.AddAsync(user);
        await dbContext.Groups.AddAsync(group);
        await dbContext.EmailNotifications.AddAsync(notification);
        await dbContext.SaveChangesAsync();

        // Setup event handler
        var completionSource = new TaskCompletionSource<bool>();
        processor.OnIterationCompleted += (sender, args) => completionSource.TrySetResult(true);

        // Act
        await processor.ProcessPendingNotificationsAsync();
        await completionSource.Task;

        // Assert
        Assert.True(completionSource.Task.IsCompletedSuccessfully);

        var processedNotification = await dbContext.EmailNotifications
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == notification.Id);

        Assert.NotNull(processedNotification);
        Assert.NotNull(processedNotification.SentAt);
        Assert.NotNull(processedNotification.FirstAttemptAt);
        Assert.NotNull(processedNotification.LastAttemptAt);
        Assert.Equal(1, processedNotification.AttemptCount);
        Assert.Null(processedNotification.LastError);

        // Verify email service was called
        await emailService.Received(1).SendDrawCompletedEmailAsync(
            "test@example.com",
            "John Doe",
            "Test Group",
            group.Id,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessPendingNotifications_WithWishlistUpdatedNotification_SendsEmailSuccessfully()
    {
        // Arrange
        var emailService = Substitute.For<IEmailService>();
        emailService
            .SendWishlistUpdatedEmailAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(EmailResult.Success());

        var customFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["EmailNotificationWorker:Enabled"] = "false"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IEmailService>();
                services.AddScoped<IEmailService>(_ => emailService);
            });
        });

        using var scope = customFactory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var processor = scope.ServiceProvider.GetRequiredService<IEmailNotificationProcessor>();

        // Create test data
        var santa = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "santa@example.com",
            FirstName = "Santa",
            LastName = "Claus",
            UserName = "santa@example.com",
        };

        var recipient = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "recipient@example.com",
            FirstName = "Jane",
            LastName = "Smith",
            UserName = "recipient@example.com",
        };

        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "Test Group",
            OrganizerUserId =santa.Id,
            DrawCompletedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var assignment = new Assignment
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            SantaUserId = santa.Id,
            RecipientUserId = recipient.Id,
            AssignedAt = DateTimeOffset.UtcNow
        };

        var notification = new EmailNotification
        {
            Id = Guid.NewGuid(),
            Type = EmailNotificationType.WishlistUpdated,
            RecipientUserId = santa.Id,
            GroupId = group.Id,
            ScheduledAt = DateTimeOffset.UtcNow.AddSeconds(-1),
            SentAt = null,
            AttemptCount = 0
        };

        await dbContext.Users.AddAsync(santa);
        await dbContext.Users.AddAsync(recipient);
        await dbContext.Groups.AddAsync(group);
        await dbContext.Assignments.AddAsync(assignment);
        await dbContext.EmailNotifications.AddAsync(notification);
        await dbContext.SaveChangesAsync();

        // Setup event handler
        var completionSource = new TaskCompletionSource<bool>();
        processor.OnIterationCompleted += (sender, args) => completionSource.SetResult(true);

        // Act
        await processor.ProcessPendingNotificationsAsync();
        await completionSource.Task;

        // Assert
        var processedNotification = await dbContext.EmailNotifications
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == notification.Id);

        Assert.NotNull(processedNotification);
        Assert.NotNull(processedNotification.SentAt);
        Assert.Equal(1, processedNotification.AttemptCount);
        Assert.Null(processedNotification.LastError);

        await emailService.Received(1).SendWishlistUpdatedEmailAsync(
            "santa@example.com",
            "Santa Claus",
            "Test Group",
            group.Id,
            "Jane",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessPendingNotifications_WhenEmailFails_SchedulesRetryWithExponentialBackoff()
    {
        // Arrange
        var emailService = Substitute.For<IEmailService>();
        emailService
            .SendDrawCompletedEmailAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(EmailResult.Failure("Service unavailable", "503"));

        var customFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["EmailNotificationWorker:Enabled"] = "false"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IEmailService>();
                services.AddScoped<IEmailService>(_ => emailService);
            });
        });

        using var scope = customFactory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var processor = scope.ServiceProvider.GetRequiredService<IEmailNotificationProcessor>();

        // Create test data
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            UserName = "test@example.com",
        };

        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "Test Group",
            OrganizerUserId =user.Id,
            DrawCompletedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var notification = new EmailNotification
        {
            Id = Guid.NewGuid(),
            Type = EmailNotificationType.DrawCompleted,
            RecipientUserId = user.Id,
            GroupId = group.Id,
            ScheduledAt = DateTimeOffset.UtcNow.AddSeconds(-1),
            SentAt = null,
            AttemptCount = 0
        };

        await dbContext.Users.AddAsync(user);
        await dbContext.Groups.AddAsync(group);
        await dbContext.EmailNotifications.AddAsync(notification);
        await dbContext.SaveChangesAsync();

        // Setup event handler
        var completionSource = new TaskCompletionSource<bool>();
        processor.OnIterationCompleted += (sender, args) => completionSource.SetResult(true);

        // Act
        await processor.ProcessPendingNotificationsAsync();
        await completionSource.Task;

        // Assert
        var failedNotification = await dbContext.EmailNotifications
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == notification.Id);

        Assert.NotNull(failedNotification);
        Assert.Null(failedNotification.SentAt); // Not sent
        Assert.Equal(1, failedNotification.AttemptCount);
        Assert.NotNull(failedNotification.LastError);
        Assert.Contains("503", failedNotification.LastError);
        Assert.Contains("Service unavailable", failedNotification.LastError);

        // Verify retry is scheduled with exponential backoff (60 seconds for first retry)
        var retryDelay = failedNotification.ScheduledAt - failedNotification.LastAttemptAt!.Value;
        Assert.True(retryDelay.TotalSeconds >= 55 && retryDelay.TotalSeconds <= 65);
    }

    [Fact]
    public async Task ProcessPendingNotifications_AfterMaxRetries_StopsRetrying()
    {
        // Arrange
        var emailService = Substitute.For<IEmailService>();
        emailService
            .SendDrawCompletedEmailAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(EmailResult.Failure("Permanent failure", "500"));

        var customFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["EmailNotificationWorker:Enabled"] = "false"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IEmailService>();
                services.AddScoped<IEmailService>(_ => emailService);
            });
        });

        using var scope = customFactory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var processor = scope.ServiceProvider.GetRequiredService<IEmailNotificationProcessor>();

        // Create test data with 4 previous attempts
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            UserName = "test@example.com",
        };

        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "Test Group",
            OrganizerUserId =user.Id,
            DrawCompletedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var notification = new EmailNotification
        {
            Id = Guid.NewGuid(),
            Type = EmailNotificationType.DrawCompleted,
            RecipientUserId = user.Id,
            GroupId = group.Id,
            ScheduledAt = DateTimeOffset.UtcNow.AddSeconds(-1),
            SentAt = null,
            AttemptCount = 4, // 4th attempt, next will be 5th (max)
            LastError = "Previous failure"
        };

        await dbContext.Users.AddAsync(user);
        await dbContext.Groups.AddAsync(group);
        await dbContext.EmailNotifications.AddAsync(notification);
        await dbContext.SaveChangesAsync();

        // Setup event handler
        var completionSource = new TaskCompletionSource<bool>();
        processor.OnIterationCompleted += (sender, args) => completionSource.SetResult(true);

        // Act
        await processor.ProcessPendingNotificationsAsync();
        await completionSource.Task;

        // Assert
        var finalNotification = await dbContext.EmailNotifications
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == notification.Id);

        Assert.NotNull(finalNotification);
        Assert.Null(finalNotification.SentAt); // Still not sent
        Assert.Equal(5, finalNotification.AttemptCount); // Reached max attempts
        Assert.NotNull(finalNotification.LastError);

        // Scheduled time should not be updated after max attempts
        var timeSinceLastAttempt = DateTimeOffset.UtcNow - finalNotification.LastAttemptAt!.Value;
        Assert.True(timeSinceLastAttempt.TotalSeconds < 5);
    }

    [Fact]
    public async Task ProcessPendingNotifications_WithMultipleNotifications_ProcessesAllInBatch()
    {
        // Arrange
        var emailService = Substitute.For<IEmailService>();
        emailService
            .SendDrawCompletedEmailAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(EmailResult.Success());

        var customFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["EmailNotificationWorker:Enabled"] = "false"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IEmailService>();
                services.AddScoped<IEmailService>(_ => emailService);
            });
        });

        using var scope = customFactory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var processor = scope.ServiceProvider.GetRequiredService<IEmailNotificationProcessor>();

        // Create test data - organizer + 3 users and 3 notifications
        var organizer = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "organizer@example.com",
            FirstName = "Organizer",
            LastName = "Test",
            UserName = "organizer@example.com"
        };
        await dbContext.Users.AddAsync(organizer);

        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "Test Group",
            OrganizerUserId = organizer.Id,
            DrawCompletedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await dbContext.Groups.AddAsync(group);

        for (int i = 0; i < 3; i++)
        {
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                Email = $"user{i}@example.com",
                FirstName = $"User{i}",
                LastName = "Test",
                UserName = $"user{i}@example.com"
            };
            await dbContext.Users.AddAsync(user);

            var notification = new EmailNotification
            {
                Id = Guid.NewGuid(),
                Type = EmailNotificationType.DrawCompleted,
                RecipientUserId = user.Id,
                GroupId = group.Id,
                ScheduledAt = DateTimeOffset.UtcNow.AddSeconds(-1),
                SentAt = null,
                AttemptCount = 0
            };
            await dbContext.EmailNotifications.AddAsync(notification);
        }

        await dbContext.SaveChangesAsync();

        // Setup event handler
        var completionSource = new TaskCompletionSource<bool>();
        processor.OnIterationCompleted += (sender, args) => completionSource.SetResult(true);

        // Act
        await processor.ProcessPendingNotificationsAsync();
        await completionSource.Task;

        // Assert
        var processedNotifications = await dbContext.EmailNotifications
            .AsNoTracking()
            .Where(n => n.GroupId == group.Id)
            .ToListAsync();

        Assert.Equal(3, processedNotifications.Count);
        Assert.All(processedNotifications, n =>
        {
            Assert.NotNull(n.SentAt);
            Assert.Equal(1, n.AttemptCount);
            Assert.Null(n.LastError);
        });

        // Verify 3 emails were sent
        await emailService.Received(3).SendDrawCompletedEmailAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessPendingNotifications_WithFutureScheduledNotification_SkipsProcessing()
    {
        // Arrange
        var emailService = Substitute.For<IEmailService>();

        var customFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["EmailNotificationWorker:Enabled"] = "false"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IEmailService>();
                services.AddScoped<IEmailService>(_ => emailService);
            });
        });

        using var scope = customFactory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var processor = scope.ServiceProvider.GetRequiredService<IEmailNotificationProcessor>();

        // Create test data
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            UserName = "test@example.com",
        };

        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "Test Group",
            OrganizerUserId =user.Id,
            DrawCompletedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var futureNotification = new EmailNotification
        {
            Id = Guid.NewGuid(),
            Type = EmailNotificationType.WishlistUpdated,
            RecipientUserId = user.Id,
            GroupId = group.Id,
            ScheduledAt = DateTimeOffset.UtcNow.AddHours(1), // Future
            SentAt = null,
            AttemptCount = 0
        };

        await dbContext.Users.AddAsync(user);
        await dbContext.Groups.AddAsync(group);
        await dbContext.EmailNotifications.AddAsync(futureNotification);
        await dbContext.SaveChangesAsync();

        // Setup event handler
        var completionSource = new TaskCompletionSource<bool>();
        processor.OnIterationCompleted += (sender, args) => completionSource.SetResult(true);

        // Act
        await processor.ProcessPendingNotificationsAsync();
        await completionSource.Task;

        // Assert
        var notification = await dbContext.EmailNotifications
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == futureNotification.Id);

        Assert.NotNull(notification);
        Assert.Null(notification.SentAt); // Should not be sent
        Assert.Equal(0, notification.AttemptCount); // Should not have been attempted

        // Verify no emails were sent
        await emailService.DidNotReceive().SendDrawCompletedEmailAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessPendingNotifications_WhenEmailServiceThrowsException_HandlesGracefully()
    {
        // Arrange - Create a custom email service that throws
        var emailService = Substitute.For<IEmailService>();
        var exceptionThrown = false;

        emailService
            .SendDrawCompletedEmailAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<EmailResult>>(callInfo =>
            {
                exceptionThrown = true;
                throw new InvalidOperationException("Unexpected error");
            });

        var customFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["EmailNotificationWorker:Enabled"] = "false"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IEmailService>();
                services.AddScoped<IEmailService>(_ => emailService);
            });
        });

        using var scope = customFactory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var processor = scope.ServiceProvider.GetRequiredService<IEmailNotificationProcessor>();

        // Create test data
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            UserName = "test@example.com"
        };

        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "Test Group",
            OrganizerUserId = user.Id,
            DrawCompletedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var notification = new EmailNotification
        {
            Id = Guid.NewGuid(),
            Type = EmailNotificationType.DrawCompleted,
            RecipientUserId = user.Id,
            GroupId = group.Id,
            ScheduledAt = DateTimeOffset.UtcNow.AddSeconds(-1),
            SentAt = null,
            AttemptCount = 0
        };

        await dbContext.Users.AddAsync(user);
        await dbContext.Groups.AddAsync(group);
        await dbContext.EmailNotifications.AddAsync(notification);
        await dbContext.SaveChangesAsync();

        // Setup event handler
        var completionSource = new TaskCompletionSource<bool>();
        processor.OnIterationCompleted += (sender, args) => completionSource.SetResult(true);

        // Act
        await processor.ProcessPendingNotificationsAsync();
        await completionSource.Task;

        // Assert
        Assert.True(exceptionThrown, "Exception should have been thrown by email service");

        var failedNotification = await dbContext.EmailNotifications
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == notification.Id);

        Assert.NotNull(failedNotification);
        Assert.Null(failedNotification.SentAt);
        Assert.Equal(1, failedNotification.AttemptCount);
        Assert.NotNull(failedNotification.LastError);
        Assert.Contains("Unexpected error", failedNotification.LastError);

        // Verify retry is scheduled
        var retryDelay = failedNotification.ScheduledAt - failedNotification.LastAttemptAt!.Value;
        Assert.True(retryDelay.TotalSeconds >= 55 && retryDelay.TotalSeconds <= 65);
    }
}
