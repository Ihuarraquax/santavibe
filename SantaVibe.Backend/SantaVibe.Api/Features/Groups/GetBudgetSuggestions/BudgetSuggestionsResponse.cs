namespace SantaVibe.Api.Features.Groups.GetBudgetSuggestions;

/// <summary>
/// Response containing anonymous budget suggestions from group participants
/// </summary>
public record BudgetSuggestionsResponse
{
    /// <summary>
    /// The group identifier (matches request parameter)
    /// </summary>
    public required Guid GroupId { get; init; }

    /// <summary>
    /// Array of budget amounts sorted in ascending order (anonymous)
    /// </summary>
    public required List<decimal> Suggestions { get; init; }

    /// <summary>
    /// Number of suggestions in the array (equal to suggestionsReceived)
    /// </summary>
    public required int Count { get; init; }

    /// <summary>
    /// Total number of participants in the group
    /// </summary>
    public required int ParticipantCount { get; init; }

    /// <summary>
    /// Number of participants who provided a budget suggestion
    /// </summary>
    public required int SuggestionsReceived { get; init; }

    /// <summary>
    /// The finalized budget set by organizer (null before draw execution)
    /// </summary>
    public decimal? CurrentBudget { get; init; }
}
