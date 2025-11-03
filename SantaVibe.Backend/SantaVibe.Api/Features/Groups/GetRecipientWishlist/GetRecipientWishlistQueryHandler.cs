using MediatR;
using Microsoft.EntityFrameworkCore;
using SantaVibe.Api.Common;
using SantaVibe.Api.Data;
using SantaVibe.Api.Services;

namespace SantaVibe.Api.Features.Groups.GetRecipientWishlist;

/// <summary>
/// Handler for GetRecipientWishlistQuery.
/// Retrieves the assigned recipient's wishlist with authorization checks.
/// </summary>
public class GetRecipientWishlistQueryHandler(
    ApplicationDbContext context,
    IUserAccessor userAccessor)
    : IRequestHandler<GetRecipientWishlistQuery, Result<GetRecipientWishlistResponse>>
{
    public async Task<Result<GetRecipientWishlistResponse>> Handle(
        GetRecipientWishlistQuery request,
        CancellationToken cancellationToken)
    {
        // Get current user ID from JWT token
        var currentUserId = userAccessor.GetCurrentUserId();

        // Query group with participants to verify access
        var group = await context.Groups
            .AsNoTracking()
            .Include(g => g.GroupParticipants)
            .FirstOrDefaultAsync(g => g.Id == request.GroupId, cancellationToken);

        // Check if group exists
        if (group == null)
        {
            return Result<GetRecipientWishlistResponse>.Failure(
                "GroupNotFound",
                "Group does not exist"
            );
        }

        // Check if user is a participant
        var isParticipant = group.GroupParticipants
            .Any(gp => gp.UserId == currentUserId.ToString());

        if (!isParticipant)
        {
            return Result<GetRecipientWishlistResponse>.Failure(
                "NotAParticipant",
                "You are not a participant in this group"
            );
        }

        // Check if draw has been completed
        if (group.DrawCompletedAt == null)
        {
            return Result<GetRecipientWishlistResponse>.Failure(
                "DrawNotCompleted",
                "Draw has not been completed yet. You cannot view recipient wishlist."
            );
        }

        // Query user's assignment (where they are the Santa)
        var assignment = await context.Assignments
            .AsNoTracking()
            .Include(a => a.Recipient)
            .FirstOrDefaultAsync(a =>
                a.GroupId == request.GroupId &&
                a.SantaUserId == currentUserId.ToString(),
                cancellationToken);

        // Check if assignment exists
        if (assignment == null)
        {
            return Result<GetRecipientWishlistResponse>.Failure(
                "AssignmentNotFound",
                "No assignment found for this user"
            );
        }

        // Query recipient's wishlist from GroupParticipant
        var recipientParticipant = await context.GroupParticipants
            .AsNoTracking()
            .FirstOrDefaultAsync(gp =>
                gp.GroupId == request.GroupId &&
                gp.UserId == assignment.RecipientUserId,
                cancellationToken);

        // Build response (wishlist may be null if recipient hasn't created one)
        var response = new GetRecipientWishlistResponse
        {
            GroupId = request.GroupId,
            RecipientId = assignment.RecipientUserId,
            RecipientFirstName = assignment.Recipient.FirstName,
            RecipientLastName = assignment.Recipient.LastName,
            WishlistContent = recipientParticipant?.WishlistContent,
            LastModified = recipientParticipant?.WishlistLastModified
        };

        return Result<GetRecipientWishlistResponse>.Success(response);
    }
}
