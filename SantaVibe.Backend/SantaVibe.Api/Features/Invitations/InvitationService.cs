using Microsoft.EntityFrameworkCore;
using SantaVibe.Api.Data;
using SantaVibe.Api.Features.Invitations.GetInvitationDetails;

namespace SantaVibe.Api.Features.Invitations;

/// <summary>
/// Service for invitation-related operations
/// </summary>
public sealed class InvitationService(
    ApplicationDbContext context,
    ILogger<InvitationService> logger) : IInvitationService
{
    /// <inheritdoc />
    public async Task<GetInvitationDetailsResponse?> GetInvitationDetailsAsync(
        Guid token,
        CancellationToken cancellationToken = default)
    {
        // Query database for group with matching invitation token
        // Use AsNoTracking() for read-only query optimization
        // Eager load Organizer and GroupParticipants to avoid N+1 queries
        var group = await context.Groups
            .AsNoTracking()
            .Include(g => g.Organizer)
            .Include(g => g.GroupParticipants)
            .FirstOrDefaultAsync(g => g.InvitationToken == token, cancellationToken);

        // Return null if group not found (caller will return 404)
        if (group is null)
        {
            logger.LogWarning("Invitation token {Token} not found", token);
            return null;
        }

        // Build organizer full name
        var organizerName = $"{group.Organizer.FirstName} {group.Organizer.LastName}";

        // Calculate participant count
        var participantCount = group.GroupParticipants.Count;

        // Check if draw has been completed
        var drawCompleted = group.DrawCompletedAt.HasValue;

        // Log successful retrieval for analytics
        logger.LogInformation(
            "Retrieved invitation details for token {Token}, Group: {GroupName}, Draw completed: {DrawCompleted}",
            token,
            group.Name,
            drawCompleted);

        // Map to response DTO
        return new GetInvitationDetailsResponse(
            InvitationToken: group.InvitationToken,
            GroupId: group.Id,
            GroupName: group.Name,
            OrganizerName: organizerName,
            ParticipantCount: participantCount,
            DrawCompleted: drawCompleted,
            IsValid: true
        );
    }
}
