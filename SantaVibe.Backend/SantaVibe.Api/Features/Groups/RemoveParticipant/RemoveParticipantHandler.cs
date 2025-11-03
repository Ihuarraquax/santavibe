using MediatR;
using Microsoft.EntityFrameworkCore;
using SantaVibe.Api.Common;
using SantaVibe.Api.Data;

namespace SantaVibe.Api.Features.Groups.RemoveParticipant;

/// <summary>
/// Handler for removing a participant from a group
/// Orchestrates participant removal by delegating business logic to the Group aggregate
/// and handling exclusion rule cleanup
/// </summary>
public sealed class RemoveParticipantHandler(
    ApplicationDbContext context,
    ILogger<RemoveParticipantHandler> logger)
    : IRequestHandler<RemoveParticipantCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(RemoveParticipantCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Removing participant {UserIdToRemove} from group {GroupId} by user {RequestingUserId}",
            request.UserIdToRemove,
            request.GroupId,
            request.RequestingUserId);

        // Load group with participants
        var group = await context.Groups
            .Include(g => g.GroupParticipants)
            .FirstOrDefaultAsync(g => g.Id == request.GroupId, cancellationToken);

        if (group == null)
        {
            logger.LogInformation(
                "Group {GroupId} not found",
                request.GroupId);

            return Result<Unit>.Failure(
                "GroupNotFound",
                "Group not found");
        }

        // Business rule: Only organizer can remove participants
        if (group.OrganizerUserId != request.RequestingUserId)
        {
            logger.LogWarning(
                "User {RequestingUserId} attempted to remove participant from group {GroupId} but is not the organizer",
                request.RequestingUserId,
                request.GroupId);

            return Result<Unit>.Failure(
                "Forbidden",
                "Only the group organizer can remove participants");
        }

        // Delegate to domain model for business logic
        var result = group.RemoveParticipant(request.UserIdToRemove);

        if (!result.IsSuccess)
        {
            logger.LogInformation(
                "Failed to remove participant {UserIdToRemove} from group {GroupId}: {Error} - {Message}",
                request.UserIdToRemove,
                request.GroupId,
                result.Error,
                result.Message);

            return result;
        }

        // Clean up related exclusion rules
        var exclusionRulesToRemove = await context.ExclusionRules
            .Where(er => er.GroupId == request.GroupId &&
                        (er.UserId1 == request.UserIdToRemove || er.UserId2 == request.UserIdToRemove))
            .ToListAsync(cancellationToken);

        if (exclusionRulesToRemove.Any())
        {
            logger.LogInformation(
                "Removing {Count} exclusion rules for participant {UserIdToRemove} in group {GroupId}",
                exclusionRulesToRemove.Count,
                request.UserIdToRemove,
                request.GroupId);

            context.ExclusionRules.RemoveRange(exclusionRulesToRemove);
        }

        await context.SaveEntitiesAsync(cancellationToken);

        logger.LogInformation(
            "Successfully removed participant {UserIdToRemove} from group {GroupId}",
            request.UserIdToRemove,
            request.GroupId);

        return Result<Unit>.Success(Unit.Value);
    }
}
