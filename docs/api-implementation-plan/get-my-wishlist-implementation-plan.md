# API Endpoint Implementation Plan: Get My Wishlist

## 1. Endpoint Overview

**Purpose**: Retrieve the authenticated user's wishlist content for a specific Secret Santa group.

**Key Functionality**:
- Read-only operation to fetch wishlist data
- Returns wishlist content and last modification timestamp
- Handles empty wishlist scenario (null values)
- Enforces participant authorization (user must be in the group)

**Business Context**:
- Users can view their own wishlist to review what they've shared with their Secret Santa
- Wishlist can be empty (null content and timestamp)
- Only participants in the group can access this endpoint
- **IMPORTANT**: Only available after draw completion - users create wishlists post-draw when final budget is known

---

## 2. Request Details

**HTTP Method**: `GET`

**URL Structure**: `/api/groups/{groupId}/participants/me/wishlist`

**Path Parameters**:
- `groupId` (required, UUID): The unique identifier of the Secret Santa group

**Query Parameters**: None

**Request Headers**:
- `Authorization` (required): Bearer JWT token containing user identity

**Request Body**: None (GET request)

**Authorization Requirements**:
- User must be authenticated (JWT token)
- User must be a participant in the group
- Draw must be completed for the group

---

## 3. Used Types

### Query Model

```csharp
// File: SantaVibe.Api/Features/Wishlists/GetMyWishlist/GetMyWishlistQuery.cs

public record GetMyWishlistQuery(
    Guid GroupId
) : IRequest<Result<GetMyWishlistResponse>>;
```

### Response DTO

```csharp
// File: SantaVibe.Api/Features/Wishlists/GetMyWishlist/GetMyWishlistResponse.cs

public class GetMyWishlistResponse
{
    public required Guid GroupId { get; init; }
    public string? WishlistContent { get; init; }
    public DateTimeOffset? LastModified { get; init; }
}
```

### Handler

```csharp
// File: SantaVibe.Api/Features/Wishlists/GetMyWishlist/GetMyWishlistQueryHandler.cs

public class GetMyWishlistQueryHandler : IRequestHandler<GetMyWishlistQuery, Result<GetMyWishlistResponse>>
{
    private readonly ApplicationDbContext _context;
    private readonly IUserAccessor _userAccessor;
    private readonly ILogger<GetMyWishlistQueryHandler> _logger;

    // Constructor and Handle method (see Implementation Steps)
}
```

### Endpoint

```csharp
// File: SantaVibe.Api/Features/Wishlists/GetMyWishlist/GetMyWishlistEndpoint.cs

public static class GetMyWishlistEndpoint
{
    public static IEndpointRouteBuilder MapGetMyWishlistEndpoint(this IEndpointRouteBuilder app)
    {
        // Endpoint registration (see Implementation Steps)
    }
}
```

---

## 4. Response Details

### Success Response (200 OK) - With Wishlist Content

```json
{
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "wishlistContent": "I would love books about cooking, especially Italian cuisine. Size M for clothes. I also enjoy board games.",
  "lastModified": "2025-10-15T14:30:00Z"
}
```

**Headers**: `Content-Type: application/json`

---

### Success Response (200 OK) - Empty Wishlist

```json
{
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "wishlistContent": null,
  "lastModified": null
}
```

**Note**: Empty wishlist is represented by null values, not an empty string.

---

### Error Response (401 Unauthorized)

**Trigger**: Missing or invalid JWT token

**Handled By**: ASP.NET Core authentication middleware (automatic)

```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401
}
```

---

### Error Response (403 Forbidden)

**Trigger**: User is not a participant in the group

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.4",
  "title": "Forbidden",
  "status": 403,
  "detail": "You are not a participant in this group",
  "extensions": {
    "error": "NotParticipant"
  }
}
```

**Error Code**: `NotParticipant`
**Handler Logic**: Return `Result<GetMyWishlistResponse>.Failure("NotParticipant", "You are not a participant in this group")`

---

### Error Response (403 Forbidden) - Draw Not Completed

**Trigger**: Draw has not been completed for the group

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.4",
  "title": "Forbidden",
  "status": 403,
  "detail": "Wishlist can only be viewed after the draw has been completed",
  "extensions": {
    "error": "DrawNotCompleted"
  }
}
```

