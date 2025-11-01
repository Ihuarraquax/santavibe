using MediatR;
using Microsoft.EntityFrameworkCore;
using SantaVibe.Api.Common;
using SantaVibe.Api.Data;
using SantaVibe.Api.Services;

namespace SantaVibe.Api.Features.Groups.ValidateDraw;

/// <summary>
/// Handler for validating draw feasibility
/// </summary>
public sealed class ValidateDrawHandler(
    ApplicationDbContext context,
    IDrawAlgorithmService drawAlgorithmService,
    ILogger<ValidateDrawHandler> logger)
    : IRequestHandler<ValidateDrawQuery, Result<ValidateDrawResponse>>
{
    public async Task<Result<ValidateDrawResponse>> Handle(
        ValidateDrawQuery query,
        CancellationToken cancellationToken)
    {
        // Query group with related data
        var group = await context.Groups
            .AsNoTracking()
            .Include(g => g.GroupParticipants)
            .Include(g => g.ExclusionRules)
            .FirstOrDefaultAsync(g => g.Id == query.GroupId, cancellationToken);

        if (group == null)
        {
            logger.LogWarning("Group {GroupId} not found for draw validation", query.GroupId);
            return Result<ValidateDrawResponse>.Failure(
                "NotFound",
                "Group not found");
        }

        // Check if user is the organizer
        if (group.OrganizerUserId != query.UserId)
        {
            logger.LogWarning(
                "User {UserId} attempted to validate draw for group {GroupId} but is not the organizer",
                query.UserId,
                query.GroupId);
            return Result<ValidateDrawResponse>.Failure(
                "Forbidden",
                "Only the group organizer can validate the draw");
        }

        var errors = new List<string>();
        var warnings = new List<string>();
        var participantCount = group.GroupParticipants.Count;
        var exclusionRuleCount = group.ExclusionRules.Count;

        // Check if draw already completed
        if (group.IsDrawCompleted())
        {
            warnings.Add("Draw has already been completed for this group");
        }

        // Validate minimum participants
        if (participantCount < 3)
        {
            errors.Add("Minimum 3 participants required for draw");
        }

        // Get participant IDs and exclusion pairs
        var participantIds = group.GroupParticipants
            .Select(gp => gp.UserId)
            .ToList();

        var exclusionPairs = group.ExclusionRules
            .Select(er => (er.UserId1, er.UserId2))
            .ToList();

        // Validate draw feasibility using algorithm service
        bool isValid = true;
        if (participantCount >= 3)
        {
            var validationResult = drawAlgorithmService.ValidateDrawFeasibility(
                participantIds,
                exclusionPairs);

            if (!validationResult.IsValid)
            {
                isValid = false;
                errors.AddRange(validationResult.Errors);
            }
        }
        else
        {
            isValid = false;
        }

        // Determine if draw can be executed
        bool canDraw = isValid && !group.IsDrawCompleted();

        logger.LogInformation(
            "Draw validation for group {GroupId}: IsValid={IsValid}, CanDraw={CanDraw}, " +
            "ParticipantCount={ParticipantCount}, ExclusionRuleCount={ExclusionRuleCount}",
            query.GroupId,
            isValid,
            canDraw,
            participantCount,
            exclusionRuleCount);

        var response = new ValidateDrawResponse(
            GroupId: query.GroupId,
            IsValid: isValid,
            CanDraw: canDraw,
            ParticipantCount: participantCount,
            ExclusionRuleCount: exclusionRuleCount,
            Errors: errors,
            Warnings: warnings);

        return Result<ValidateDrawResponse>.Success(response);
    }
}
