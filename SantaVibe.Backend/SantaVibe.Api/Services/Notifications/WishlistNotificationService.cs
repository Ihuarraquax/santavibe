using Microsoft.EntityFrameworkCore;
using SantaVibe.Api.Data;
using SantaVibe.Api.Data.Entities;

namespace SantaVibe.Api.Services.Notifications;

/// <summary>
/// Service for scheduling wishlist update notifications with deduplication
/// </summary>
public class WishlistNotificationService(
    ApplicationDbContext dbContext,
    ILogger<WishlistNotificationService> logger) : IWishlistNotificationService
{
    public async Task<bool> ScheduleWishlistUpdateNotificationAsync(
        Guid groupId,
        string recipientUserId,
        CancellationToken cancellationToken = default)
    {
        // Find the assigned Santa for this recipient
        var assignment = await dbContext.Assignments
            .FirstOrDefaultAsync(
                a => a.GroupId == groupId && a.RecipientUserId == recipientUserId,
                cancellationToken);

        if (assignment == null)
        {
            logger.LogWarning(
                "No assignment found for recipient {UserId} in group {GroupId}. Skipping notification.",
                recipientUserId,
                groupId);
            return false;
        }

        var santaUserId = assignment.SantaUserId;

        // Check for existing pending notification within 1-hour window
        var oneHourFromNow = DateTimeOffset.UtcNow.AddHours(1);
        var existingNotification = await dbContext.EmailNotifications
            .AnyAsync(
                n => n.GroupId == groupId
                     && n.RecipientUserId == santaUserId
                     && n.Type == EmailNotificationType.WishlistUpdated
                     && n.SentAt == null
                     && n.ScheduledAt <= oneHourFromNow,
                cancellationToken);

        if (existingNotification)
        {
            logger.LogInformation(
                "Skipping duplicate wishlist notification for Santa {SantaId} in group {GroupId}",
                santaUserId,
                groupId);
            return false;
        }

        // Schedule new notification with 1-hour delay
        var notification = new EmailNotification
        {
            Id = Guid.NewGuid(),
            Type = EmailNotificationType.WishlistUpdated,
            RecipientUserId = santaUserId,
            GroupId = groupId,
            ScheduledAt = DateTimeOffset.UtcNow.AddHours(1),
            SentAt = null,
            FirstAttemptAt = null,
            LastAttemptAt = null,
            AttemptCount = 0,
            LastError = null
        };

        await dbContext.EmailNotifications.AddAsync(notification, cancellationToken);

        logger.LogInformation(
            "Scheduled wishlist update notification {NotificationId} for Santa {SantaId} in group {GroupId}",
            notification.Id,
            santaUserId,
            groupId);

        return true;
    }
}