**Error Code**: `DrawNotCompleted`
**Handler Logic**: Return `Result<GetMyWishlistResponse>.Failure("DrawNotCompleted", "Wishlist can only be viewed after the draw has been completed")`

---

### Error Response (404 Not Found)

**Trigger**: Group with specified groupId does not exist

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found",
  "status": 404,
  "detail": "Group not found",
  "extensions": {
    "error": "GroupNotFound"
  }
}
```

**Error Code**: `GroupNotFound`
**Handler Logic**: Return `Result<GetMyWishlistResponse>.Failure("GroupNotFound", "Group not found")`

---

### Error Response (400 Bad Request)

**Trigger**: Invalid groupId format (not a valid UUID)

**Handled By**: ASP.NET Core model binding (automatic)

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "groupId": ["The value 'invalid-uuid' is not valid."]
  }
}
```

---

### Error Response (500 Internal Server Error)

**Trigger**: Unexpected exception during processing

**Handled By**: GlobalExceptionHandler middleware

```json
{
  "error": "InternalServerError",
  "message": "An unexpected error occurred. Please try again later.",
  "traceId": "0HMVFE0A0000B:00000001"
}
```

---

## 5. Data Flow

### High-Level Flow

```
1. HTTP Request arrives at endpoint
   ↓
2. ASP.NET Core authentication middleware validates JWT token
   ↓
3. Endpoint extracts groupId from route parameters
   ↓
4. MediatR sends GetMyWishlistQuery to handler
   ↓
5. Handler extracts userId from IUserAccessor (JWT claims)
   ↓
6. Handler queries database with filtered Include:
   - Find Group by groupId
   - Include only GroupParticipant where UserId = current user
   - Use AsNoTracking() for read-only performance
   ↓
7. Handler validates authorization:
   - Group exists? → No: Return 404 GroupNotFound
   - User is participant? → No: Return 403 NotParticipant
   - Draw completed? → No: Return 403 DrawNotCompleted
   ↓
8. Handler maps entity to response DTO:
   - groupId from Group.Id
   - wishlistContent from GroupParticipant.WishlistContent (nullable)
   - lastModified from GroupParticipant.WishlistLastModified (nullable)
   ↓
9. Handler returns Result<GetMyWishlistResponse>.Success(response)
   ↓
10. Endpoint checks result:
    - Success? → Return 200 OK with response
    - Failure? → Call result.ToProblem() to map error to ProblemDetails
   ↓
11. HTTP Response sent to client
```

### Database Query

```csharp
var group = await _context.Groups
    .AsNoTracking()
    .Include(g => g.GroupParticipants.Where(gp => gp.UserId == userId.ToString()))
    .FirstOrDefaultAsync(g => g.Id == query.GroupId, cancellationToken);
```

**Query Optimization**:
- `AsNoTracking()`: Read-only query, no change tracking overhead
- Filtered `Include()`: Loads only current user's participant record, not all participants
- `FirstOrDefaultAsync()`: Efficient single-result query with cancellation support

**Entities Accessed**:
- `Groups` table (primary)
- `GroupParticipants` table (navigation property with filter)

---

## 6. Security Considerations

### Authentication

**Mechanism**: JWT Bearer token authentication

**Enforcement**:
- Endpoint decorated with `.RequireAuthorization()`
- ASP.NET Core authentication middleware validates token signature, expiration, and claims
- User identity extracted from `ClaimTypes.NameIdentifier` claim

**Failure Handling**: Automatic 401 Unauthorized if token is missing, expired, or invalid

---

### Authorization

**Rules**:
1. User must be authenticated (handled by middleware)
2. Group must exist (404 if not found)
3. User must be a participant in the group (403 if not)
4. Draw must be completed (403 if not) - wishlists can only be created/viewed post-draw

**Implementation**:
- Authorization logic in handler (not middleware)
- Query filters to only load user's own participant record
- Explicit validation before returning data

