namespace SantaVibe.Api.Services.AI;

public interface IGiftSuggestionService
{
    Task<GiftSuggestionsResult> GenerateGiftSuggestionsAsync(
        GiftSuggestionContext context,
        CancellationToken cancellationToken = default);
}
