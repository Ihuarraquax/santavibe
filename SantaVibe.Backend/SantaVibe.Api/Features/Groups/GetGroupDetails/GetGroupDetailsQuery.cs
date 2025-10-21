using MediatR;
using SantaVibe.Api.Common;

namespace SantaVibe.Api.Features.Groups.GetGroupDetails;

/// <summary>
/// Query to retrieve detailed information about a specific Secret Santa group
/// </summary>
/// <param name="GroupId">The unique identifier of the group</param>
public record GetGroupDetailsQuery(Guid GroupId)
    : IRequest<Result<GetGroupDetailsResponse>>;
