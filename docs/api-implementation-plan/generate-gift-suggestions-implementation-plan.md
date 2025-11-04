# API Endpoint Implementation Plan: Generate Gift Suggestions

## 1. Endpoint Overview

This endpoint generates AI-powered gift suggestions for the authenticated user's assigned Secret Santa recipient. It integrates with OpenRouter.ai to analyze the recipient's wishlist, inferred gender, and group budget to provide 3-5 personalized gift recommendations tailored to the Polish market.

**Key Characteristics:**
- External API dependency (OpenRouter.ai)
- No request body required (all context gathered from database)
- Rate-limited to prevent abuse and control costs (5 requests per user per group per hour)
- Requires completed draw and valid assignment
- Returns structured, actionable gift suggestions

## 2. Request Details

### HTTP Method
`POST`

### URL Structure
```
/api/groups/{groupId}/my-assignment/gift-suggestions
```

### Path Parameters
- **groupId** (UUID, required): Unique identifier of the Secret Santa group

### Authentication
- **Required**: JWT Bearer token in Authorization header
- **Claims needed**: `userId` (extracted from token)

### Request Body
None. All context is gathered from:
- User's assignment in the group (from database)
- Recipient's wishlist (from database)
- Group budget (from database)
- Recipient's first name (from database)

### Headers
```
Authorization: Bearer {jwt_token}
Content-Type: application/json
```

## 3. Used Types

### Command Model
```csharp
namespace SantaVibe.Api.Features.Gifts.GenerateGiftSuggestions;

public record GenerateGiftSuggestionsCommand(Guid GroupId, string UserId);
```

### Response DTOs
```csharp
namespace SantaVibe.Api.Features.Gifts.GenerateGiftSuggestions;

public record GiftSuggestionsResponse(
    Guid GroupId,
    string RecipientFirstName,
    decimal Budget,
    List<GiftSuggestionDto> Suggestions,
    DateTime GeneratedAt,
    string AiModel
);

public record GiftSuggestionDto(
    string Category,
    string ItemName,
    string Description,
    decimal ApproximatePrice,
    string Currency // Always "PLN" for MVP
);
```

### Internal Context Model
```csharp
namespace SantaVibe.Api.Services.AI;

public record GiftSuggestionContext(
    string RecipientFirstName,
    string? WishlistContent,
    string? InferredGender,
    decimal Budget
);
```

### AI Service Models
```csharp
namespace SantaVibe.Api.Services.AI;

// OpenRouter.ai API request
public record OpenRouterRequest(
    string Model,
    List<OpenRouterMessage> Messages,
    double Temperature = 0.7,
    int MaxTokens = 1500
);

public record OpenRouterMessage(
    string Role, // "system" or "user"
    string Content
);

// OpenRouter.ai API response
public record OpenRouterResponse(
    string Id,
    List<OpenRouterChoice> Choices,
    OpenRouterUsage Usage
);

public record OpenRouterChoice(
    OpenRouterMessage Message,
    string FinishReason
);

public record OpenRouterUsage(
    int PromptTokens,
    int CompletionTokens,
    int TotalTokens
);
```

### Rate Limit Models
```csharp
namespace SantaVibe.Api.Services.RateLimit;

public record RateLimitKey(
    string UserId,
    Guid GroupId,
    string Action // "gift-suggestions"
)
{
    public string ToCacheKey() => $"ratelimit:{Action}:{UserId}:{GroupId}";
}

public record RateLimitStatus(
    int CurrentCount,
    int Limit,
    DateTime WindowStart,
    DateTime WindowEnd,
    bool IsExceeded
);
```

## 4. Response Details

### Success Response (200 OK)
```json
{
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "recipientFirstName": "Maria",
  "budget": 100.00,
  "suggestions": [
    {
      "category": "Books",
      "itemName": "Agatha Christie Mystery Collection",
      "description": "A curated box set of 5 classic Agatha Christie mystery novels, perfect for mystery enthusiasts",
      "approximatePrice": 85.00,
      "currency": "PLN"
    },
    {
      "category": "Gardening",
      "itemName": "Premium Gardening Tool Set",
      "description": "Professional-grade gardening tools including pruner, trowel, and cultivator with ergonomic handles",
      "approximatePrice": 95.00,
      "currency": "PLN"
    }
  ],
  "generatedAt": "2025-10-20T17:00:00Z",
  "aiModel": "gpt-4o"
}
```

### Error Responses

#### 401 Unauthorized
```json
{
  "error": "Unauthorized",
  "message": "Authentication is required to access this resource"
}
```

#### 403 Forbidden - Not a Participant
```json
{
  "error": "Forbidden",
  "message": "You are not a participant in this group"
}
```

#### 403 Forbidden - Draw Not Completed
```json
{
  "error": "DrawNotCompleted",
  "message": "Draw has not been completed yet. You cannot view recipient wishlist."
}
```

#### 404 Not Found - Group
```json
{
  "error": "NotFound",
  "message": "Group not found"
}
```

