using MediatR;
using SantaVibe.Api.Common;

namespace SantaVibe.Api.Features.ExclusionRules.DeleteExclusionRule;

/// <summary>
/// Command to delete an exclusion rule
/// Only the group organizer can delete exclusion rules before draw completion
/// </summary>
public record DeleteExclusionRuleCommand(
    Guid GroupId,
    Guid RuleId
) : IRequest<Result<Unit>>;