**Privacy**:
- Users can only access their own wishlist (not other participants')
- No endpoint reveals other users' wishlists before draw

---

### Input Validation

**Path Parameter**:
- `groupId`: Validated as UUID by ASP.NET Core model binding
- Invalid UUIDs result in automatic 400 Bad Request

**No Additional Validation Needed**:
- No request body (GET request)
- No query parameters
- No custom validation filter required

---

### Data Privacy

- User can only see their own wishlist content
- No exposure of other participants' data
- Follows principle of least privilege (minimal data returned)

---

## 7. Error Handling

### Error Summary Table

| Error Scenario | Error Code | HTTP Status | Returned By | Message |
|---------------|------------|-------------|-------------|---------|
| Missing JWT token | N/A | 401 Unauthorized | Auth Middleware | Automatic |
| Invalid JWT token | N/A | 401 Unauthorized | Auth Middleware | Automatic |
| Expired JWT token | N/A | 401 Unauthorized | Auth Middleware | Automatic |
| Invalid groupId format | N/A | 400 Bad Request | Model Binding | "The value 'X' is not valid." |
| Group not found | `GroupNotFound` | 404 Not Found | Handler | "Group not found" |
| User not participant | `NotParticipant` | 403 Forbidden | Handler | "You are not a participant in this group" |
| Draw not completed | `DrawNotCompleted` | 403 Forbidden | Handler | "Wishlist can only be viewed after the draw has been completed" |
| Unexpected exception | N/A | 500 Internal Server Error | GlobalExceptionHandler | "An unexpected error occurred..." |

---

### Handler Error Handling Pattern

```csharp
// Group not found
if (group == null)
{
    _logger.LogWarning(
        "User {UserId} attempted to access wishlist for non-existent group {GroupId}",
        userId, query.GroupId);
    return Result<GetMyWishlistResponse>.Failure("GroupNotFound", "Group not found");
}

// User not a participant
var participant = group.GroupParticipants.FirstOrDefault();
if (participant == null)
{
    _logger.LogWarning(
        "User {UserId} attempted to access wishlist for group {GroupId} but is not a participant",
        userId, query.GroupId);
    return Result<GetMyWishlistResponse>.Failure("NotParticipant", "You are not a participant in this group");
}

// Draw not completed
if (!group.DrawCompletedAt.HasValue)
{
    _logger.LogWarning(
        "User {UserId} attempted to access wishlist for group {GroupId} before draw completion",
        userId, query.GroupId);
    return Result<GetMyWishlistResponse>.Failure("DrawNotCompleted",
        "Wishlist can only be viewed after the draw has been completed");
}
```

---

### Error Mapping to HTTP Status Codes

**Handled by ProblemDetailsExtensions.ToProblem()**:

```csharp
// In endpoint:
if (result.IsSuccess)
    return Results.Ok(result.Value);

return result.ToProblem(); // Maps error codes to HTTP status codes
```

**Mapping Logic** (existing in codebase):
- `GroupNotFound` → 404 Not Found
- `NotParticipant` → 403 Forbidden
- Other errors → 400 Bad Request (default)

---

### Logging Strategy

**Log Levels**:
- **Warning**: Authorization failures (user not participant, group not found)
- **Information**: Successful operations (optional, may be verbose for simple GET)
- **Error**: Unexpected exceptions (handled by GlobalExceptionHandler)

**Example Logs**:
```csharp
_logger.LogWarning(
    "User {UserId} attempted to access wishlist for non-existent group {GroupId}",
    userId, query.GroupId);

_logger.LogWarning(
    "User {UserId} attempted to access wishlist for group {GroupId} but is not a participant",
    userId, query.GroupId);
```

---

## 8. Performance Considerations

### Query Optimization

1. **AsNoTracking()**: Read-only query with no change tracking overhead
   - Reduces memory usage
   - Improves query performance (~15-20% for simple queries)

2. **Filtered Include()**: Load only current user's participant record
   - Reduces data transfer from database
   - Avoids loading all participants (could be 5-30+ records)

3. **Index Usage**: Query uses existing indexes
   - Primary key index on `Groups.Id` for group lookup
   - Composite primary key index on `GroupParticipants.(GroupId, UserId)` for participant lookup

---

### Potential Bottlenecks

**None expected** for this simple read operation:
- Single database query with indexed lookups
- Small result set (1 group + 1 participant record)
- No complex business logic or external service calls

---

### Caching Considerations (Future Enhancement)

**Not implemented in MVP**, but could be added:
- Cache user's wishlist data with 5-minute expiration
- Cache key: `wishlist:{userId}:{groupId}`
- Invalidate cache on wishlist update
- Would reduce database load for repeated views

---

## 9. Implementation Steps

### Step 1: Create Response DTO

**File**: `SantaVibe.Api/Features/Wishlists/GetMyWishlist/GetMyWishlistResponse.cs`

```csharp
namespace SantaVibe.Api.Features.Wishlists.GetMyWishlist;

public class GetMyWishlistResponse
{
    public required Guid GroupId { get; init; }
    public string? WishlistContent { get; init; }
    public DateTimeOffset? LastModified { get; init; }
}
```

**Notes**:
- `required` keyword ensures `GroupId` is always set
- Nullable properties for `WishlistContent` and `LastModified` (empty wishlist scenario)
- Use `init` for immutability

---

### Step 2: Create Query Model

**File**: `SantaVibe.Api/Features/Wishlists/GetMyWishlist/GetMyWishlistQuery.cs`

```csharp
using MediatR;
using SantaVibe.Api.Common;

namespace SantaVibe.Api.Features.Wishlists.GetMyWishlist;

public record GetMyWishlistQuery(
    Guid GroupId
) : IRequest<Result<GetMyWishlistResponse>>;
```

**Notes**:
- Use `record` for immutability
- Implements `IRequest<Result<GetMyWishlistResponse>>` for MediatR
- No need for `ITransactionalCommand` (read-only query)

---

### Step 3: Create Query Handler

**File**: `SantaVibe.Api/Features/Wishlists/GetMyWishlist/GetMyWishlistQueryHandler.cs`

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using SantaVibe.Api.Common;
using SantaVibe.Api.Data;
using SantaVibe.Api.Services;

namespace SantaVibe.Api.Features.Wishlists.GetMyWishlist;

public class GetMyWishlistQueryHandler : IRequestHandler<GetMyWishlistQuery, Result<GetMyWishlistResponse>>
{
    private readonly ApplicationDbContext _context;
    private readonly IUserAccessor _userAccessor;
    private readonly ILogger<GetMyWishlistQueryHandler> _logger;

    public GetMyWishlistQueryHandler(
        ApplicationDbContext context,
        IUserAccessor userAccessor,
        ILogger<GetMyWishlistQueryHandler> logger)
    {
        _context = context;
        _userAccessor = userAccessor;
        _logger = logger;
    }

    public async Task<Result<GetMyWishlistResponse>> Handle(
        GetMyWishlistQuery query,
        CancellationToken cancellationToken)
    {
        // Extract current user ID from JWT token
        var userId = _userAccessor.GetCurrentUserId();

        // Query database with filtered include (only current user's participant record)
        var group = await _context.Groups
            .AsNoTracking()
            .Include(g => g.GroupParticipants.Where(gp => gp.UserId == userId.ToString()))
            .FirstOrDefaultAsync(g => g.Id == query.GroupId, cancellationToken);

        // Validate group exists
        if (group == null)
        {
            _logger.LogWarning(
                "User {UserId} attempted to access wishlist for non-existent group {GroupId}",
                userId, query.GroupId);
            return Result<GetMyWishlistResponse>.Failure("GroupNotFound", "Group not found");
        }

        // Validate user is participant
        var participant = group.GroupParticipants.FirstOrDefault();
        if (participant == null)
        {
            _logger.LogWarning(
                "User {UserId} attempted to access wishlist for group {GroupId} but is not a participant",
                userId, query.GroupId);
            return Result<GetMyWishlistResponse>.Failure("NotParticipant", "You are not a participant in this group");
        }

        // Validate draw is completed (wishlists can only be created/viewed after draw)
        if (!group.DrawCompletedAt.HasValue)
        {
            _logger.LogWarning(
                "User {UserId} attempted to access wishlist for group {GroupId} before draw completion",
                userId, query.GroupId);
            return Result<GetMyWishlistResponse>.Failure("DrawNotCompleted",
                "Wishlist can only be viewed after the draw has been completed");
        }

        // Map to response DTO
        var response = new GetMyWishlistResponse
        {
            GroupId = group.Id,
            WishlistContent = participant.WishlistContent,
            LastModified = participant.WishlistLastModified
        };

        return Result<GetMyWishlistResponse>.Success(response);
    }
}
```

**Key Implementation Details**:
- Use `IUserAccessor.GetCurrentUserId()` to extract user ID from JWT token
- Use `AsNoTracking()` for read-only query performance
- Use filtered `Include()` to load only current user's participant record
- Return specific error codes (`GroupNotFound`, `NotParticipant`) for proper HTTP status mapping
- Log warnings for authorization failures
- Handle null values correctly (empty wishlist scenario)

---

### Step 4: Create Endpoint

**File**: `SantaVibe.Api/Features/Wishlists/GetMyWishlist/GetMyWishlistEndpoint.cs`

```csharp
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace SantaVibe.Api.Features.Wishlists.GetMyWishlist;

public static class GetMyWishlistEndpoint
{
    public static IEndpointRouteBuilder MapGetMyWishlistEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/groups/{groupId}/participants/me/wishlist", async (
                [FromRoute] Guid groupId,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var query = new GetMyWishlistQuery(groupId);
                var result = await sender.Send(query, cancellationToken);

                if (result.IsSuccess)
                    return Results.Ok(result.Value);

                return result.ToProblem();
            })
            .RequireAuthorization()
            .WithName("GetMyWishlist")
            .WithTags("Wishlists")
            .Produces<GetMyWishlistResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .WithOpenApi();

        return app;
    }
}
```

**Key Implementation Details**:
- Use `MapGet()` for GET request
- Extract `groupId` from route with `[FromRoute]` attribute
- Inject `ISender` (MediatR) for sending query to handler
- Inject `CancellationToken` for cancellation support
- Use `.RequireAuthorization()` to enforce JWT authentication
- Use `.WithName()`, `.WithTags()`, and `.Produces<T>()` for OpenAPI documentation
- Use `.WithOpenApi()` to include in Swagger/OpenAPI spec
- Return `Results.Ok()` on success, `result.ToProblem()` on failure

---

### Step 5: Register Endpoint in Program.cs

**File**: `SantaVibe.Api/Program.cs`

Add this line after existing endpoint registrations (around line 100-120):

```csharp
// Wishlist endpoints
app.MapUpdateWishlistEndpoint();
app.MapGetMyWishlistEndpoint(); // ← Add this line
```

**Location**: Group with other wishlist-related endpoints for organization.

---

### Step 6: Test the Implementation

#### Unit Tests (Handler)

**File**: `SantaVibe.Api.Tests/Features/Wishlists/GetMyWishlist/GetMyWishlistQueryHandlerTests.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SantaVibe.Api.Data;
using SantaVibe.Api.Features.Wishlists.GetMyWishlist;
using SantaVibe.Api.Services;
using Xunit;

