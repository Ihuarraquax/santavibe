using MediatR;
using Microsoft.EntityFrameworkCore;
using SantaVibe.Api.Common;
using SantaVibe.Api.Data;
using SantaVibe.Api.Services;

namespace SantaVibe.Api.Features.Groups.UpdateBudgetSuggestion;

public class UpdateBudgetSuggestionCommandHandler(
    ApplicationDbContext context,
    IUserAccessor userAccessor,
    ILogger<UpdateBudgetSuggestionCommandHandler> logger)
    : IRequestHandler<UpdateBudgetSuggestionCommand, Result<UpdateBudgetSuggestionResponse>>
{
    public async Task<Result<UpdateBudgetSuggestionResponse>> Handle(
        UpdateBudgetSuggestionCommand command,
        CancellationToken cancellationToken)
    {
        // Get current user ID from JWT
        var userId = userAccessor.GetCurrentUserId().ToString();

        // Validate budget suggestion range
        if (command.BudgetSuggestion.HasValue)
        {
            const decimal minBudget = 0.01m;
            const decimal maxBudget = 99999999.99m;

            if (command.BudgetSuggestion.Value < minBudget ||
                command.BudgetSuggestion.Value > maxBudget)
            {
                var errors = new Dictionary<string, string[]>
                {
                    ["budgetSuggestion"] = new[]
                    {
                        $"Budget suggestion must be between {minBudget} and {maxBudget}"
                    }
                };

                logger.LogWarning(
                    "Invalid budget suggestion {BudgetSuggestion} for user {UserId} in group {GroupId}",
                    command.BudgetSuggestion, userId, command.GroupId);

                return Result<UpdateBudgetSuggestionResponse>.ValidationFailure(
                    "Budget suggestion validation failed", errors);
            }
        }

        // Query for participant record directly (most efficient)
        var participant = await context.GroupParticipants
            .Include(gp => gp.Group)
            .FirstOrDefaultAsync(gp =>
                gp.GroupId == command.GroupId &&
                gp.UserId == userId,
                cancellationToken);

        // Check if participant exists
        if (participant == null)
        {
            // Distinguish between group not found and not a participant
            var groupExists = await context.Groups
                .AnyAsync(g => g.Id == command.GroupId, cancellationToken);

            if (!groupExists)
            {
                logger.LogWarning(
                    "Group {GroupId} not found for budget suggestion update by user {UserId}",
                    command.GroupId, userId);

                return Result<UpdateBudgetSuggestionResponse>.Failure(
                    "GroupNotFound",
                    "Group not found");
            }

            logger.LogWarning(
                "User {UserId} is not a participant in group {GroupId}",
                userId, command.GroupId);

            return Result<UpdateBudgetSuggestionResponse>.Failure(
                "NotParticipant",
                "You are not a participant in this group");
        }

        // Check if draw has been completed
        if (participant.Group.IsDrawCompleted())
        {
            logger.LogWarning(
                "User {UserId} attempted to update budget suggestion for group {GroupId} after draw completion",
                userId, command.GroupId);

            return Result<UpdateBudgetSuggestionResponse>.Failure(
                "DrawAlreadyCompleted",
                "Cannot modify budget suggestion after draw has been completed");
        }

        // Update budget suggestion
        participant.BudgetSuggestion = command.BudgetSuggestion;

        // Save changes
        try
        {
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "User {UserId} updated budget suggestion to {BudgetSuggestion} for group {GroupId}",
                userId, command.BudgetSuggestion, command.GroupId);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex,
                "Database error updating budget suggestion for user {UserId} in group {GroupId}",
                userId, command.GroupId);

            return Result<UpdateBudgetSuggestionResponse>.Failure(
                "InternalServerError",
                "An unexpected error occurred while updating budget suggestion");
        }

        // Create response
        var response = new UpdateBudgetSuggestionResponse
        {
            GroupId = command.GroupId,
            BudgetSuggestion = command.BudgetSuggestion,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        return Result<UpdateBudgetSuggestionResponse>.Success(response);
    }
}
