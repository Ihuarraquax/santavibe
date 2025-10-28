namespace SantaVibe.Api.Features.Groups.UpdateBudgetSuggestion;

public record UpdateBudgetSuggestionRequest
{
    public decimal? BudgetSuggestion { get; init; }
}
