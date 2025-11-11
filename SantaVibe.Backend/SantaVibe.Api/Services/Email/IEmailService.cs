namespace SantaVibe.Api.Services.Email;

/// <summary>
/// Service for sending email notifications via external provider
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends draw completion notification to a participant
    /// </summary>
    /// <param name="recipientEmail">Recipient email address</param>
    /// <param name="recipientName">Recipient full name</param>
    /// <param name="groupName">Name of the Secret Santa group</param>
    /// <param name="groupId">Group identifier for generating links</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure with error details</returns>
    Task<EmailResult> SendDrawCompletedEmailAsync(
        string recipientEmail,
        string recipientName,
        string groupName,
        Guid groupId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends wishlist update notification to assigned Santa
    /// </summary>
    /// <param name="recipientEmail">Santa's email address</param>
    /// <param name="recipientName">Santa's full name</param>
    /// <param name="groupName">Name of the Secret Santa group</param>
    /// <param name="groupId">Group identifier for generating links</param>
    /// <param name="recipientFirstName">Name of person who updated wishlist</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure with error details</returns>
    Task<EmailResult> SendWishlistUpdatedEmailAsync(
        string recipientEmail,
        string recipientName,
        string groupName,
        Guid groupId,
        string recipientFirstName,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of email sending operation
/// </summary>
public sealed record EmailResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ErrorCode { get; init; }

    public static EmailResult Success() => new() { IsSuccess = true };

    public static EmailResult Failure(string errorMessage, string? errorCode = null) =>
        new()
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode
        };
}
