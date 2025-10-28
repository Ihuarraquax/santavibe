namespace SantaVibe.Api.Features.Groups.UpdateBudgetSuggestion;

public record UpdateBudgetSuggestionResponse
{
    public required Guid GroupId { get; init; }
    public decimal? BudgetSuggestion { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