#### 404 Not Found - Assignment
```json
{
  "error": "NotFound",
  "message": "You do not have an assignment in this group"
}
```

#### 429 Too Many Requests
```json
{
  "error": "RateLimitExceeded",
  "message": "You have exceeded the rate limit for gift suggestions. Please try again later.",
  "retryAfter": 3600
}
```

#### 500 Internal Server Error
```json
{
  "error": "AIServiceUnavailable",
  "message": "Gift suggestion service is temporarily unavailable. Please try again later."
}
```

#### 503 Service Unavailable
```json
{
  "error": "AIServiceTimeout",
  "message": "Gift suggestion generation timed out. Please try again."
}
```

## 5. Data Flow

### High-Level Flow
```
1. Request arrives with groupId in path
2. Extract userId from JWT claims (middleware)
3. Validate groupId format (routing)
4. Check rate limit (cache/database)
   ├─ If exceeded → Return 429
   └─ If OK → Continue
5. Load group with participants (database query)
   └─ If not found → Return 404
6. Verify user is participant
   └─ If not → Return 403
7. Verify draw is completed
   └─ If not → Return 403
8. Load user's assignment for this group
   └─ If not found → Return 404
9. Load recipient's wishlist (may be null/empty)
10. Get recipient's first name
11. Infer gender from Polish name (optional enrichment)
12. Build GiftSuggestionContext
13. Call IGiftSuggestionService with context
    ├─ Build AI prompt with Polish market context
    ├─ Call OpenRouter.ai API (with timeout)
    ├─ Parse JSON response
    └─ Validate suggestions format
14. Increment rate limit counter
15. Return GiftSuggestionsResponse (200 OK)
```

### Database Queries

**Query 1: Load Group with Budget and Participants**
```csharp
var group = await context.Groups
    .Include(g => g.Participants)
    .FirstOrDefaultAsync(g => g.GroupId == groupId, ct);
```

**Query 2: Load User's Assignment with Recipient Info**
```csharp
var assignment = await context.Assignments
    .Include(a => a.RecipientUser)
    .Include(a => a.RecipientParticipant)
    .FirstOrDefaultAsync(a =>
        a.GroupId == groupId &&
        a.SantaUserId == userId, ct);
```

**Query 3: Load Recipient's Wishlist**
```csharp
var participant = await context.GroupParticipants
    .FirstOrDefaultAsync(gp =>
        gp.GroupId == groupId &&
        gp.UserId == assignment.RecipientUserId, ct);
// wishlist = participant?.WishlistContent
```

### External Service Call

**OpenRouter.ai API Call:**
```
POST https://openrouter.ai/api/v1/chat/completions
Authorization: Bearer {OPENROUTER_API_KEY}
Content-Type: application/json

{
  "model": "openai/gpt-4o",
  "messages": [
    {
      "role": "system",
      "content": "You are a helpful gift suggestion assistant for the Polish market. Provide practical, thoughtful gift ideas within the specified budget."
    },
    {
      "role": "user",
      "content": "Generate 3-5 gift suggestions for a person named Maria (female) with a budget of 100.00 PLN. Their wishlist: 'I love mystery novels, especially Agatha Christie. I also enjoy gardening tools and flower seeds.' Return JSON array with structure: [{category, itemName, description, approximatePrice, currency}]"
    }
  ],
  "temperature": 0.7,
  "max_tokens": 1500
}
```

**Timeout Configuration:**
- Request timeout: 30 seconds
- Retry policy: None (fail fast for user experience)

## 6. Security Considerations

### Authentication
- JWT Bearer token required (enforced by `[Authorize]` or endpoint filter)
- UserId extracted from `ClaimTypes.NameIdentifier` claim
- Token expiration validated by middleware

### Authorization
- **Participant Check**: User must exist in `group.Participants` collection
- **Draw Completion Check**: `group.DrawCompletedAt` must not be null
- **Assignment Check**: User must have a valid assignment in this group

### Rate Limiting
- **Limit**: 5 requests per user per group per hour
- **Storage**: Distributed cache (Redis recommended) or database table
- **Key Format**: `ratelimit:gift-suggestions:{userId}:{groupId}`
- **Sliding Window**: Track timestamps of last 5 requests
- **Response Headers**:
  ```
  X-RateLimit-Limit: 5
  X-RateLimit-Remaining: 3
  X-RateLimit-Reset: 1729526400 (Unix timestamp)
  Retry-After: 3600 (seconds, only when 429)
  ```

### Input Validation
- groupId must be valid UUID (routing handles this)
- No user-provided content in this endpoint (all data from trusted database)
- AI response validation to prevent injection attacks

### Data Privacy
- User can only see suggestions for their own assigned recipient
- No exposure of other participants' assignments
- Wishlist content sent to external AI service (disclose in privacy policy)

## 7. Error Handling

### Error Categories and Handling

