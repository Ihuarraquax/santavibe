using MediatR;
using Microsoft.EntityFrameworkCore;
using SantaVibe.Api.Common;
using SantaVibe.Api.Data;
using SantaVibe.Api.Data.Entities;
using SantaVibe.Api.Features.ExclusionRules.GetExclusionRules;
using SantaVibe.Api.Services;
using SantaVibe.Api.Services.DrawValidation;

namespace SantaVibe.Api.Features.ExclusionRules.CreateExclusionRule;

/// <summary>
/// Handler for creating a new exclusion rule
/// Validates business rules and ensures draw feasibility
/// </summary>
public class CreateExclusionRuleCommandHandler
    : IRequestHandler<CreateExclusionRuleCommand, Result<CreateExclusionRuleResponse>>
{
    private readonly ApplicationDbContext _context;
    private readonly IUserAccessor _userAccessor;
    private readonly IDrawValidationService _drawValidationService;
    private readonly ILogger<CreateExclusionRuleCommandHandler> _logger;

    public CreateExclusionRuleCommandHandler(
        ApplicationDbContext context,
        IUserAccessor userAccessor,
        IDrawValidationService drawValidationService,
        ILogger<CreateExclusionRuleCommandHandler> logger)
    {
        _context = context;
        _userAccessor = userAccessor;
        _drawValidationService = drawValidationService;
        _logger = logger;
    }

    public async Task<Result<CreateExclusionRuleResponse>> Handle(
        CreateExclusionRuleCommand request,
        CancellationToken cancellationToken)
    {
        var currentUserId = _userAccessor.GetCurrentUserId().ToString();

        // Validate userId1 and userId2 are different
        if (request.UserId1 == request.UserId2)
        {
            _logger.LogWarning(
                "Attempted to create exclusion rule with same user for group {GroupId}",
                request.GroupId);
            return Result<CreateExclusionRuleResponse>.Failure(
                "SameUser",
                "Cannot create exclusion rule for the same user");
        }

        // Check if group exists and load organizer info
        var group = await _context.Groups
            .FirstOrDefaultAsync(g => g.Id == request.GroupId, cancellationToken);

        if (group == null)
        {
            _logger.LogInformation("Group {GroupId} not found", request.GroupId);
            return Result<CreateExclusionRuleResponse>.Failure(
                "GroupNotFound",
                "Group not found");
        }

        // Check if user is organizer
        if (group.OrganizerUserId != currentUserId)
        {
            _logger.LogWarning(
                "User {UserId} attempted to create exclusion rule for group {GroupId} without authorization",
                currentUserId, request.GroupId);
            return Result<CreateExclusionRuleResponse>.Failure(
                "Forbidden",
                "You are not authorized to create exclusion rules for this group");
        }

        // Check if draw is already completed
        if (group.DrawCompletedAt.HasValue)
        {
            _logger.LogWarning(
                "Attempted to create exclusion rule for group {GroupId} after draw completion",
                request.GroupId);
            return Result<CreateExclusionRuleResponse>.Failure(
                "DrawAlreadyCompleted",
                "Cannot add exclusion rules after draw has been completed");
        }

        // Check if both users are participants
        var participants = await _context.GroupParticipants
            .Where(gp => gp.GroupId == request.GroupId &&
                        (gp.UserId == request.UserId1 || gp.UserId == request.UserId2))
            .Select(gp => gp.UserId)
            .ToListAsync(cancellationToken);

        if (participants.Count != 2)
        {
            _logger.LogWarning(
                "One or both users not found as participants in group {GroupId}",
                request.GroupId);
            return Result<CreateExclusionRuleResponse>.Failure(
                "GroupNotFound",
                "One or both users not found in this group");
        }

        // Check for duplicate exclusion rule (bidirectional)
        var duplicateExists = await _context.ExclusionRules
            .AnyAsync(er => er.GroupId == request.GroupId &&
                           ((er.UserId1 == request.UserId1 && er.UserId2 == request.UserId2) ||
                            (er.UserId1 == request.UserId2 && er.UserId2 == request.UserId1)),
                     cancellationToken);

        if (duplicateExists)
        {
            _logger.LogWarning(
                "Duplicate exclusion rule for group {GroupId} between users {UserId1} and {UserId2}",
                request.GroupId, request.UserId1, request.UserId2);
            return Result<CreateExclusionRuleResponse>.Failure(
                "DuplicateExclusionRule",
                "This exclusion rule already exists");
        }

        // Create new exclusion rule
        var exclusionRule = new ExclusionRule
        {
            GroupId = request.GroupId,
            UserId1 = request.UserId1,
            UserId2 = request.UserId2,
            CreatedByUserId = currentUserId,
            CreatedAt = DateTimeOffset.UtcNow,
            Id = Guid.NewGuid()
        };

        _context.ExclusionRules.Add(exclusionRule);
        await _context.SaveChangesAsync(cancellationToken);

        // Validate draw feasibility
        var drawValidation = await _drawValidationService
            .ValidateDrawFeasibilityAsync(request.GroupId, cancellationToken);

        if (!drawValidation.IsValid)
        {
            // Rollback: remove the rule if it makes draw impossible
            _context.ExclusionRules.Remove(exclusionRule);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogWarning(
                "Exclusion rule for group {GroupId} would make draw impossible",
                request.GroupId);

            return Result<CreateExclusionRuleResponse>.Failure(
                "InvalidExclusionRule",
                "This exclusion rule would make a valid draw impossible");
        }

        // Fetch user details
        var users = await _context.Users
            .Where(u => u.Id == request.UserId1 || u.Id == request.UserId2)
            .Select(u => new { u.Id, u.FirstName, u.LastName })
            .ToListAsync(cancellationToken);

        var user1Info = users.First(u => u.Id == request.UserId1);
        var user2Info = users.First(u => u.Id == request.UserId2);

        _logger.LogInformation(
            "Exclusion rule {RuleId} created for group {GroupId}",
            exclusionRule.Id, request.GroupId);

        var response = new CreateExclusionRuleResponse(
            exclusionRule.Id,
            exclusionRule.GroupId,
            new UserInfoDto(user1Info.Id, user1Info.FirstName, user1Info.LastName),
            new UserInfoDto(user2Info.Id, user2Info.FirstName, user2Info.LastName),
            exclusionRule.CreatedAt,
            new DrawValidationDto(drawValidation.IsValid, drawValidation.Errors)
        );

        return Result<CreateExclusionRuleResponse>.Success(response);
    }
}