namespace SantaVibe.Api.Tests.Features.Wishlists.GetMyWishlist;

public class GetMyWishlistQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenGroupExistsAndUserIsParticipant_ReturnsSuccess()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb")
            .Options;

        await using var context = new ApplicationDbContext(options);

        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        var group = new Group
        {
            Id = groupId,
            Name = "Test Group",
            OrganizerUserId = userId.ToString(),
            InvitationToken = Guid.NewGuid()
        };

        var participant = new GroupParticipant
        {
            GroupId = groupId,
            UserId = userId.ToString(),
            WishlistContent = "Test wishlist content",
            WishlistLastModified = DateTimeOffset.UtcNow
        };

        context.Groups.Add(group);
        context.GroupParticipants.Add(participant);
        await context.SaveChangesAsync();

        var userAccessor = Substitute.For<IUserAccessor>();
        userAccessor.GetCurrentUserId().Returns(userId);

        var logger = Substitute.For<ILogger<GetMyWishlistQueryHandler>>();
        var handler = new GetMyWishlistQueryHandler(context, userAccessor, logger);

        var query = new GetMyWishlistQuery(groupId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(groupId, result.Value.GroupId);
        Assert.Equal("Test wishlist content", result.Value.WishlistContent);
        Assert.NotNull(result.Value.LastModified);
    }

    [Fact]
    public async Task Handle_WhenGroupDoesNotExist_ReturnsGroupNotFoundError()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb2")
            .Options;

        await using var context = new ApplicationDbContext(options);

        var userId = Guid.NewGuid();
        var nonExistentGroupId = Guid.NewGuid();

        var userAccessor = Substitute.For<IUserAccessor>();
        userAccessor.GetCurrentUserId().Returns(userId);

        var logger = Substitute.For<ILogger<GetMyWishlistQueryHandler>>();
        var handler = new GetMyWishlistQueryHandler(context, userAccessor, logger);

        var query = new GetMyWishlistQuery(nonExistentGroupId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("GroupNotFound", result.Error);
        Assert.Equal("Group not found", result.Message);
    }

    [Fact]
    public async Task Handle_WhenUserIsNotParticipant_ReturnsNotParticipantError()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb3")
            .Options;

        await using var context = new ApplicationDbContext(options);

        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        var group = new Group
        {
            Id = groupId,
            Name = "Test Group",
            OrganizerUserId = otherUserId.ToString(),
            InvitationToken = Guid.NewGuid()
        };

        context.Groups.Add(group);
        await context.SaveChangesAsync();

        var userAccessor = Substitute.For<IUserAccessor>();
        userAccessor.GetCurrentUserId().Returns(userId);

        var logger = Substitute.For<ILogger<GetMyWishlistQueryHandler>>();
        var handler = new GetMyWishlistQueryHandler(context, userAccessor, logger);

        var query = new GetMyWishlistQuery(groupId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("NotParticipant", result.Error);
        Assert.Equal("You are not a participant in this group", result.Message);
    }

    [Fact]
    public async Task Handle_WhenWishlistIsEmpty_ReturnsSuccessWithNullValues()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb4")
            .Options;

        await using var context = new ApplicationDbContext(options);

        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        var group = new Group
        {
            Id = groupId,
            Name = "Test Group",
            OrganizerUserId = userId.ToString(),
            InvitationToken = Guid.NewGuid()
        };

        var participant = new GroupParticipant
        {
            GroupId = groupId,
            UserId = userId.ToString(),
            WishlistContent = null, // Empty wishlist
            WishlistLastModified = null
        };

        context.Groups.Add(group);
        context.GroupParticipants.Add(participant);
        await context.SaveChangesAsync();

        var userAccessor = Substitute.For<IUserAccessor>();
        userAccessor.GetCurrentUserId().Returns(userId);

        var logger = Substitute.For<ILogger<GetMyWishlistQueryHandler>>();
        var handler = new GetMyWishlistQueryHandler(context, userAccessor, logger);

        var query = new GetMyWishlistQuery(groupId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(groupId, result.Value.GroupId);
        Assert.Null(result.Value.WishlistContent);
        Assert.Null(result.Value.LastModified);
    }
}
```

---

#### Integration Tests (Endpoint)

**File**: `SantaVibe.Api.Tests/Features/Wishlists/GetMyWishlist/GetMyWishlistEndpointTests.cs`

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SantaVibe.Api.Features.Wishlists.GetMyWishlist;
using Xunit;

namespace SantaVibe.Api.Tests.Features.Wishlists.GetMyWishlist;

public class GetMyWishlistEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GetMyWishlistEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetMyWishlist_WithValidTokenAndParticipant_Returns200()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = await GetValidJwtToken(client); // Helper method to get token
        var groupId = await CreateGroupAndJoin(client, token); // Helper method

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync($"/api/groups/{groupId}/participants/me/wishlist");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<GetMyWishlistResponse>();
        Assert.NotNull(result);
        Assert.Equal(groupId, result.GroupId);
    }

    [Fact]
    public async Task GetMyWishlist_WithoutToken_Returns401()
    {
        // Arrange
        var client = _factory.CreateClient();
        var groupId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/groups/{groupId}/participants/me/wishlist");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMyWishlist_WithNonExistentGroup_Returns404()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = await GetValidJwtToken(client);
        var nonExistentGroupId = Guid.NewGuid();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync($"/api/groups/{nonExistentGroupId}/participants/me/wishlist");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMyWishlist_WhenNotParticipant_Returns403()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = await GetValidJwtToken(client);
        var groupId = await CreateGroupWithDifferentOrganizer(client); // Helper method

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync($"/api/groups/{groupId}/participants/me/wishlist");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // Helper methods for test setup
    private async Task<string> GetValidJwtToken(HttpClient client) { /* ... */ }
    private async Task<Guid> CreateGroupAndJoin(HttpClient client, string token) { /* ... */ }
    private async Task<Guid> CreateGroupWithDifferentOrganizer(HttpClient client) { /* ... */ }
}
```