| Error Type | Status Code | Logging Level | Retry Strategy | User Message |
|------------|-------------|---------------|----------------|--------------|
| Invalid JWT | 401 | Information | N/A | "Authentication is required" |
| Not participant | 403 | Information | N/A | "You are not a participant in this group" |
| Draw not completed | 403 | Information | N/A | "Draw has not been completed yet" |
| Group not found | 404 | Information | N/A | "Group not found" |
| Assignment not found | 404 | Warning | N/A | "You do not have an assignment in this group" |
| Rate limit exceeded | 429 | Warning | N/A | "Rate limit exceeded. Try again in X minutes" |
| AI API error (4xx) | 500 | Error | No | "Gift suggestion service is temporarily unavailable" |
| AI API error (5xx) | 500 | Error | No | "Gift suggestion service is temporarily unavailable" |
| AI timeout | 503 | Warning | No | "Gift suggestion generation timed out" |
| AI invalid response | 500 | Error | No | "Failed to generate suggestions. Please try again" |
| Database error | 500 | Critical | N/A | "An unexpected error occurred" |

### Structured Logging Examples

```csharp
// Rate limit exceeded
_logger.LogWarning(
    "Rate limit exceeded for gift suggestions. UserId: {UserId}, GroupId: {GroupId}, Count: {Count}",
    userId, groupId, rateLimitStatus.CurrentCount);

// AI service timeout
_logger.LogWarning(
    "OpenRouter.ai API timeout. GroupId: {GroupId}, UserId: {UserId}, Timeout: {Timeout}ms",
    groupId, userId, 30000);

// AI service error
_logger.LogError(exception,
    "OpenRouter.ai API error. GroupId: {GroupId}, StatusCode: {StatusCode}, Response: {Response}",
    groupId, statusCode, responseBody);

// Invalid AI response
_logger.LogError(
    "Invalid AI response format. GroupId: {GroupId}, Response: {Response}",
    groupId, aiResponse);
```

### Exception Handling Strategy

```csharp
try
{
    // Main handler logic
}
catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.GatewayTimeout)
{
    _logger.LogWarning(ex, "AI service timeout");
    return Results.StatusCode(503, new { error = "AIServiceTimeout", message = "..." });
}
catch (HttpRequestException ex)
{
    _logger.LogError(ex, "AI service error");
    return Results.StatusCode(500, new { error = "AIServiceUnavailable", message = "..." });
}
catch (JsonException ex)
{
    _logger.LogError(ex, "Invalid AI response format");
    return Results.StatusCode(500, new { error = "AIServiceUnavailable", message = "..." });
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error generating gift suggestions");
    return Results.StatusCode(500, new { error = "InternalServerError", message = "..." });
}
```

## 8. Performance Considerations

### Potential Bottlenecks
1. **External API Latency**: OpenRouter.ai calls can take 5-15 seconds
2. **Database Queries**: Multiple queries to load group, assignment, wishlist
3. **Rate Limit Checks**: Cache lookups add latency
4. **Concurrent Requests**: Multiple users generating suggestions simultaneously

### Optimization Strategies

#### 1. Database Query Optimization
```csharp
// Single optimized query with all necessary includes
var assignmentWithContext = await context.Assignments
    .Include(a => a.Group)
        .ThenInclude(g => g.Participants)
    .Include(a => a.RecipientUser)
    .Include(a => a.RecipientParticipant)
    .Where(a => a.GroupId == groupId && a.SantaUserId == userId)
    .Select(a => new {
        Assignment = a,
        Group = a.Group,
        RecipientName = a.RecipientUser.FirstName,
        RecipientWishlist = a.RecipientParticipant.WishlistContent,
        Budget = a.Group.Budget,
        DrawCompleted = a.Group.DrawCompletedAt != null,
        IsParticipant = a.Group.Participants.Any(p => p.UserId == userId)
    })
    .AsNoTracking() // Read-only query
    .FirstOrDefaultAsync(ct);
```

#### 2. Rate Limit Caching
```csharp
// Use distributed cache (Redis) instead of database
// TTL = 1 hour (matches rate limit window)
var cacheKey = $"ratelimit:gift-suggestions:{userId}:{groupId}";
var timestamps = await _cache.GetAsync<List<DateTime>>(cacheKey);
```

#### 3. AI Request Optimization
```csharp
// Use streaming responses if supported by OpenRouter.ai
// Configure connection pooling for HttpClient
// Set reasonable timeout (30 seconds)
var httpClient = _httpClientFactory.CreateClient("OpenRouter");
httpClient.Timeout = TimeSpan.FromSeconds(30);
```

#### 4. Async All The Way
- Use `async`/`await` throughout the call stack
- Use `CancellationToken` to allow request cancellation
- Use `AsNoTracking()` for read-only queries

#### 5. Response Caching (Optional Enhancement)
```csharp
// Cache AI suggestions for 1 hour to avoid duplicate AI calls
// Key: {userId}:{groupId}:suggestions
// Only cache if wishlist hasn't changed
```

### Expected Performance
- **Database queries**: < 100ms (with proper indexing)
- **Rate limit check**: < 10ms (Redis cache)
- **AI API call**: 5-15 seconds (external dependency)
- **Total response time**: 5-15 seconds (dominated by AI call)

