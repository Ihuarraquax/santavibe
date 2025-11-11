namespace SantaVibe.Api.Services.Notifications;

/// <summary>
/// Service for scheduling wishlist update notifications with deduplication
/// </summary>
public interface IWishlistNotificationService
{
    /// <summary>
    /// Schedules a wishlist update notification with 1-hour delay and deduplication
    /// </summary>
    /// <param name="groupId">Group identifier</param>
    /// <param name="recipientUserId">User who updated their wishlist</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if notification was scheduled, false if duplicate was found</returns>
    Task<bool> ScheduleWishlistUpdateNotificationAsync(
        Guid groupId,
        string recipientUserId,
        CancellationToken cancellationToken = default);
}
