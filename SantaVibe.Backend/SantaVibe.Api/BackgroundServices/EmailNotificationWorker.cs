using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SantaVibe.Api.Data;
using SantaVibe.Api.Data.Entities;
using SantaVibe.Api.Services.Email;

namespace SantaVibe.Api.BackgroundServices;

/// <summary>
/// Background service that processes pending email notifications from the queue
/// </summary>
public class EmailNotificationWorker(
    IServiceProvider serviceProvider,
    IOptions<EmailNotificationWorkerOptions> options,
    ILogger<EmailNotificationWorker> logger) : BackgroundService, IEmailNotificationProcessor
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<EmailNotificationWorker> _logger = logger;
    private readonly EmailNotificationWorkerOptions _options = options.Value;

    /// <summary>
    /// Event raised when a processing iteration completes (for testing)
    /// </summary>
    public event EventHandler? OnIterationCompleted;

    /// <summary>
    /// Event raised when an error occurs during processing (for testing)
    /// </summary>
    public event EventHandler<Exception>? OnError;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Email Notification Worker starting with polling interval {Interval}s",
            _options.PollingIntervalSeconds);

        // Wait 10 seconds before first poll to allow app startup
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingNotificationsAsync(stoppingToken);
                OnIterationCompleted?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing email notifications");
                OnError?.Invoke(this, ex);
            }

            await Task.Delay(
                TimeSpan.FromSeconds(_options.PollingIntervalSeconds),
                stoppingToken);
        }

        _logger.LogInformation("Email Notification Worker stopping");
    }

    public async Task ProcessPendingNotificationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            // Query pending notifications
            var now = DateTimeOffset.UtcNow;
            var notifications = await dbContext.EmailNotifications
                .Include(n => n.Recipient)
                .Include(n => n.Group)
                .Where(n => n.SentAt == null
                         && n.ScheduledAt <= now
                         && n.AttemptCount < _options.MaxRetryAttempts)
                .OrderBy(n => n.ScheduledAt)
                .Take(_options.BatchSize)
                .ToListAsync(cancellationToken);

            if (notifications.Count == 0)
            {
                OnIterationCompleted?.Invoke(this, EventArgs.Empty);
                return;
            }

            _logger.LogInformation(
                "Processing {Count} pending email notification(s)",
                notifications.Count);

            foreach (var notification in notifications)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                await ProcessNotificationAsync(notification, emailService, dbContext, cancellationToken);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            OnIterationCompleted?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, ex);
            throw;
        }
    }

    private async Task ProcessNotificationAsync(
        EmailNotification notification,
        IEmailService emailService,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        // Track first attempt
        if (notification.FirstAttemptAt == null)
        {
            notification.FirstAttemptAt = now;
        }

        notification.LastAttemptAt = now;
        notification.AttemptCount++;

        try
        {
            EmailResult result = notification.Type switch
            {
                EmailNotificationType.DrawCompleted => await emailService.SendDrawCompletedEmailAsync(
                    notification.Recipient.Email!,
                    $"{notification.Recipient.FirstName} {notification.Recipient.LastName}",
                    notification.Group.Name,
                    notification.GroupId,
                    cancellationToken),

                EmailNotificationType.WishlistUpdated => await SendWishlistUpdatedEmailAsync(
                    notification,
                    emailService,
                    dbContext,
                    cancellationToken),

                _ => throw new InvalidOperationException($"Unknown notification type: {notification.Type}")
            };

            if (result.IsSuccess)
            {
                notification.SentAt = now;
                notification.LastError = null;

                _logger.LogInformation(
                    "Email notification {Id} ({Type}) sent successfully to {Email} on attempt {Attempt}",
                    notification.Id,
                    notification.Type,
                    notification.Recipient.Email,
                    notification.AttemptCount);
            }
            else
            {
                notification.LastError = $"[{result.ErrorCode}] {result.ErrorMessage}";

                _logger.LogWarning(
                    "Email notification {Id} ({Type}) failed on attempt {Attempt}/{Max}: {Error}",
                    notification.Id,
                    notification.Type,
                    notification.AttemptCount,
                    _options.MaxRetryAttempts,
                    notification.LastError);

                // Schedule retry with exponential backoff
                if (notification.AttemptCount < _options.MaxRetryAttempts)
                {
                    var delaySeconds = CalculateRetryDelay(notification.AttemptCount);
                    notification.ScheduledAt = now.AddSeconds(delaySeconds);

                    _logger.LogInformation(
                        "Scheduling retry for notification {Id} in {Delay} seconds",
                        notification.Id,
                        delaySeconds);
                }
                else
                {
                    _logger.LogError(
                        "Email notification {Id} ({Type}) exceeded max retry attempts ({Max}). Giving up.",
                        notification.Id,
                        notification.Type,
                        _options.MaxRetryAttempts);
                }
            }
        }
        catch (Exception ex)
        {
            notification.LastError = $"Exception: {ex.Message}";

            _logger.LogError(
                ex,
                "Unexpected error processing notification {Id} ({Type})",
                notification.Id,
                notification.Type);

            // Schedule retry
            if (notification.AttemptCount < _options.MaxRetryAttempts)
            {
                var delaySeconds = CalculateRetryDelay(notification.AttemptCount);
                notification.ScheduledAt = now.AddSeconds(delaySeconds);
            }
        }
    }

    private async Task<EmailResult> SendWishlistUpdatedEmailAsync(
        EmailNotification notification,
        IEmailService emailService,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        // Find the recipient who updated their wishlist
        var recipient = await dbContext.Assignments
            .Where(a => a.GroupId == notification.GroupId
                     && a.SantaUserId == notification.RecipientUserId)
            .Select(a => a.Recipient.FirstName)
            .FirstOrDefaultAsync(cancellationToken);

        if (recipient == null)
        {
            return EmailResult.Failure("Assignment not found for wishlist notification", "AssignmentNotFound");
        }

        return await emailService.SendWishlistUpdatedEmailAsync(
            notification.Recipient.Email!,
            $"{notification.Recipient.FirstName} {notification.Recipient.LastName}",
            notification.Group.Name,
            notification.GroupId,
            recipient,
            cancellationToken);
    }

    private int CalculateRetryDelay(int attemptCount)
    {
        // Exponential backoff: 60s, 120s, 240s, 480s, 960s
        return _options.InitialRetryDelaySeconds * (int)Math.Pow(2, attemptCount - 1);
    }
}

/// <summary>
/// Configuration options for email notification worker
/// </summary>
public class EmailNotificationWorkerOptions
{
    public const string SectionName = "EmailNotificationWorker";

    public bool Enabled { get; set; } = true;
    public int PollingIntervalSeconds { get; set; } = 30;
    public int MaxRetryAttempts { get; set; } = 5;
    public int InitialRetryDelaySeconds { get; set; } = 60;
    public int BatchSize { get; set; } = 50;
}

/// <summary>
/// Manual trigger for processing notifications (used in tests)
/// </summary>
public interface IEmailNotificationProcessor
{
    /// <summary>
    /// Manually process pending notifications (for testing)
    /// </summary>
    Task ProcessPendingNotificationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when a processing iteration completes
    /// </summary>
    event EventHandler? OnIterationCompleted;

    /// <summary>
    /// Event raised when an error occurs during processing
    /// </summary>
    event EventHandler<Exception>? OnError;
}