### Indexing Requirements
Ensure these indexes exist:
```sql
CREATE INDEX IX_Assignments_GroupId_SantaUserId ON Assignments(GroupId, SantaUserId);
CREATE INDEX IX_GroupParticipants_GroupId_UserId ON GroupParticipants(GroupId, UserId);
CREATE INDEX IX_Groups_GroupId ON Groups(GroupId);
```

## 9. Implementation Steps

### Step 1: Create Service Interfaces and Models

**File: `SantaVibe.Api/Services/AI/IGiftSuggestionService.cs`**
```csharp
namespace SantaVibe.Api.Services.AI;

public interface IGiftSuggestionService
{
    Task<GiftSuggestionsResponse> GenerateGiftSuggestionsAsync(
        GiftSuggestionContext context,
        CancellationToken cancellationToken = default);
}
```

**File: `SantaVibe.Api/Services/AI/GiftSuggestionModels.cs`**
```csharp
namespace SantaVibe.Api.Services.AI;

public record GiftSuggestionContext(
    string RecipientFirstName,
    string? WishlistContent,
    string? InferredGender,
    decimal Budget
);

// OpenRouter.ai API models (as defined in section 3)
```

**File: `SantaVibe.Api/Services/RateLimit/IRateLimitService.cs`**
```csharp
namespace SantaVibe.Api.Services.RateLimit;

public interface IRateLimitService
{
    Task<RateLimitStatus> CheckRateLimitAsync(
        RateLimitKey key,
        int limit,
        TimeSpan window,
        CancellationToken cancellationToken = default);

    Task IncrementRateLimitAsync(
        RateLimitKey key,
        TimeSpan window,
        CancellationToken cancellationToken = default);
}
```

### Step 2: Implement Gender Inference Utility

**File: `SantaVibe.Api/Services/Utilities/PolishNameGenderInference.cs`**
```csharp
namespace SantaVibe.Api.Services.Utilities;

public static class PolishNameGenderInference
{
    private static readonly HashSet<string> MaleEndings = new() { "ek", "aw", "sław", "mir", "cz" };
    private static readonly HashSet<string> FemaleEndings = new() { "a", "ia", "yna", "ina" };

    private static readonly Dictionary<string, string> CommonNames = new()
    {
        // Male names
        ["Jan"] = "male", ["Piotr"] = "male", ["Paweł"] = "male",
        ["Marek"] = "male", ["Tomasz"] = "male", ["Michał"] = "male",

        // Female names
        ["Anna"] = "female", ["Maria"] = "female", ["Katarzyna"] = "female",
        ["Magdalena"] = "female", ["Agnieszka"] = "female", ["Ewa"] = "female"
    };

    public static string? InferGender(string firstName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            return null;

        // Check common names dictionary
        if (CommonNames.TryGetValue(firstName, out var gender))
            return gender;

        // Check endings (simplified rules)
        if (firstName.EndsWith("a") && !firstName.EndsWith("sza"))
            return "female";

        return null; // Unable to infer with confidence
    }
}
```

### Step 3: Implement Rate Limit Service

**File: `SantaVibe.Api/Services/RateLimit/RateLimitService.cs`**
```csharp
namespace SantaVibe.Api.Services.RateLimit;

public class RateLimitService : IRateLimitService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RateLimitService> _logger;

    public RateLimitService(IDistributedCache cache, ILogger<RateLimitService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<RateLimitStatus> CheckRateLimitAsync(
        RateLimitKey key,
        int limit,
        TimeSpan window,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = key.ToCacheKey();
        var timestampsJson = await _cache.GetStringAsync(cacheKey, cancellationToken);

        var now = DateTime.UtcNow;
        var windowStart = now - window;

        var timestamps = string.IsNullOrEmpty(timestampsJson)
            ? new List<DateTime>()
            : JsonSerializer.Deserialize<List<DateTime>>(timestampsJson) ?? new List<DateTime>();

        // Remove expired timestamps
        timestamps = timestamps.Where(t => t > windowStart).ToList();

        var currentCount = timestamps.Count;
        var isExceeded = currentCount >= limit;

        return new RateLimitStatus(
            CurrentCount: currentCount,
            Limit: limit,
            WindowStart: windowStart,
            WindowEnd: now + window,
            IsExceeded: isExceeded
        );
    }

    public async Task IncrementRateLimitAsync(
        RateLimitKey key,
        TimeSpan window,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = key.ToCacheKey();
        var timestampsJson = await _cache.GetStringAsync(cacheKey, cancellationToken);

        var now = DateTime.UtcNow;
        var windowStart = now - window;

        var timestamps = string.IsNullOrEmpty(timestampsJson)
            ? new List<DateTime>()
            : JsonSerializer.Deserialize<List<DateTime>>(timestampsJson) ?? new List<DateTime>();

        // Remove expired timestamps and add new one
        timestamps = timestamps.Where(t => t > windowStart).ToList();
        timestamps.Add(now);

        var updatedJson = JsonSerializer.Serialize(timestamps);
        await _cache.SetStringAsync(
            cacheKey,
            updatedJson,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = window },
            cancellationToken
        );
    }
}
```

