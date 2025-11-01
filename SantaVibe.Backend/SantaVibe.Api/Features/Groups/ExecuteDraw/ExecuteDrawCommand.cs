using MediatR;
using SantaVibe.Api.Common;

namespace SantaVibe.Api.Features.Groups.ExecuteDraw;

/// <summary>
/// Command to execute Secret Santa draw for a group
/// </summary>
public sealed record ExecuteDrawCommand(
    Guid GroupId,
    string UserId,
    decimal Budget) : IRequest<Result<ExecuteDrawResponse>>, ITransactionalCommand;