---

#### Manual Testing with Swagger

1. Start the application: `dotnet run --project SantaVibe.Api`
2. Navigate to Swagger UI: `https://localhost:5001/swagger`
3. Authenticate:
   - Register a new user via `/api/auth/register`
   - Copy the JWT token from response
   - Click "Authorize" button, paste token
4. Create a group via `/api/groups` (POST)
5. Test the endpoint:
   - Use the `GET /api/groups/{groupId}/participants/me/wishlist` endpoint
   - Verify 200 OK response with null wishlist (empty by default)
6. Update wishlist via `/api/groups/{groupId}/participants/me/wishlist` (PUT)
7. Get wishlist again and verify the content is returned

---

### Step 7: Verify Documentation

1. **Check Swagger/OpenAPI spec**: Ensure endpoint appears with correct request/response schemas
2. **Verify OpenAPI annotations**: Confirm `.Produces<T>()` calls generate correct status code documentation
3. **Test with API client**: Use Postman or similar to verify endpoint behavior matches specification

---

## 10. Definition of Done

- [ ] Response DTO created (`GetMyWishlistResponse.cs`)
- [ ] Query model created (`GetMyWishlistQuery.cs`)
- [ ] Query handler implemented (`GetMyWishlistQueryHandler.cs`)
- [ ] Endpoint registered (`GetMyWishlistEndpoint.cs`)
- [ ] Endpoint registered in `Program.cs`
- [ ] Unit tests written for handler (4+ test cases)
- [ ] Integration tests written for endpoint (4+ test cases)
- [ ] All tests pass
- [ ] Endpoint appears in Swagger/OpenAPI documentation
- [ ] Manual testing completed successfully
- [ ] Code follows existing patterns and conventions
- [ ] Error handling tested for all scenarios
- [ ] Logging implemented for authorization failures
- [ ] Performance verified (query uses `AsNoTracking()` and filtered `Include()`)