### Step 4: Implement Gift Suggestion Service

**File: `SantaVibe.Api/Services/AI/GiftSuggestionService.cs`**
```csharp
namespace SantaVibe.Api.Services.AI;

public class GiftSuggestionService : IGiftSuggestionService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GiftSuggestionService> _logger;

    private const string DefaultModel = "openai/gpt-4o";

    public GiftSuggestionService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<GiftSuggestionService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<GiftSuggestionsResponse> GenerateGiftSuggestionsAsync(
        GiftSuggestionContext context,
        CancellationToken cancellationToken = default)
    {
        var httpClient = _httpClientFactory.CreateClient("OpenRouter");
        httpClient.Timeout = TimeSpan.FromSeconds(30);

        var prompt = BuildPrompt(context);
        var request = new OpenRouterRequest(
            Model: DefaultModel,
            Messages: new List<OpenRouterMessage>
            {
                new("system", "You are a helpful gift suggestion assistant for the Polish market. Provide practical, thoughtful gift ideas within the specified budget. Return ONLY a valid JSON array with no additional text."),
                new("user", prompt)
            },
            Temperature: 0.7,
            MaxTokens: 1500
        );

        var apiKey = _configuration["OpenRouter:ApiKey"]
            ?? throw new InvalidOperationException("OpenRouter API key not configured");

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        try
        {
            var response = await httpClient.PostAsJsonAsync(
                "https://openrouter.ai/api/v1/chat/completions",
                request,
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "OpenRouter.ai API error. StatusCode: {StatusCode}, Response: {Response}",
                    response.StatusCode, errorContent);
                throw new HttpRequestException($"AI service returned {response.StatusCode}");
            }

            var openRouterResponse = await response.Content.ReadFromJsonAsync<OpenRouterResponse>(cancellationToken)
                ?? throw new JsonException("Failed to deserialize OpenRouter response");

            var aiContent = openRouterResponse.Choices.FirstOrDefault()?.Message.Content
                ?? throw new JsonException("No content in AI response");

            // Parse suggestions from AI response (expecting JSON array)
            var suggestions = ParseSuggestions(aiContent);

            return new GiftSuggestionsResponse(
                GroupId: Guid.Empty, // Will be set by handler
                RecipientFirstName: context.RecipientFirstName,
                Budget: context.Budget,
                Suggestions: suggestions,
                GeneratedAt: DateTime.UtcNow,
                AiModel: DefaultModel
            );
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogWarning(ex, "OpenRouter.ai API timeout");
            throw new HttpRequestException("AI service timeout", ex, HttpStatusCode.GatewayTimeout);
        }
    }

    private string BuildPrompt(GiftSuggestionContext context)
    {
        var genderContext = context.InferredGender != null
            ? $" ({context.InferredGender})"
            : "";

        var wishlistContext = !string.IsNullOrWhiteSpace(context.WishlistContent)
            ? $"Their wishlist: '{context.WishlistContent}'"
            : "They have not provided a wishlist.";

        return $@"Generate 3-5 gift suggestions for a person named {context.RecipientFirstName}{genderContext} with a budget of {context.Budget:F2} PLN.
{wishlistContext}

Consider the Polish market and suggest practical gifts available in Poland.
Return ONLY a JSON array with this exact structure (no additional text or markdown):
[
  {{
    ""category"": ""Category name"",
    ""itemName"": ""Specific item name"",
    ""description"": ""Detailed description"",
    ""approximatePrice"": 85.00,
    ""currency"": ""PLN""
  }}
]

Ensure all prices are within or close to the budget of {context.Budget:F2} PLN.";
    }

    private List<GiftSuggestionDto> ParseSuggestions(string aiContent)
    {
        // Remove markdown code blocks if present
        var jsonContent = aiContent.Trim();
        if (jsonContent.StartsWith("```json"))
            jsonContent = jsonContent.Substring(7).TrimStart();
        if (jsonContent.StartsWith("```"))
            jsonContent = jsonContent.Substring(3).TrimStart();
        if (jsonContent.EndsWith("```"))
            jsonContent = jsonContent.Substring(0, jsonContent.Length - 3).TrimEnd();

        var suggestions = JsonSerializer.Deserialize<List<GiftSuggestionDto>>(
            jsonContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        ) ?? throw new JsonException("Failed to parse gift suggestions");

        if (suggestions.Count < 3 || suggestions.Count > 5)
            _logger.LogWarning("AI returned {Count} suggestions (expected 3-5)", suggestions.Count);

        return suggestions;
    }
}
```

### Step 5: Create MediatR Command and Handler

**File: `SantaVibe.Api/Features/Gifts/GenerateGiftSuggestions/GenerateGiftSuggestionsCommand.cs`**
```csharp
namespace SantaVibe.Api.Features.Gifts.GenerateGiftSuggestions;

