using MediatR;
using Microsoft.EntityFrameworkCore;
using SantaVibe.Api.Data;
using SantaVibe.Api.Data.Entities;

namespace SantaVibe.Api.Features.Wishlists.UpdateWishlist;

/// <summary>
/// Handles wishlist update events by scheduling email notifications to the assigned Santa
/// Implements 1-hour delay and deduplication logic
/// </summary>
public class WishlistUpdatedNotificationHandler : INotificationHandler<WishlistUpdatedNotification>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<WishlistUpdatedNotificationHandler> _logger;

    public WishlistUpdatedNotificationHandler(
        ApplicationDbContext context,
        ILogger<WishlistUpdatedNotificationHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Handle(WishlistUpdatedNotification notification, CancellationToken cancellationToken)
    {
        // Find the user's Santa (the person who will buy them a gift)
        var assignment = await _context.Assignments
            .AsNoTracking()
            .Where(a => a.GroupId == notification.GroupId && a.RecipientUserId == notification.RecipientUserId)
            .Select(a => new { a.SantaUserId })
            .FirstOrDefaultAsync(cancellationToken);

        if (assignment == null)
        {
            // No assignment found - shouldn't happen if draw is completed, but handle gracefully
            _logger.LogWarning(
                "No assignment found for user {UserId} in group {GroupId} despite draw being completed",
                notification.RecipientUserId,
                notification.GroupId);
            return;
        }

        // Check for existing pending notification within 1-hour window (deduplication)
        var oneHourFromNow = DateTimeOffset.UtcNow.AddHours(1);
        var hasPendingNotification = await _context.EmailNotifications
            .AsNoTracking()
            .Where(n =>
                n.Type == EmailNotificationType.WishlistUpdated &&
                n.GroupId == notification.GroupId &&
                n.RecipientUserId == assignment.SantaUserId &&
                n.SentAt == null &&
                n.ScheduledAt <= oneHourFromNow)
            .AnyAsync(cancellationToken);

        if (hasPendingNotification)
        {
            _logger.LogInformation(
                "Skipped duplicate wishlist notification for Santa {SantaUserId} in group {GroupId}",
                assignment.SantaUserId,
                notification.GroupId);
            return;
        }

        // Create new email notification with 1-hour delay
        var emailNotification = new EmailNotification
        {
            Id = Guid.NewGuid(),
            Type = EmailNotificationType.WishlistUpdated,
            RecipientUserId = assignment.SantaUserId,
            GroupId = notification.GroupId,
            ScheduledAt = DateTimeOffset.UtcNow.AddHours(1),
            SentAt = null,
            AttemptCount = 0
        };

        _context.EmailNotifications.Add(emailNotification);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Scheduled wishlist update notification for Santa {SantaUserId} in group {GroupId}",
            assignment.SantaUserId,
            notification.GroupId);
    }
}