---

## 11. Future Enhancements

**Not in MVP, but potential future improvements**:

1. **Response Caching**: Cache wishlist data for 5 minutes to reduce database queries
2. **ETag Support**: Implement `If-None-Match` headers for conditional requests
3. **Partial Response**: Support field selection with query parameter (e.g., `?fields=wishlistContent`)
4. **Audit Logging**: Track wishlist views in audit log table
5. **Rate Limiting**: Apply specific rate limit for wishlist endpoints (prevent abuse)

---

## 12. References

**Related Endpoints**:
- `PUT /api/groups/{groupId}/participants/me/wishlist` - Update wishlist (already implemented)
- `GET /api/groups/{groupId}/my-assignment/wishlist` - Get recipient's wishlist (future)

**Related Documentation**:
- API Plan: `docs/api-plan.md` (section 3.6)
- Backend Guidelines: `docs/backend-guidelines.md`
- Database Schema: `SantaVibe.Api/Data/Entities/GroupParticipant.cs`

**Existing Implementation References**:
- Update Wishlist Handler: `SantaVibe.Api/Features/Wishlists/UpdateWishlist/UpdateWishlistCommandHandler.cs`
- Get Group Details Handler: `SantaVibe.Api/Features/Groups/GetGroupDetails/GetGroupDetailsQueryHandler.cs`
- User Accessor: `SantaVibe.Api/Services/UserAccessor.cs`

---

**Document Version**: 1.0
**Created**: 2025-10-28
**Author**: Implementation Planning Agent
**Status**: Ready for Implementation