public record GenerateGiftSuggestionsCommand(Guid GroupId, string UserId) : IRequest<GiftSuggestionsResponse>;
```

**File: `SantaVibe.Api/Features/Gifts/GenerateGiftSuggestions/GenerateGiftSuggestionsHandler.cs`**
```csharp
namespace SantaVibe.Api.Features.Gifts.GenerateGiftSuggestions;

public class GenerateGiftSuggestionsHandler : IRequestHandler<GenerateGiftSuggestionsCommand, GiftSuggestionsResponse>
{
    private readonly ApplicationDbContext _context;
    private readonly IGiftSuggestionService _giftSuggestionService;
    private readonly IRateLimitService _rateLimitService;
    private readonly ILogger<GenerateGiftSuggestionsHandler> _logger;

    private const int RateLimit = 5;
    private static readonly TimeSpan RateLimitWindow = TimeSpan.FromHours(1);

    public GenerateGiftSuggestionsHandler(
        ApplicationDbContext context,
        IGiftSuggestionService giftSuggestionService,
        IRateLimitService rateLimitService,
        ILogger<GenerateGiftSuggestionsHandler> logger)
    {
        _context = context;
        _giftSuggestionService = giftSuggestionService;
        _rateLimitService = rateLimitService;
        _logger = logger;
    }

    public async Task<GiftSuggestionsResponse> Handle(
        GenerateGiftSuggestionsCommand request,
        CancellationToken cancellationToken)
    {
        // Step 1: Check rate limit
        var rateLimitKey = new RateLimitKey(request.UserId, request.GroupId, "gift-suggestions");
        var rateLimitStatus = await _rateLimitService.CheckRateLimitAsync(
            rateLimitKey,
            RateLimit,
            RateLimitWindow,
            cancellationToken
        );

        if (rateLimitStatus.IsExceeded)
        {
            _logger.LogWarning(
                "Rate limit exceeded for gift suggestions. UserId: {UserId}, GroupId: {GroupId}",
                request.UserId, request.GroupId);

            throw new RateLimitExceededException(
                $"Rate limit exceeded. Please try again after {rateLimitStatus.WindowEnd:HH:mm}",
                (int)(rateLimitStatus.WindowEnd - DateTime.UtcNow).TotalSeconds
            );
        }

        // Step 2: Load assignment with all necessary data (optimized single query)
        var assignmentData = await _context.Assignments
            .Include(a => a.Group)
                .ThenInclude(g => g.Participants)
            .Include(a => a.RecipientUser)
            .Where(a => a.GroupId == request.GroupId && a.SantaUserId == request.UserId)
            .Select(a => new
            {
                GroupId = a.GroupId,
                Budget = a.Group.Budget,
                DrawCompleted = a.Group.DrawCompletedAt != null,
                IsParticipant = a.Group.Participants.Any(p => p.UserId == request.UserId),
                RecipientFirstName = a.RecipientUser.FirstName,
                RecipientUserId = a.RecipientUserId
            })
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        // Step 3: Validate business rules
        if (assignmentData == null)
        {
            var groupExists = await _context.Groups.AnyAsync(g => g.GroupId == request.GroupId, cancellationToken);
            if (!groupExists)
                throw new NotFoundException("Group not found");

            var isParticipant = await _context.GroupParticipants.AnyAsync(
                gp => gp.GroupId == request.GroupId && gp.UserId == request.UserId,
                cancellationToken
            );

            if (!isParticipant)
                throw new ForbiddenException("You are not a participant in this group");

            var drawCompleted = await _context.Groups
                .Where(g => g.GroupId == request.GroupId)
                .Select(g => g.DrawCompletedAt != null)
                .FirstOrDefaultAsync(cancellationToken);

            if (!drawCompleted)
                throw new ForbiddenException("Draw has not been completed yet. You cannot view recipient information.");

            throw new NotFoundException("You do not have an assignment in this group");
        }

        if (!assignmentData.IsParticipant)
            throw new ForbiddenException("You are not a participant in this group");

        if (!assignmentData.DrawCompleted)
            throw new ForbiddenException("Draw has not been completed yet. You cannot view recipient information.");

        if (!assignmentData.Budget.HasValue)
            throw new InvalidOperationException("Group budget is not set");

        // Step 4: Load recipient's wishlist
        var wishlist = await _context.GroupParticipants
            .Where(gp => gp.GroupId == request.GroupId && gp.UserId == assignmentData.RecipientUserId)
            .Select(gp => gp.WishlistContent)
            .FirstOrDefaultAsync(cancellationToken);

        // Step 5: Infer gender from name
        var inferredGender = PolishNameGenderInference.InferGender(assignmentData.RecipientFirstName);

        // Step 6: Build context and call AI service
        var context = new GiftSuggestionContext(
            RecipientFirstName: assignmentData.RecipientFirstName,
            WishlistContent: wishlist,
            InferredGender: inferredGender,
            Budget: assignmentData.Budget.Value
        );

        var response = await _giftSuggestionService.GenerateGiftSuggestionsAsync(context, cancellationToken);

        // Step 7: Increment rate limit counter
        await _rateLimitService.IncrementRateLimitAsync(rateLimitKey, RateLimitWindow, cancellationToken);

        // Step 8: Return response with correct GroupId
        return response with { GroupId = request.GroupId };
    }
}
```

### Step 6: Create Custom Exceptions

**File: `SantaVibe.Api/Common/Exceptions/RateLimitExceededException.cs`**
```csharp
namespace SantaVibe.Api.Common.Exceptions;

