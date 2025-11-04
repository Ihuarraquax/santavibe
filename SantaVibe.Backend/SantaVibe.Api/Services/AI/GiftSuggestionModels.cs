using System.Text.Json.Serialization;

namespace SantaVibe.Api.Services.AI;

/// <summary>
/// Context information needed to generate gift suggestions for a recipient
/// </summary>
public record GiftSuggestionContext(
    string RecipientFirstName,
    string? WishlistContent,
    decimal Budget
);

/// <summary>
/// Result of gift suggestion generation including suggestions and metadata
/// </summary>
public record GiftSuggestionsResult(
    string RecipientFirstName,
    decimal Budget,
    string SuggestionsMarkdown,
    DateTime GeneratedAt,
    string AiModel
);

// OpenRouter.ai API request models

/// <summary>
/// Request to OpenRouter.ai chat completion API
/// </summary>
public record OpenRouterRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("messages")] List<OpenRouterMessage> Messages,
    [property: JsonPropertyName("temperature")] double Temperature = 0.7,
    [property: JsonPropertyName("max_tokens")] int MaxTokens = 1500
);

/// <summary>
/// Message in OpenRouter.ai conversation
/// </summary>
public record OpenRouterMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content
);

// OpenRouter.ai API response models

/// <summary>
/// Response from OpenRouter.ai chat completion API
/// </summary>
public record OpenRouterResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("choices")] List<OpenRouterChoice> Choices,
    [property: JsonPropertyName("usage")] OpenRouterUsage Usage
);

/// <summary>
/// Choice containing the AI-generated message
/// </summary>
public record OpenRouterChoice(
    [property: JsonPropertyName("message")] OpenRouterMessage Message,
    [property: JsonPropertyName("finish_reason")] string FinishReason
);

/// <summary>
/// Token usage statistics from OpenRouter.ai
/// </summary>
public record OpenRouterUsage(
    [property: JsonPropertyName("prompt_tokens")] int PromptTokens,
    [property: JsonPropertyName("completion_tokens")] int CompletionTokens,
    [property: JsonPropertyName("total_tokens")] int TotalTokens
);
