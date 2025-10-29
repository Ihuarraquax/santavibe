namespace SantaVibe.Api.Services.DrawValidation;

/// <summary>
/// Service for validating Secret Santa draw feasibility with exclusion rules
/// </summary>
public interface IDrawValidationService
{
    /// <summary>
    /// Validates whether a valid Secret Santa draw is possible given current exclusion rules
    /// Uses graph theory to ensure each participant can be assigned a valid recipient
    /// </summary>
    /// <param name="groupId">The group to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result indicating if draw is possible and any errors</returns>
    Task<DrawValidationResult> ValidateDrawFeasibilityAsync(
        Guid groupId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of draw feasibility validation
/// </summary>
public record DrawValidationResult(
    bool IsValid,
    List<string> Errors
);