public class RateLimitExceededException : Exception
{
    public int RetryAfter { get; }

    public RateLimitExceededException(string message, int retryAfter) : base(message)
    {
        RetryAfter = retryAfter;
    }
}
```

### Step 7: Register Endpoint

**File: `SantaVibe.Api/Features/Gifts/GenerateGiftSuggestions/GenerateGiftSuggestionsEndpoint.cs`**
```csharp
namespace SantaVibe.Api.Features.Gifts.GenerateGiftSuggestions;

public static class GenerateGiftSuggestionsEndpoint
{
    public static void MapGenerateGiftSuggestionsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/groups/{groupId}/my-assignment/gift-suggestions", HandleAsync)
            .RequireAuthorization()
            .WithName("GenerateGiftSuggestions")
            .WithTags("Gifts")
            .Produces<GiftSuggestionsResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status429TooManyRequests)
            .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
            .Produces<ErrorResponse>(StatusCodes.Status503ServiceUnavailable);
    }

    private static async Task<IResult> HandleAsync(
        Guid groupId,
        ISender sender,
        ICurrentUserService currentUserService,
        ILogger<GenerateGiftSuggestionsEndpoint> logger,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new UnauthorizedAccessException("User ID not found in claims");

        try
        {
            var command = new GenerateGiftSuggestionsCommand(groupId, userId);
            var response = await sender.Send(command, cancellationToken);

            return Results.Ok(response);
        }
        catch (RateLimitExceededException ex)
        {
            logger.LogWarning(ex, "Rate limit exceeded for user {UserId}", userId);
            return Results.StatusCode(429, new ErrorResponse(
                Error: "RateLimitExceeded",
                Message: ex.Message,
                Details: new Dictionary<string, object> { ["retryAfter"] = ex.RetryAfter }
            ));
        }
        catch (ForbiddenException ex)
        {
            logger.LogInformation("Forbidden access: {Message}", ex.Message);
            return Results.StatusCode(403, new ErrorResponse(
                Error: ex.Message.Contains("Draw") ? "DrawNotCompleted" : "Forbidden",
                Message: ex.Message
            ));
        }
        catch (NotFoundException ex)
        {
            logger.LogInformation("Not found: {Message}", ex.Message);
            return Results.NotFound(new ErrorResponse(Error: "NotFound", Message: ex.Message));
        }
        catch (HttpRequestException ex) when (
            ex.StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.GatewayTimeout)
        {
            logger.LogWarning(ex, "AI service timeout");
            return Results.StatusCode(503, new ErrorResponse(
                Error: "AIServiceTimeout",
                Message: "Gift suggestion generation timed out. Please try again."
            ));
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "AI service error");
            return Results.StatusCode(500, new ErrorResponse(
                Error: "AIServiceUnavailable",
                Message: "Gift suggestion service is temporarily unavailable. Please try again later."
            ));
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Invalid AI response format");
            return Results.StatusCode(500, new ErrorResponse(
                Error: "AIServiceUnavailable",
                Message: "Gift suggestion service is temporarily unavailable. Please try again later."
            ));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error generating gift suggestions");
            return Results.StatusCode(500, new ErrorResponse(
                Error: "InternalServerError",
                Message: "An unexpected error occurred. Please try again later."
            ));
        }
    }
}
```

### Step 8: Configure Services in DI Container

**File: `SantaVibe.Api/Program.cs`** (add to existing configuration)
```csharp
// Register HttpClient for OpenRouter
builder.Services.AddHttpClient("OpenRouter", client =>
{
    client.BaseAddress = new Uri("https://openrouter.ai/api/v1/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Register distributed cache (Redis recommended for production)
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "SantaVibe:";
});

// Register services
builder.Services.AddScoped<IGiftSuggestionService, GiftSuggestionService>();
builder.Services.AddScoped<IRateLimitService, RateLimitService>();

// Map endpoint
app.MapGenerateGiftSuggestionsEndpoint();
```

### Step 9: Add Configuration

**File: `appsettings.json`**
```json
{
  "OpenRouter": {
    "ApiKey": "your-api-key-here"
  },
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  }
}
```

**File: `appsettings.Production.json`**
```json
{
  "OpenRouter": {
    "ApiKey": "${OPENROUTER_API_KEY}"
  },
  "ConnectionStrings": {
    "Redis": "${REDIS_CONNECTION_STRING}"
  }
}
```

### Step 10: Write Unit Tests

**File: `SantaVibe.Api.Tests/Features/Gifts/GenerateGiftSuggestionsHandlerTests.cs`**
```csharp
public class GenerateGiftSuggestionsHandlerTests
{
    [Fact]
    public async Task Handle_ValidRequest_ReturnsGiftSuggestions()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var (groupId, userId) = await SeedTestDataAsync(context);

