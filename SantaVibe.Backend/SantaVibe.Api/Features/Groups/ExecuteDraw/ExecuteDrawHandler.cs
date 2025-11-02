using MediatR;
using SantaVibe.Api.Common;

namespace SantaVibe.Api.Features.Groups.ExecuteDraw;

/// <summary>
/// Handler for executing Secret Santa draw
/// Orchestrates the draw process by delegating to domain services and the Group aggregate
/// </summary>
public sealed class ExecuteDrawHandler : IRequestHandler<ExecuteDrawCommand, Result<ExecuteDrawResponse>>
{
    public Task<Result<ExecuteDrawResponse>> Handle(ExecuteDrawCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
