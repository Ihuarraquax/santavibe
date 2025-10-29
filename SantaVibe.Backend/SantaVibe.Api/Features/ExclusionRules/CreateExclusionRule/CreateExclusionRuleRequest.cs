namespace SantaVibe.Api.Features.ExclusionRules.CreateExclusionRule;

/// <summary>
/// Request to create a new exclusion rule
/// </summary>
public record CreateExclusionRuleRequest(
    string UserId1,
    string UserId2
);
