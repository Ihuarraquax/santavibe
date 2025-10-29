using MediatR;
using Microsoft.EntityFrameworkCore;
using SantaVibe.Api.Common;
using SantaVibe.Api.Data;
using SantaVibe.Api.Services;

namespace SantaVibe.Api.Features.ExclusionRules.GetExclusionRules;

/// <summary>
/// Handler for retrieving exclusion rules for a group
/// Validates that the requesting user is the group organizer
/// </summary>
public class GetExclusionRulesQueryHandler
    : IRequestHandler<GetExclusionRulesQuery, Result<GetExclusionRulesResponse>>
{
    private readonly ApplicationDbContext _context;
    private readonly IUserAccessor _userAccessor;
    private readonly ILogger<GetExclusionRulesQueryHandler> _logger;

    public GetExclusionRulesQueryHandler(
        ApplicationDbContext context,
        IUserAccessor userAccessor,
        ILogger<GetExclusionRulesQueryHandler> logger)
    {
        _context = context;
        _userAccessor = userAccessor;
        _logger = logger;
    }

    public async Task<Result<GetExclusionRulesResponse>> Handle(
        GetExclusionRulesQuery request,
        CancellationToken cancellationToken)
    {
        var currentUserId = _userAccessor.GetCurrentUserId().ToString();

        // Check if group exists
        var group = await _context.Groups
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == request.GroupId, cancellationToken);

        if (group == null)
        {
            _logger.LogInformation(
                "Group {GroupId} not found when attempting to retrieve exclusion rules",
                request.GroupId);
            return Result<GetExclusionRulesResponse>.Failure(
                "GroupNotFound",
                "Group not found");
        }

        // Check if user is organizer
        if (group.OrganizerUserId != currentUserId)
        {
            _logger.LogWarning(
                "User {UserId} attempted to access exclusion rules for group {GroupId} without authorization",
                currentUserId, request.GroupId);
            return Result<GetExclusionRulesResponse>.Failure(
                "Forbidden",
                "You are not authorized to view exclusion rules for this group");
        }

        // Fetch exclusion rules with user details
        var exclusionRules = await _context.ExclusionRules
            .AsNoTracking()
            .Include(er => er.User1)
            .Include(er => er.User2)
            .Where(er => er.GroupId == request.GroupId)
            .OrderBy(er => er.CreatedAt)
            .Select(er => new ExclusionRuleDto(
                er.Id,
                new UserInfoDto(er.UserId1, er.User1.FirstName, er.User1.LastName),
                new UserInfoDto(er.UserId2, er.User2.FirstName, er.User2.LastName),
                er.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Retrieved {Count} exclusion rules for group {GroupId}",
            exclusionRules.Count, request.GroupId);

        var response = new GetExclusionRulesResponse(
            request.GroupId,
            exclusionRules,
            exclusionRules.Count
        );

        return Result<GetExclusionRulesResponse>.Success(response);
    }
}
