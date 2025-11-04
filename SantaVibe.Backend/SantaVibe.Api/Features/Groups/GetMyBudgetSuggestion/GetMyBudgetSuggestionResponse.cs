namespace SantaVibe.Api.Features.Groups.GetMyBudgetSuggestion;

public record GetMyBudgetSuggestionResponse
{
    public required Guid GroupId { get; init; }
    public decimal? BudgetSuggestion { get; init; }
    public DateTimeOffset? SubmittedAt { get; init; }
}
