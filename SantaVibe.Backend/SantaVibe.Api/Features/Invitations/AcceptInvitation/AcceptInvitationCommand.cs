using MediatR;
using SantaVibe.Api.Common;

namespace SantaVibe.Api.Features.Invitations.AcceptInvitation;

/// <summary>
/// Command to accept an invitation and join a group
/// </summary>
/// <param name="Token">Invitation token (UUID)</param>
/// <param name="UserId">Current authenticated user ID</param>
/// <param name="BudgetSuggestion">Optional budget suggestion in PLN</param>
public record AcceptInvitationCommand(
    Guid Token,
    string UserId,
    decimal? BudgetSuggestion
) : IRequest<Result<AcceptInvitationResponse>>, ITransactionalCommand;
