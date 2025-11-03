using MediatR;
using SantaVibe.Api.Common;

namespace SantaVibe.Api.Features.Groups.RemoveParticipant;

/// <summary>
/// Command to remove a participant from a group
/// </summary>
public sealed record RemoveParticipantCommand(
    Guid GroupId,
    string UserIdToRemove,
    string RequestingUserId) : IRequest<Result<Unit>>, ITransactionalCommand;