        var giftService = Substitute.For<IGiftSuggestionService>();
        giftService.GenerateGiftSuggestionsAsync(Arg.Any<GiftSuggestionContext>(), Arg.Any<CancellationToken>())
            .Returns(CreateMockResponse());

        var rateLimitService = Substitute.For<IRateLimitService>();
        rateLimitService.CheckRateLimitAsync(Arg.Any<RateLimitKey>(), Arg.Any<int>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new RateLimitStatus(3, 5, DateTime.UtcNow.AddHours(-1), DateTime.UtcNow, false));

        var handler = new GenerateGiftSuggestionsHandler(context, giftService, rateLimitService, NullLogger<GenerateGiftSuggestionsHandler>.Instance);
        var command = new GenerateGiftSuggestionsCommand(groupId, userId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.GroupId.Should().Be(groupId);
        result.Suggestions.Should().HaveCountGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task Handle_RateLimitExceeded_ThrowsException()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var (groupId, userId) = await SeedTestDataAsync(context);

        var rateLimitService = Substitute.For<IRateLimitService>();
        rateLimitService.CheckRateLimitAsync(Arg.Any<RateLimitKey>(), Arg.Any<int>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new RateLimitStatus(5, 5, DateTime.UtcNow.AddHours(-1), DateTime.UtcNow, true));

        var handler = new GenerateGiftSuggestionsHandler(context, null, rateLimitService, NullLogger<GenerateGiftSuggestionsHandler>.Instance);
        var command = new GenerateGiftSuggestionsCommand(groupId, userId);

        // Act & Assert
        await Assert.ThrowsAsync<RateLimitExceededException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DrawNotCompleted_ThrowsForbiddenException()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var (groupId, userId) = await SeedTestDataWithoutDrawAsync(context);

        var rateLimitService = Substitute.For<IRateLimitService>();
        rateLimitService.CheckRateLimitAsync(Arg.Any<RateLimitKey>(), Arg.Any<int>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new RateLimitStatus(0, 5, DateTime.UtcNow.AddHours(-1), DateTime.UtcNow, false));

        var handler = new GenerateGiftSuggestionsHandler(context, null, rateLimitService, NullLogger<GenerateGiftSuggestionsHandler>.Instance);
        var command = new GenerateGiftSuggestionsCommand(groupId, userId);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(command, CancellationToken.None));
    }
}
```

### Step 11: Write Integration Tests

**File: `SantaVibe.Api.Tests/Integration/GiftSuggestionsEndpointTests.cs`**
```csharp
public class GiftSuggestionsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GiftSuggestionsEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task POST_GenerateGiftSuggestions_WithValidData_Returns200()
    {
        // Arrange
        var client = _factory.CreateClient();
        var (groupId, token) = await SetupAuthenticatedUserWithAssignmentAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsync($"/api/groups/{groupId}/my-assignment/gift-suggestions", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<GiftSuggestionsResponse>();
        content.Should().NotBeNull();
        content!.Suggestions.Should().HaveCountGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task POST_GenerateGiftSuggestions_Unauthorized_Returns401()
    {
        // Arrange
        var client = _factory.CreateClient();
        var groupId = Guid.NewGuid();

        // Act
        var response = await client.PostAsync($"/api/groups/{groupId}/my-assignment/gift-suggestions", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

### Step 12: Update API Documentation (Swagger)

Swagger documentation should be auto-generated from the endpoint registration. Ensure proper XML comments and attributes are in place.

---

## 10. Testing Checklist

- [ ] Unit test: Valid request returns gift suggestions
- [ ] Unit test: Rate limit exceeded throws exception
- [ ] Unit test: Draw not completed throws ForbiddenException
- [ ] Unit test: User not participant throws ForbiddenException
- [ ] Unit test: Group not found throws NotFoundException
- [ ] Unit test: Assignment not found throws NotFoundException
- [ ] Integration test: Valid request returns 200 OK
- [ ] Integration test: Unauthorized request returns 401
- [ ] Integration test: Rate limit exceeded returns 429
- [ ] Integration test: AI service timeout returns 503
- [ ] Integration test: AI service error returns 500
- [ ] Manual test: Verify AI suggestions quality
- [ ] Manual test: Verify Polish name gender inference
- [ ] Manual test: Verify rate limiting with multiple requests
- [ ] Load test: Concurrent requests from multiple users

---

## 11. Deployment Checklist

- [ ] Add `OPENROUTER_API_KEY` environment variable
- [ ] Add `REDIS_CONNECTION_STRING` environment variable
- [ ] Verify Redis/distributed cache is available
- [ ] Configure HttpClient connection pooling limits
- [ ] Set appropriate timeout values for production
- [ ] Enable Serilog structured logging
- [ ] Configure rate limit monitoring/alerting
- [ ] Test AI service failover behavior
- [ ] Document AI service cost monitoring
- [ ] Add health check for OpenRouter.ai availability
