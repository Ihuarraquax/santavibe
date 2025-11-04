using MediatR;
using Microsoft.EntityFrameworkCore;
using SantaVibe.Api.Common;
using SantaVibe.Api.Data;
using SantaVibe.Api.Services;

namespace SantaVibe.Api.Features.Groups.GetMyBudgetSuggestion;

public class GetMyBudgetSuggestionQueryHandler(
    ApplicationDbContext context,
    IUserAccessor userAccessor,
    ILogger<GetMyBudgetSuggestionQueryHandler> logger)
    : IRequestHandler<GetMyBudgetSuggestionQuery, Result<GetMyBudgetSuggestionResponse>>
{
    public async Task<Result<GetMyBudgetSuggestionResponse>> Handle(
        GetMyBudgetSuggestionQuery query,
        CancellationToken cancellationToken)
    {
        // Get current user ID from JWT
        var userId = userAccessor.GetCurrentUserId().ToString();

        // Query for participant record
        var participant = await context.GroupParticipants
            .AsNoTracking() // Read-only query optimization
            .FirstOrDefaultAsync(gp =>
                gp.GroupId == query.GroupId &&
                gp.UserId == userId,
                cancellationToken);

        // Check if participant exists
        if (participant == null)
        {
            // Distinguish between group not found and not a participant
            var groupExists = await context.Groups
                .AsNoTracking()
                .AnyAsync(g => g.Id == query.GroupId, cancellationToken);

            if (!groupExists)
            {
                logger.LogWarning(
                    "Group {GroupId} not found for budget suggestion retrieval by user {UserId}",
                    query.GroupId, userId);

                return Result<GetMyBudgetSuggestionResponse>.Failure(
                    "GroupNotFound",
                    "Group not found");
            }

            logger.LogWarning(
                "User {UserId} is not a participant in group {GroupId}",
                userId, query.GroupId);

            return Result<GetMyBudgetSuggestionResponse>.Failure(
                "NotParticipant",
                "You are not a participant in this group");
        }

        // Map participant data to response
        var response = new GetMyBudgetSuggestionResponse
        {
            GroupId = query.GroupId,
            BudgetSuggestion = participant.BudgetSuggestion,
            // Return JoinedAt as SubmittedAt when budget suggestion exists, null otherwise
            SubmittedAt = participant.BudgetSuggestion.HasValue ? participant.JoinedAt : null
        };

        return Result<GetMyBudgetSuggestionResponse>.Success(response);
    }
}
