namespace SantaVibe.Api.Data.Entities;

/// <summary>
/// Types of email notifications sent by the system
/// </summary>
public enum EmailNotificationType
{
    /// <summary>
    /// Sent to all participants when draw is executed (FR-018)
    /// </summary>
    DrawCompleted,

    /// <summary>
    /// Sent to Santa when recipient updates wishlist (FR-019)
    /// </summary>
    WishlistUpdated
}
