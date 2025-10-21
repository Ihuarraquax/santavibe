namespace SantaVibe.Api.Features.Invitations;

/// <summary>
/// Service interface for invitation-related operations
/// </summary>
public interface IInvitationService
{
    /// <summary>
    /// Retrieves group information for a given invitation token
    /// </summary>
    /// <param name="token">The invitation token (UUID)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>
    /// Returns GetInvitationDetailsResponse if token is found, null if token is invalid
    /// </returns>
    Task<GetInvitationDetails.GetInvitationDetailsResponse?> GetInvitationDetailsAsync(
        Guid token,
        CancellationToken cancellationToken = default);
}
