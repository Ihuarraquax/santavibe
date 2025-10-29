using SantaVibe.Api.Features.ExclusionRules.GetExclusionRules;

namespace SantaVibe.Api.Features.ExclusionRules.CreateExclusionRule;

/// <summary>
/// Response containing the created exclusion rule details and draw validation result
/// </summary>
public record CreateExclusionRuleResponse(
    Guid RuleId,
    Guid GroupId,
    UserInfoDto User1,
    UserInfoDto User2,
    DateTimeOffset CreatedAt,
    DrawValidationDto DrawValidation
);

/// <summary>
/// DTO containing draw validation result
/// Indicates whether the draw is still feasible with the new exclusion rule
/// </summary>
public record DrawValidationDto(
    bool IsValid,
    List<string> Errors
);
