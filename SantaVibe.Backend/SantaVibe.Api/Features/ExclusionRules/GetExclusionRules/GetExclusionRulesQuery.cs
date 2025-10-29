using MediatR;
using SantaVibe.Api.Common;

namespace SantaVibe.Api.Features.ExclusionRules.GetExclusionRules;

/// <summary>
/// Query to retrieve all exclusion rules for a specific group
/// Only the group organizer can view exclusion rules
/// </summary>
public record GetExclusionRulesQuery(
    Guid GroupId
) : IRequest<Result<GetExclusionRulesResponse>>;
