using MediatR;
using Microsoft.EntityFrameworkCore;
using SantaVibe.Api.Common;
using SantaVibe.Api.Data;
using SantaVibe.Api.Services;

namespace SantaVibe.Api.Features.Groups.ExecuteDraw;

/// <summary>
/// Handler for executing Secret Santa draw
/// Orchestrates the draw process by delegating to domain services and the Group aggregate
/// </summary>
public sealed class ExecuteDrawHandler(
    ApplicationDbContext context,
    IDrawAlgorithmService drawAlgorithmService,
    ILogger<ExecuteDrawHandler> logger)
    : IRequestHandler<ExecuteDrawCommand, Result<ExecuteDrawResponse>>
{
    public async Task<Result<ExecuteDrawResponse>> Handle(
        ExecuteDrawCommand command,
        CancellationToken cancellationToken)
    {
        // Validate budget format (manual validation as per project guidelines)
        const decimal minBudget = 0.01m;
        const decimal maxBudget = 99999999.99m;

        if (command.Budget < minBudget || command.Budget > maxBudget)
        {
            var errors = new Dictionary<string, string[]>
            {
                ["budget"] = new[] { $"Budget must be between {minBudget} and {maxBudget}" }
            };

            logger.LogWarning(
                "Invalid budget {Budget} for draw execution in group {GroupId}",
                command.Budget,
                command.GroupId);

            return Result<ExecuteDrawResponse>.ValidationFailure(
                "Budget validation failed",
                errors);
        }

        // Check for decimal precision (max 2 decimal places)
        var decimalPlaces = BitConverter.GetBytes(decimal.GetBits(command.Budget)[3])[2];
        if (decimalPlaces > 2)
        {
            var errors = new Dictionary<string, string[]>
            {
                ["budget"] = new[] { "Budget must have at most 2 decimal places" }
            };

            return Result<ExecuteDrawResponse>.ValidationFailure(
                "Budget validation failed",
                errors);
        }

        // Query group with all necessary related data for read operations
        var group = await context.Groups
            .Include(g => g.GroupParticipants)
                .ThenInclude(gp => gp.User)
            .Include(g => g.ExclusionRules)
            .FirstOrDefaultAsync(g => g.Id == command.GroupId, cancellationToken);

        if (group == null)
        {
            logger.LogWarning("Group {GroupId} not found for draw execution", command.GroupId);
            return Result<ExecuteDrawResponse>.Failure(
                "NotFound",
                "Group not found");
        }

        // Check if user is the organizer
        if (group.OrganizerUserId != command.UserId)
        {
            logger.LogWarning(
                "User {UserId} attempted to execute draw for group {GroupId} but is not the organizer",
                command.UserId,
                command.GroupId);
            return Result<ExecuteDrawResponse>.Failure(
                "Forbidden",
                "Only the group organizer can execute the draw");
        }

        // Check if draw already completed (redundant with domain logic, but provides better error message)
        if (group.IsDrawCompleted())
        {
            logger.LogWarning(
                "Draw already completed for group {GroupId} at {DrawCompletedAt}",
                command.GroupId,
                group.DrawCompletedAt);
            return Result<ExecuteDrawResponse>.Failure(
                "DrawAlreadyCompleted",
                "Draw has already been completed for this group");
        }

        var participantCount = group.GroupParticipants.Count;

        // Validate minimum participants (redundant with domain logic, but provides better logging)
        if (participantCount < 3)
        {
            logger.LogWarning(
                "Cannot execute draw for group {GroupId}: only {ParticipantCount} participants",
                command.GroupId,
                participantCount);
            return Result<ExecuteDrawResponse>.Failure(
                "DrawValidationFailed",
                "Cannot execute draw: Minimum 3 participants required");
        }

        // Get participant IDs and exclusion pairs for algorithm
        var participantIds = group.GroupParticipants
            .Select(gp => gp.UserId)
            .ToList();

        var exclusionPairs = group.ExclusionRules
            .Select(er => (er.UserId1, er.UserId2))
            .ToList();

        // Validate draw feasibility using domain service
        var validationResult = drawAlgorithmService.ValidateDrawFeasibility(
            participantIds,
            exclusionPairs);

        if (!validationResult.IsValid)
        {
            logger.LogWarning(
                "Draw validation failed for group {GroupId}: {Errors}",
                command.GroupId,
                string.Join(", ", validationResult.Errors));
            return Result<ExecuteDrawResponse>.Failure(
                "DrawValidationFailed",
                $"Cannot execute draw: {string.Join(", ", validationResult.Errors)}");
        }

        logger.LogInformation(
            "Executing draw for group {GroupId} with {ParticipantCount} participants and budget {Budget}",
            command.GroupId,
            participantCount,
            command.Budget);

        // Execute draw algorithm using domain service
        Dictionary<string, string> santaToRecipientMap;
        try
        {
            santaToRecipientMap = drawAlgorithmService.ExecuteDrawAlgorithm(
                participantIds,
                exclusionPairs);
        }
        catch (DrawAlgorithmException ex)
        {
            logger.LogError(ex, "Draw algorithm failed for group {GroupId}", command.GroupId);
            return Result<ExecuteDrawResponse>.Failure(
                "DrawExecutionFailed",
                "An unexpected error occurred during draw execution. Please contact support.");
        }

        // Execute draw on the aggregate root (Group)
        // This will update the group state and raise domain events
        var drawResult = group.ExecuteDraw(command.Budget, santaToRecipientMap);

        if (!drawResult.IsSuccess)
        {
            logger.LogError(
                "Group.ExecuteDraw failed for group {GroupId}: {Error} - {Message}",
                command.GroupId,
                drawResult.Error,
                drawResult.Message);
            return Result<ExecuteDrawResponse>.Failure(
                drawResult.Error!,
                drawResult.Message!);
        }

        // Save changes (domain events will be dispatched by DomainEventDispatcherBehavior)
        try
        {
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Draw completed successfully for group {GroupId}",
                command.GroupId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save draw execution for group {GroupId}", command.GroupId);
            return Result<ExecuteDrawResponse>.Failure(
                "InternalServerError",
                "An unexpected error occurred while saving. Please try again later.");
        }

        // Get organizer's assignment to return in response
        var organizerAssignment = santaToRecipientMap[command.UserId];
        var recipient = group.GroupParticipants
            .First(gp => gp.UserId == organizerAssignment)
            .User;

        var response = new ExecuteDrawResponse(
            GroupId: command.GroupId,
            Budget: command.Budget,
            DrawCompleted: true,
            DrawCompletedAt: group.DrawCompletedAt!.Value,
            ParticipantCount: participantCount,
            AssignmentsCreated: santaToRecipientMap.Count,
            EmailNotificationsScheduled: participantIds.Count,
            MyAssignment: new AssignmentDto(
                RecipientId: recipient.Id,
                RecipientFirstName: recipient.FirstName,
                RecipientLastName: recipient.LastName));

        return Result<ExecuteDrawResponse>.Success(response);
    }
}
