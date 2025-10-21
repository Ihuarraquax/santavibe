using System.ComponentModel.DataAnnotations;

namespace SantaVibe.Api.Features.Invitations.AcceptInvitation;

/// <summary>
/// Request DTO for accepting an invitation and joining a group
/// </summary>
public record AcceptInvitationRequest
{
    /// <summary>
    /// Optional budget suggestion in PLN (FR-009)
    /// </summary>
    [Range(0.01, 99999999.99, ErrorMessage = "Budget suggestion must be between 0.01 and 99999999.99")]
    public decimal? BudgetSuggestion { get; init; }
}
