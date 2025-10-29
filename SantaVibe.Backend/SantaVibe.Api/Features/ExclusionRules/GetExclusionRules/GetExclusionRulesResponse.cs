namespace SantaVibe.Api.Features.ExclusionRules.GetExclusionRules;

/// <summary>
/// Response containing all exclusion rules for a group
/// </summary>
public record GetExclusionRulesResponse(
    Guid GroupId,
    List<ExclusionRuleDto> ExclusionRules,
    int TotalCount
);

/// <summary>
/// DTO representing an exclusion rule with user details
/// </summary>
public record ExclusionRuleDto(
    Guid RuleId,
    UserInfoDto User1,
    UserInfoDto User2,
    DateTimeOffset CreatedAt
);

/// <summary>
/// DTO containing basic user information
/// </summary>
public record UserInfoDto(
    string UserId,
    string FirstName,
    string LastName
);
