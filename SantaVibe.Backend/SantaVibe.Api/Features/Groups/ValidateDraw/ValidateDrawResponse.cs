namespace SantaVibe.Api.Features.Groups.ValidateDraw;

/// <summary>
/// Response DTO for draw validation endpoint
/// </summary>
public sealed record ValidateDrawResponse(
    Guid GroupId,
    bool IsValid,
    bool CanDraw,
    int ParticipantCount,
    int ExclusionRuleCount,
    List<string> Errors,
    List<string> Warnings);
