using MediatR;
using Microsoft.EntityFrameworkCore;
using SantaVibe.Api.Common;
using SantaVibe.Api.Data;

namespace SantaVibe.Api.Features.Assignments.GetMyAssignment;

/// <summary>
/// Handler for retrieving the authenticated user's Secret Santa assignment
/// Enforces privacy by ensuring users can only see their own assignment
/// </summary>
public sealed class GetMyAssignmentHandler(
    ApplicationDbContext context,
    ILogger<GetMyAssignmentHandler> logger)
    : IRequestHandler<GetMyAssignmentQuery, Result<GetMyAssignmentResponse>>
{
    public async Task<Result<GetMyAssignmentResponse>> Handle(
        GetMyAssignmentQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "User {UserId} retrieving assignment for group {GroupId}",
            request.UserId,
            request.GroupId);

        // Step 1: Validate group existence and draw completion
        var group = await context.Groups
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == request.GroupId, cancellationToken);

        if (group == null)
        {
            logger.LogWarning(
                "Group {GroupId} not found for user {UserId}",
                request.GroupId,
                request.UserId);

            return Result<GetMyAssignmentResponse>.Failure(
                "GroupNotFound",
                "Group does not exist");
        }

        if (group.DrawCompletedAt == null)
        {
            logger.LogWarning(
                "User {UserId} attempted to access assignment for group {GroupId} but draw not completed",
                request.UserId,
                request.GroupId);

            return Result<GetMyAssignmentResponse>.Failure(
                "DrawNotCompleted",
                "Draw has not been completed yet");
        }

        // Step 2: Verify user participation
        var isParticipant = await context.GroupParticipants
            .AsNoTracking()
            .AnyAsync(
                gp => gp.GroupId == request.GroupId && gp.UserId == request.UserId,
                cancellationToken);

        if (!isParticipant)
        {
            logger.LogWarning(
                "User {UserId} attempted to access assignment for group {GroupId} but is not a participant",
                request.UserId,
                request.GroupId);

            return Result<GetMyAssignmentResponse>.Failure(
                "NotAParticipant",
                "You are not a participant in this group");
        }

        // Step 3: Load assignment with recipient details in a single optimized query
        var assignmentData = await context.Assignments
            .AsNoTracking()
            .Where(a => a.GroupId == request.GroupId && a.SantaUserId == request.UserId)
            .Select(a => new
            {
                a.Group.Id,
                a.Group.Name,
                a.Group.Budget,
                a.Group.DrawCompletedAt,
                RecipientUserId = a.RecipientUserId,
                RecipientFirstName = a.Recipient.FirstName,
                RecipientLastName = a.Recipient.LastName,
                RecipientParticipant = context.GroupParticipants
                    .Where(gp => gp.GroupId == request.GroupId && gp.UserId == a.RecipientUserId)
                    .Select(gp => new
                    {
                        gp.WishlistContent,
                        gp.WishlistLastModified
                    })
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (assignmentData == null)
        {
            logger.LogWarning(
                "No assignment found for user {UserId} in group {GroupId}",
                request.UserId,
                request.GroupId);

            return Result<GetMyAssignmentResponse>.Failure(
                "AssignmentNotFound",
                "No assignment found for this group");
        }

        // Step 4: Map to response DTO
        var response = new GetMyAssignmentResponse(
            GroupId: assignmentData.Id,
            GroupName: assignmentData.Name,
            Budget: assignmentData.Budget!.Value, // Budget is set during draw
            DrawCompletedAt: assignmentData.DrawCompletedAt!.Value,
            Recipient: new RecipientDto(
                UserId: assignmentData.RecipientUserId,
                FirstName: assignmentData.RecipientFirstName,
                LastName: assignmentData.RecipientLastName,
                HasWishlist: !string.IsNullOrWhiteSpace(assignmentData.RecipientParticipant?.WishlistContent),
                WishlistLastModified: assignmentData.RecipientParticipant?.WishlistLastModified
            )
        );

        logger.LogInformation(
            "User {UserId} successfully retrieved assignment for group {GroupId}",
            request.UserId,
            request.GroupId);

        return Result<GetMyAssignmentResponse>.Success(response);
    }
}
