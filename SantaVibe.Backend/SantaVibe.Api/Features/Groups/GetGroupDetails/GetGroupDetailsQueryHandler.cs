using MediatR;
using Microsoft.EntityFrameworkCore;
using SantaVibe.Api.Common;
using SantaVibe.Api.Data;
using SantaVibe.Api.Services;

namespace SantaVibe.Api.Features.Groups.GetGroupDetails;

/// <summary>
/// Handler for GetGroupDetailsQuery
/// Retrieves detailed information about a Secret Santa group with authorization checks
/// </summary>
public class GetGroupDetailsQueryHandler(
    ApplicationDbContext context,
    IUserAccessor userAccessor,
    IConfiguration configuration)
    : IRequestHandler<GetGroupDetailsQuery, Result<GetGroupDetailsResponse>>
{
    public async Task<Result<GetGroupDetailsResponse>> Handle(
        GetGroupDetailsQuery request,
        CancellationToken cancellationToken)
    {
        // Get current user ID from JWT token
        var currentUserId = userAccessor.GetCurrentUserId();

        // Query group with necessary includes
        var group = await context.Groups
            .AsNoTracking()
            .Include(g => g.Organizer)
            .Include(g => g.GroupParticipants)
                .ThenInclude(gp => gp.User)
            .FirstOrDefaultAsync(g => g.Id == request.GroupId, cancellationToken);

        // Check if group exists
        if (group == null)
        {
            return Result<GetGroupDetailsResponse>.Failure(
                "GroupNotFound",
                "Group does not exist"
            );
        }

        // Check if user is a participant in the group
        var isParticipant = group.GroupParticipants
            .Any(gp => gp.UserId == currentUserId.ToString());

        if (!isParticipant)
        {
            return Result<GetGroupDetailsResponse>.Failure(
                "Forbidden",
                "You are not a participant in this group"
            );
        }

        // Determine if current user is the organizer
        var isOrganizer = group.OrganizerUserId == currentUserId.ToString();

        // Calculate participant count
        var participantCount = group.GroupParticipants.Count;

        // Build response based on draw status
        GetGroupDetailsResponse response;

        if (group.DrawCompletedAt == null)
        {
            // Before draw: Include participants, exclusion rules, and validation
            response = await BuildBeforeDrawResponse(
                group,
                isOrganizer,
                participantCount,
                cancellationToken);
        }
        else
        {
            // After draw: Include user's assignment only
            response = await BuildAfterDrawResponse(
                group,
                currentUserId,
                isOrganizer,
                participantCount,
                cancellationToken);
        }

        return Result<GetGroupDetailsResponse>.Success(response);
    }

    /// <summary>
    /// Builds response for groups where the draw has not been completed
    /// Includes full participant list, exclusion rule count, and draw validation
    /// </summary>
    private async Task<GetGroupDetailsResponse> BuildBeforeDrawResponse(
        Data.Entities.Group group,
        bool isOrganizer,
        int participantCount,
        CancellationToken cancellationToken)
    {
        // Map participants to DTOs
        var participants = group.GroupParticipants
            .Select(gp => new ParticipantDto
            {
                UserId = gp.UserId,
                FirstName = gp.User.FirstName,
                LastName = gp.User.LastName,
                JoinedAt = gp.JoinedAt,
                HasWishlist = !string.IsNullOrWhiteSpace(gp.WishlistContent),
                IsOrganizer = gp.UserId == group.OrganizerUserId
            })
            .OrderBy(p => p.JoinedAt)
            .ToList();

        // Count exclusion rules for this group
        var exclusionRuleCount = await context.ExclusionRules
            .CountAsync(er => er.GroupId == group.Id, cancellationToken);

        // Build invitation link for organizer
        string? invitationLink = null;
        if (isOrganizer)
        {
            var baseUrl = configuration["App:BaseUrl"] ?? "https://santavibe.com";
            invitationLink = $"{baseUrl}/invite/{group.InvitationToken}";
        }

        // Validate draw feasibility
        var drawValidation = ValidateDraw(participantCount);

        return new GetGroupDetailsResponse
        {
            // Common fields
            GroupId = group.Id,
            Name = group.Name,
            OrganizerId = group.OrganizerUserId,
            OrganizerName = $"{group.Organizer.FirstName} {group.Organizer.LastName}",
            IsOrganizer = isOrganizer,
            Budget = group.Budget,
            DrawCompleted = false,
            DrawCompletedAt = null,
            CreatedAt = group.CreatedAt,
            ParticipantCount = participantCount,

            // Before draw fields
            Participants = participants,
            ExclusionRuleCount = exclusionRuleCount,
            InvitationLink = invitationLink,
            CanDraw = drawValidation.IsValid,
            DrawValidation = drawValidation,

            // After draw fields (null)
            MyAssignment = null
        };
    }

    /// <summary>
    /// Builds response for groups where the draw has been completed
    /// Includes user's assignment (who they are buying a gift for)
    /// </summary>
    private async Task<GetGroupDetailsResponse> BuildAfterDrawResponse(
        Data.Entities.Group group,
        Guid currentUserId,
        bool isOrganizer,
        int participantCount,
        CancellationToken cancellationToken)
    {
        // Query user's assignment
        var assignment = await context.Assignments
            .AsNoTracking()
            .Include(a => a.Recipient)
            .FirstOrDefaultAsync(a =>
                a.GroupId == group.Id &&
                a.SantaUserId == currentUserId.ToString(),
                cancellationToken);

        // Map assignment to DTO (should always exist if draw is completed and user is participant)
        AssignmentDto? myAssignment = null;
        if (assignment != null)
        {
            // Get recipient's wishlist status from GroupParticipant
            var recipientParticipant = await context.GroupParticipants
                .AsNoTracking()
                .FirstOrDefaultAsync(gp =>
                    gp.GroupId == group.Id &&
                    gp.UserId == assignment.RecipientUserId,
                    cancellationToken);

            myAssignment = new AssignmentDto
            {
                RecipientId = assignment.RecipientUserId,
                RecipientFirstName = assignment.Recipient.FirstName,
                RecipientLastName = assignment.Recipient.LastName,
                HasWishlist = recipientParticipant != null &&
                             !string.IsNullOrWhiteSpace(recipientParticipant.WishlistContent)
            };
        }

        return new GetGroupDetailsResponse
        {
            // Common fields
            GroupId = group.Id,
            Name = group.Name,
            OrganizerId = group.OrganizerUserId,
            OrganizerName = $"{group.Organizer.FirstName} {group.Organizer.LastName}",
            IsOrganizer = isOrganizer,
            Budget = group.Budget,
            DrawCompleted = true,
            DrawCompletedAt = group.DrawCompletedAt,
            CreatedAt = group.CreatedAt,
            ParticipantCount = participantCount,

            // Before draw fields (null)
            Participants = null,
            ExclusionRuleCount = null,
            InvitationLink = null,
            CanDraw = null,
            DrawValidation = null,

            // After draw fields
            MyAssignment = myAssignment
        };
    }

    /// <summary>
    /// Validates whether a draw can be performed
    /// For MVP: Checks minimum participant count (3 required)
    /// Future: Can be enhanced with exclusion rule graph validation
    /// </summary>
    private static DrawValidationDto ValidateDraw(int participantCount)
    {
        var errors = new List<string>();

        if (participantCount < 3)
        {
            errors.Add("Minimum 3 participants required for draw");
        }

        return new DrawValidationDto
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }
}
