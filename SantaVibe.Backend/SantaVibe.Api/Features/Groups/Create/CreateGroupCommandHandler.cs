using MediatR;
using Microsoft.EntityFrameworkCore;
using SantaVibe.Api.Common;
using SantaVibe.Api.Data;
using SantaVibe.Api.Data.Entities;
using SantaVibe.Api.Services;

namespace SantaVibe.Api.Features.Groups.Create;

/// <summary>
/// Handles the creation of a new Secret Santa group
/// </summary>
public class CreateGroupCommandHandler : IRequestHandler<CreateGroupCommand, Result<CreateGroupResponse>>
{
    private readonly ApplicationDbContext _context;
    private readonly IUserAccessor _userAccessor;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CreateGroupCommandHandler> _logger;

    public CreateGroupCommandHandler(
        ApplicationDbContext context,
        IUserAccessor userAccessor,
        IConfiguration configuration,
        ILogger<CreateGroupCommandHandler> logger)
    {
        _context = context;
        _userAccessor = userAccessor;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Result<CreateGroupResponse>> Handle(
        CreateGroupCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _userAccessor.GetCurrentUserId();

        // Verify user exists and get user details
        var user = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId.ToString() && !u.IsDeleted)
            .Select(u => new { u.Id, u.FirstName, u.LastName })
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found or deleted when creating group", userId);
            return Result<CreateGroupResponse>.Failure(
                "Unauthorized",
                "User not found or account is inactive");
        }

        // Create the group entity
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            OrganizerUserId = user.Id,
            InvitationToken = Guid.NewGuid(),
            Budget = null,
            DrawCompletedAt = null,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = null
        };

        _context.Groups.Add(group);

        // Add organizer as the first participant
        var participant = new GroupParticipant
        {
            GroupId = group.Id,
            UserId = user.Id,
            BudgetSuggestion = null,
            WishlistContent = null,
            JoinedAt = DateTimeOffset.UtcNow
        };

        _context.GroupParticipants.Add(participant);

        // Save changes
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Group {GroupId} '{GroupName}' created by user {UserId}",
            group.Id,
            group.Name,
            userId);

        // Build invitation link using configured base URL
        var baseUrl = _configuration["App:BaseUrl"] ?? "https://santavibe.com";
        var invitationLink = $"{baseUrl}/invite/{group.InvitationToken}";

        // Map to response DTO
        var response = new CreateGroupResponse
        {
            GroupId = group.Id,
            Name = group.Name,
            OrganizerId = user.Id,
            OrganizerName = $"{user.FirstName} {user.LastName}",
            InvitationToken = group.InvitationToken,
            InvitationLink = invitationLink,
            ParticipantCount = 1,
            Budget = null,
            DrawCompleted = false,
            CreatedAt = group.CreatedAt
        };

        return Result<CreateGroupResponse>.Success(response);
    }
}
