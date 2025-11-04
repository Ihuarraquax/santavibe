using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SantaVibe.Api.Services.AI;

/// <summary>
/// Service for generating AI-powered gift suggestions using OpenRouter.ai
/// </summary>
public class GiftSuggestionService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<GiftSuggestionService> logger) : IGiftSuggestionService
{
    private const string DefaultModel = "openai/gpt-4o";

    public async Task<GiftSuggestionsResult> GenerateGiftSuggestionsAsync(
        GiftSuggestionContext context,
        CancellationToken cancellationToken = default)
    {
        var httpClient = httpClientFactory.CreateClient("OpenRouter");

        var prompt = BuildPrompt(context);
        var request = new OpenRouterRequest(
            Model: DefaultModel,
            Messages:
            [
                new OpenRouterMessage("system",
                    "Jesteś pomocnym asystentem ds. sugestii prezentów dla polskiego rynku. " +
                    "Pomagasz użytkownikom w znalezieniu przemyślanych, praktycznych pomysłów na prezenty " +
                    "dostępne w Polsce, z uwzględnieniem określonego budżetu. " +
                    "Odpowiadaj w formacie Markdown, tworząc czytelną i przyjazną treść."),
                new OpenRouterMessage("user", prompt)
            ],
            Temperature: 0.7,
            MaxTokens: 1500
        );

        var apiKey = configuration["OpenRouter:ApiKey"]
            ?? throw new InvalidOperationException("OpenRouter API key not configured");

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        try
        {
            logger.LogInformation(
                "Generating gift suggestions via OpenRouter.ai for recipient: {RecipientName}, Budget: {Budget} PLN",
                context.RecipientFirstName, context.Budget);

            var response = await httpClient.PostAsJsonAsync(
                "chat/completions",
                request,
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogError(
                    "OpenRouter.ai API error. StatusCode: {StatusCode}, Response: {Response}",
                    response.StatusCode, errorContent);
                throw new HttpRequestException($"AI service returned {response.StatusCode}");
            }

            var openRouterResponse = await response.Content.ReadFromJsonAsync<OpenRouterResponse>(cancellationToken)
                ?? throw new JsonException("Failed to deserialize OpenRouter response");

            var aiContent = openRouterResponse.Choices.FirstOrDefault()?.Message.Content
                ?? throw new JsonException("No content in AI response");

            logger.LogInformation(
                "AI response received. Tokens used: {TotalTokens} (prompt: {PromptTokens}, completion: {CompletionTokens})",
                openRouterResponse.Usage.TotalTokens,
                openRouterResponse.Usage.PromptTokens,
                openRouterResponse.Usage.CompletionTokens);

            // Return markdown suggestions directly
            var markdownContent = aiContent.Trim();

            logger.LogInformation(
                "Successfully generated gift suggestions markdown (length: {Length} chars)",
                markdownContent.Length);

            return new GiftSuggestionsResult(
                RecipientFirstName: context.RecipientFirstName,
                Budget: context.Budget,
                SuggestionsMarkdown: markdownContent,
                GeneratedAt: DateTime.UtcNow,
                AiModel: DefaultModel
            );
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            logger.LogWarning(ex, "OpenRouter.ai API timeout");
            throw new HttpRequestException("AI service timeout", ex, HttpStatusCode.GatewayTimeout);
        }
    }

    /// <summary>
    /// Builds the prompt for the AI model based on recipient context
    /// </summary>
    private string BuildPrompt(GiftSuggestionContext context)
    {
        var wishlistContext = !string.IsNullOrWhiteSpace(context.WishlistContent)
            ? $"Lista życzeń osoby: '{context.WishlistContent}'"
            : "Osoba nie podała listy życzeń, więc zasugeruj uniwersalne prezenty, które przemówiłyby do szerokiego grona odbiorców.";

        return $"""
            Wygeneruj 3-5 sugestii prezentów dla osoby o imieniu {context.RecipientFirstName} z budżetem {context.Budget:F2} PLN.
            {wishlistContext}

            Weź pod uwagę polski rynek i zasugeruj praktyczne prezenty dostępne w Polsce.

            Dla każdej sugestii podaj:
            - Nazwę kategorii (np. Książki, Elektronika, itp.)
            - Konkretną nazwę produktu lub pomysłu
            - Krótki opis dlaczego to dobry pomysł (1-2 zdania)
            - Przybliżoną cenę w PLN

            Upewnij się, że wszystkie ceny mieszczą się w budżecie {context.Budget:F2} PLN lub są do niego zbliżone.

            Odpowiedź sformatuj w Markdown, używając nagłówków, list i pogrubień dla lepszej czytelności.
            """;
    }
}
