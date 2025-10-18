using MediatR;
using SantaVibe.Api.Common;

namespace SantaVibe.Api.Features.Groups.Create;

/// <summary>
/// Command to create a new Secret Santa group with the current user as organizer
/// </summary>
public record CreateGroupCommand(string Name) : IRequest<Result<CreateGroupResponse>>;
