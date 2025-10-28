using MediatR;
using Microsoft.EntityFrameworkCore;
using SantaVibe.Api.Common;
using SantaVibe.Api.Data;

namespace SantaVibe.Api.Features.Invitations.AcceptInvitation;

/// <summary>
/// Handles accepting an invitation and joining a Secret Santa group
/// </summary>
public class AcceptInvitationCommandHandler : IRequestHandler<AcceptInvitationCommand, Result<AcceptInvitationResponse>>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AcceptInvitationCommandHandler> _logger;

    public AcceptInvitationCommandHandler(
        ApplicationDbContext context,
        ILogger<AcceptInvitationCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<AcceptInvitationResponse>> Handle(
        AcceptInvitationCommand command,
        CancellationToken cancellationToken)
    {
        // Query group with invitation token and eager load related data
        var group = await _context.Groups
            .Include(g => g.Organizer)
            .Include(g => g.GroupParticipants)
            .FirstOrDefaultAsync(g => g.InvitationToken == command.Token, cancellationToken);

        // Validate: Token must exist
        if (group == null)
        {
            _logger.LogWarning(
                "Invalid invitation token {Token} used by user {UserId}",
                command.Token,
                command.UserId);

            return Result<AcceptInvitationResponse>.Failure(
                "InvalidInvitation",
                "This invitation link is invalid or has expired");
        }

        // Verify user exists and is active
        var userExists = await _context.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == command.UserId && !u.IsDeleted, cancellationToken);

        if (!userExists)
        {
            _logger.LogWarning(
                "User {UserId} not found or deleted when accepting invitation",
                command.UserId);

            return Result<AcceptInvitationResponse>.Failure(
                "Unauthorized",
                "User not found or account is inactive");
        }

        // Delegate business logic to the domain model (aggregate root)
        var addParticipantResult = group.AddParticipant(command.UserId, command.BudgetSuggestion);

        if (!addParticipantResult.IsSuccess)
        {
            _logger.LogWarning(
                "User {UserId} failed to join group {GroupId}: {Error}",
                command.UserId,
                group.Id,
                addParticipantResult.Error);

            return Result<AcceptInvitationResponse>.Failure(
                addParticipantResult.Error!,
                addParticipantResult.Message!);
        }

        var participant = addParticipantResult.Value!;

        // Save changes
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "User {UserId} successfully joined group {GroupId} '{GroupName}'",
            command.UserId,
            group.Id,
            group.Name);

        // Build response DTO
        var response = new AcceptInvitationResponse(
            GroupId: group.Id,
            GroupName: group.Name,
            OrganizerName: $"{group.Organizer.FirstName} {group.Organizer.LastName}",
            ParticipantCount: group.GetParticipantCount(),
            Budget: group.Budget,
            DrawCompleted: group.IsDrawCompleted(),
            JoinedAt: participant.JoinedAt
        );

        return Result<AcceptInvitationResponse>.Success(response);
    }
}
