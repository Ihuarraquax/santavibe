using MediatR;
using SantaVibe.Api.Common;

namespace SantaVibe.Api.Features.ExclusionRules.CreateExclusionRule;

/// <summary>
/// Command to create a new exclusion rule
/// Only the group organizer can create exclusion rules before draw completion
/// </summary>
public record CreateExclusionRuleCommand(
    Guid GroupId,
    string UserId1,
    string UserId2
) : IRequest<Result<CreateExclusionRuleResponse>>;
