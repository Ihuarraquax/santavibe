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
    public async Task<Result<ExecuteDrawResponse>> Handle(ExecuteDrawCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Executing draw for group {GroupId} by user {UserId}",
            request.GroupId,
            request.UserId);

        // Load group with all related data in a single query
        var group = await context.Groups
            .Include(g => g.GroupParticipants)
                .ThenInclude(gp => gp.User)
            .Include(g => g.ExclusionRules)
            .Include(g => g.Assignments)
            .FirstOrDefaultAsync(g => g.Id == request.GroupId, cancellationToken);

        if (group == null)
        {
            return Result<ExecuteDrawResponse>.Failure(
                "GroupNotFound",
                "Group not found");
        }

        // Business rule: Only organizer can execute draw
        if (group.OrganizerUserId != request.UserId)
        {
            return Result<ExecuteDrawResponse>.Failure(
                "Forbidden",
                "Only the group organizer can execute the draw");
        }

        // Execute draw on the domain model
        var drawResult = group.ExecuteDraw(request.Budget, drawAlgorithmService);

        if (!drawResult.IsSuccess)
        {
            return Result<ExecuteDrawResponse>.Failure(
                drawResult.Error!,
                drawResult.Message!);
        }

        logger.LogInformation(
            "Draw completed for group {GroupId}. Created {AssignmentCount} assignments",
            request.GroupId,
            drawResult.Value!.Assignments.Count);
        
        await context.SaveEntitiesAsync(cancellationToken);

        var organizerAssignment = drawResult.Value.Assignments
            .First(a => a.SantaUserId == request.UserId);

        var recipient = group.GroupParticipants
            .First(gp => gp.UserId == organizerAssignment.RecipientUserId)
            .User;

        var myAssignmentDto = new AssignmentDto(
            RecipientId: recipient.Id,
            RecipientFirstName: recipient.FirstName,
            RecipientLastName: recipient.LastName,
            HasWishlist: false);

        var response = new ExecuteDrawResponse(
            GroupId: group.Id,
            Budget: group.Budget!.Value,
            DrawCompleted: true,
            DrawCompletedAt: drawResult.Value.DrawCompletedAt,
            ParticipantCount: group.GetParticipantCount(),
            AssignmentsCreated: drawResult.Value.Assignments.Count,
            MyAssignment: myAssignmentDto);

        return Result<ExecuteDrawResponse>.Success(response);
    }
}
