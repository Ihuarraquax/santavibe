namespace SantaVibe.Api.Features.Gifts.GenerateGiftSuggestions;

public record GiftSuggestionsResponse(
    Guid GroupId,
    string RecipientFirstName,
    decimal Budget,
    string SuggestionsMarkdown,
    DateTime GeneratedAt,
    string AiModel
);
