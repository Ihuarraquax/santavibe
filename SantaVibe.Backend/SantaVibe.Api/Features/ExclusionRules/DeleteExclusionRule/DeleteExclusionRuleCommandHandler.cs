using MediatR;
using Microsoft.EntityFrameworkCore;
using SantaVibe.Api.Common;
using SantaVibe.Api.Data;
using SantaVibe.Api.Services;

namespace SantaVibe.Api.Features.ExclusionRules.DeleteExclusionRule;

/// <summary>
/// Handler for deleting an exclusion rule
/// Validates business rules and authorization
/// </summary>
public class DeleteExclusionRuleCommandHandler
    : IRequestHandler<DeleteExclusionRuleCommand, Result<Unit>>
{
    private readonly ApplicationDbContext _context;
    private readonly IUserAccessor _userAccessor;
    private readonly ILogger<DeleteExclusionRuleCommandHandler> _logger;

    public DeleteExclusionRuleCommandHandler(
        ApplicationDbContext context,
        IUserAccessor userAccessor,
        ILogger<DeleteExclusionRuleCommandHandler> logger)
    {
        _context = context;
        _userAccessor = userAccessor;
        _logger = logger;
    }

    public async Task<Result<Unit>> Handle(
        DeleteExclusionRuleCommand request,
        CancellationToken cancellationToken)
    {
        var currentUserId = _userAccessor.GetCurrentUserId().ToString();

        // Check if group exists
        var group = await _context.Groups
            .FirstOrDefaultAsync(g => g.Id == request.GroupId, cancellationToken);

        if (group == null)
        {
            _logger.LogInformation("Group {GroupId} not found", request.GroupId);
            return Result<Unit>.Failure(
                "GroupNotFound",
                "Group not found");
        }

        // Check if user is organizer
        if (group.OrganizerUserId != currentUserId)
        {
            _logger.LogWarning(
                "User {UserId} attempted to delete exclusion rule for group {GroupId} without authorization",
                currentUserId, request.GroupId);
            return Result<Unit>.Failure(
                "Forbidden",
                "You are not authorized to delete exclusion rules for this group");
        }

        // Check if draw is already completed
        if (group.DrawCompletedAt.HasValue)
        {
            _logger.LogWarning(
                "Attempted to delete exclusion rule for group {GroupId} after draw completion",
                request.GroupId);
            return Result<Unit>.Failure(
                "DrawAlreadyCompleted",
                "Cannot remove exclusion rules after draw has been completed");
        }

        // Find the exclusion rule
        var exclusionRule = await _context.ExclusionRules
            .FirstOrDefaultAsync(
                er => er.Id == request.RuleId && er.GroupId == request.GroupId,
                cancellationToken);

        if (exclusionRule == null)
        {
            _logger.LogInformation(
                "Exclusion rule {RuleId} not found in group {GroupId}",
                request.RuleId, request.GroupId);
            return Result<Unit>.Failure(
                "GroupNotFound",
                "Exclusion rule not found");
        }

        // Delete the exclusion rule
        _context.ExclusionRules.Remove(exclusionRule);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Exclusion rule {RuleId} deleted from group {GroupId}",
            request.RuleId, request.GroupId);

        return Result<Unit>.Success(Unit.Value);
    }
}
