# API Endpoint Implementation Plan: Get My Budget Suggestion

## 1. Endpoint Overview

The `GET /api/groups/{groupId}/participants/me/budget-suggestion` endpoint retrieves the authenticated user's budget suggestion for a specific Secret Santa group. This is a read-only operation that returns the user's budget suggestion (if set) or null values if no suggestion has been submitted. The endpoint is available both before and after the draw has been completed, allowing participants to view their budget suggestion at any time.

## 2. Request Details

- **HTTP Method:** GET
- **URL Structure:** `/api/groups/{groupId}/participants/me/budget-suggestion`
- **Parameters:**
  - **Required Path Parameters:**
    - `groupId` (Guid): Unique identifier for the group
  - **Optional Parameters:** None
- **Request Body:** None (GET endpoint)
- **Authentication:** Required - JWT Bearer token in Authorization header

## 3. Used Types

### Query Model
```csharp
// GetMyBudgetSuggestionQuery.cs
public record GetMyBudgetSuggestionQuery(
    Guid GroupId
) : IRequest<Result<GetMyBudgetSuggestionResponse>>;
```

### Response DTO
```csharp
// GetMyBudgetSuggestionResponse.cs
public record GetMyBudgetSuggestionResponse
{
    public required Guid GroupId { get; init; }
    public decimal? BudgetSuggestion { get; init; }
    public DateTimeOffset? SubmittedAt { get; init; }
}
```

### Database Entities (Existing)
- `GroupParticipant` - Contains `BudgetSuggestion` and `JoinedAt` fields
- `Group` - Referenced to verify existence

## 4. Response Details

### Success Response (200 OK) - With Budget Suggestion
```json
{
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "budgetSuggestion": 80.00,
  "submittedAt": "2025-10-15T13:00:00Z"
}
```

### Success Response (200 OK) - No Budget Suggestion
```json
{
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "budgetSuggestion": null,
  "submittedAt": null
}
```

### Error Responses

| Status Code | Error Code | Description |
|------------|------------|-------------|
| 401 Unauthorized | N/A | Missing or invalid JWT token (handled by middleware) |
| 403 Forbidden | NotParticipant | User is not a participant in the group |
| 404 Not Found | GroupNotFound | Group does not exist |
| 500 Internal Server Error | InternalServerError | Unexpected database or server error |

## 5. Data Flow

1. **Authentication:** ASP.NET Core authorization middleware validates JWT token
2. **Extract User ID:** Use `IUserAccessor` to get authenticated user's ID from JWT claims
3. **Query Database:** Query `GroupParticipants` table with:
   - `GroupId = {groupId}` AND `UserId = {authenticatedUserId}`
4. **Authorization Check:** If no participant record found:
   - Check if group exists → return 404 if not
   - Otherwise return 403 (not a participant)
5. **Map Response:** Map `GroupParticipant` to response DTO:
   - `GroupId` → from path parameter
   - `BudgetSuggestion` → from `participant.BudgetSuggestion`
   - `SubmittedAt` → from `participant.JoinedAt` (when budget was first set/updated)
6. **Return Result:** Return 200 OK with response

### Database Query Pattern
```csharp
var participant = await context.GroupParticipants
    .FirstOrDefaultAsync(gp =>
        gp.GroupId == query.GroupId &&
        gp.UserId == userId,
        cancellationToken);
```

**Note:** No need to include `Group` navigation property unless we need to check draw completion (which we don't for this endpoint per API spec).

## 6. Security Considerations

### Authentication
- Endpoint requires JWT Bearer token authentication
- Token validated by ASP.NET Core authorization middleware
- User ID extracted from validated JWT claims via `IUserAccessor`

### Authorization
- User must be a participant in the requested group
- Participants can only view their own budget suggestion (not others')
- No organizer-only restriction - any participant can view their own suggestion

### Data Privacy
- Endpoint returns only the authenticated user's budget suggestion
- No exposure of other participants' budget suggestions
- Anonymous budget list viewing is a separate organizer-only endpoint

### Input Validation
- `groupId` path parameter validated as Guid by routing
- No additional input validation needed (GET endpoint with no query params)

## 7. Error Handling

### Error Scenarios

| Scenario | Handler Logic | Error Code | HTTP Status | Log Level |
|----------|--------------|------------|-------------|-----------|
| Group not found | Check `Groups` table if participant not found | GroupNotFound | 404 | Warning |
| User not a participant | Return 403 if participant record doesn't exist | NotParticipant | 403 | Warning |
| Database query exception | Catch `DbException` in handler | InternalServerError | 500 | Error |
| Missing JWT | Handled by authorization middleware | N/A | 401 | N/A |

### Error Response Format
```json
{
  "error": "ErrorCode",
  "message": "User-friendly error message"
}
```

### Logging Strategy
- **Warning logs:** Group not found, user not a participant (authorization failures)
- **Error logs:** Database exceptions, unexpected errors
- **Information logs:** Not needed for simple read operations (would be too verbose)
- Use structured logging with `groupId` and `userId` for troubleshooting

## 8. Performance Considerations

### Database Optimization
- Single database query to retrieve participant record
- No need for eager loading (`Include()`) since we only need `GroupParticipant` fields
- Only query `Groups` table if participant not found (to distinguish 404 vs 403)
- Use indexed composite key lookup (`GroupId`, `UserId`) - primary key on `GroupParticipants`

### Potential Bottlenecks
- None expected - simple single-record lookup by primary key
- Query is highly efficient due to composite primary key index

### Caching Opportunities (Future Enhancement)
- Could cache budget suggestion in distributed cache for frequently accessed groups
- Cache invalidation needed when budget suggestion is updated
- Not necessary for MVP given simplicity of query

## 9. Implementation Steps

### Step 1: Create Query Model
**File:** `SantaVibe.Api/Features/Groups/GetMyBudgetSuggestion/GetMyBudgetSuggestionQuery.cs`

```csharp
using MediatR;
using SantaVibe.Api.Common;

namespace SantaVibe.Api.Features.Groups.GetMyBudgetSuggestion;

public record GetMyBudgetSuggestionQuery(
    Guid GroupId
) : IRequest<Result<GetMyBudgetSuggestionResponse>>;
```

### Step 2: Create Response DTO
**File:** `SantaVibe.Api/Features/Groups/GetMyBudgetSuggestion/GetMyBudgetSuggestionResponse.cs`

```csharp
namespace SantaVibe.Api.Features.Groups.GetMyBudgetSuggestion;

public record GetMyBudgetSuggestionResponse
{
    public required Guid GroupId { get; init; }
    public decimal? BudgetSuggestion { get; init; }
    public DateTimeOffset? SubmittedAt { get; init; }
}
```

### Step 3: Create Query Handler
**File:** `SantaVibe.Api/Features/Groups/GetMyBudgetSuggestion/GetMyBudgetSuggestionQueryHandler.cs`

**Implementation:**
1. Inject `ApplicationDbContext`, `IUserAccessor`, and `ILogger<GetMyBudgetSuggestionQueryHandler>`
2. Get authenticated user ID from `IUserAccessor`
3. Query `GroupParticipants` for matching record:
   - Filter by `GroupId` and `UserId`
   - Use `FirstOrDefaultAsync()`
4. If participant not found:
   - Query `Groups` to check if group exists
   - Return `GroupNotFound` (404) if group doesn't exist
   - Return `NotParticipant` (403) if group exists but user isn't a participant
5. Map participant data to response:
   - `GroupId` from query parameter
   - `BudgetSuggestion` from `participant.BudgetSuggestion`
   - `SubmittedAt` from `participant.JoinedAt` (or null if budget not set)
6. Return success result with response DTO
7. Wrap database operations in try-catch for `DbException`
8. Log warnings for authorization failures, errors for exceptions

### Step 4: Create Endpoint
**File:** `SantaVibe.Api/Features/Groups/GetMyBudgetSuggestion/GetMyBudgetSuggestionEndpoint.cs`

**Implementation:**
1. Map GET endpoint at `/api/groups/{groupId}/participants/me/budget-suggestion`
2. Extract `groupId` from route parameters
3. Create `GetMyBudgetSuggestionQuery` with `groupId`
4. Send query using MediatR `ISender`
5. Return appropriate HTTP result:
   - 200 OK with response DTO on success
   - Problem details for errors using `result.ToProblem()`
6. Configure endpoint metadata:
   - `.RequireAuthorization()` - JWT required
   - `.WithTags("Budget")` - Swagger grouping
   - `.WithName("GetMyBudgetSuggestion")` - Route name
   - `.Produces<GetMyBudgetSuggestionResponse>(200)`
   - `.Produces<ProblemDetails>(401, 403, 404, 500)`

### Step 5: Register Endpoint in Program.cs
**File:** `SantaVibe.Api/Program.cs`

Add endpoint registration:
```csharp
app.MapGetMyBudgetSuggestionEndpoint();
```

### Step 6: Write Unit Tests (Optional for MVP)
**File:** `SantaVibe.Tests/Features/Groups/GetMyBudgetSuggestion/GetMyBudgetSuggestionQueryHandlerTests.cs`

Test cases:
- Returns budget suggestion when participant has set one
- Returns null values when participant hasn't set budget suggestion
- Returns 404 when group doesn't exist
- Returns 403 when user is not a participant
- Handles database exceptions gracefully

### Step 7: Write Integration Tests
**File:** `SantaVibe.Tests/Features/Groups/GetMyBudgetSuggestion/GetMyBudgetSuggestionIntegrationTests.cs`

Test scenarios:
1. **Success - With Budget Suggestion:**
   - Arrange: Create group, add participant with budget suggestion
   - Act: GET request as participant
   - Assert: 200 OK, correct budget and submitted timestamp

2. **Success - No Budget Suggestion:**
   - Arrange: Create group, add participant without budget suggestion
   - Act: GET request as participant
   - Assert: 200 OK, null budget and submitted timestamp

3. **Unauthorized - Missing JWT:**
   - Act: GET request without authorization header
   - Assert: 401 Unauthorized

4. **Forbidden - Not a Participant:**
   - Arrange: Create group with different participant
   - Act: GET request as non-participant
   - Assert: 403 Forbidden

5. **Not Found - Group Doesn't Exist:**
   - Act: GET request with non-existent groupId
   - Assert: 404 Not Found

6. **Available After Draw:**
   - Arrange: Create group, add participant, execute draw
   - Act: GET request as participant
   - Assert: 200 OK (confirm endpoint works post-draw)

### Step 8: Update API Documentation
- Ensure Swagger/OpenAPI documentation auto-generates correctly
- Verify endpoint appears in Swagger UI under "Budget" tag
- Confirm request/response schemas are accurate

### Step 9: Manual Testing Checklist
- [ ] Test with valid participant - budget set
- [ ] Test with valid participant - budget not set
- [ ] Test with invalid groupId (404)
- [ ] Test as non-participant (403)
- [ ] Test without JWT token (401)
- [ ] Test before draw completion
- [ ] Test after draw completion
- [ ] Verify Swagger documentation
- [ ] Verify response timestamps in ISO 8601 UTC format

## 10. Implementation Notes

### Timestamp Handling
- API spec shows `submittedAt` field
- `GroupParticipant` entity has `JoinedAt` but no separate "BudgetSuggestionUpdatedAt"
- **Decision:** Return `JoinedAt` as `submittedAt` when budget suggestion exists, null otherwise
- **Future Enhancement:** Add `BudgetSuggestionUpdatedAt` field to track updates separately

### Business Logic Differences from Update Endpoint
- **No draw completion check:** Unlike `UpdateBudgetSuggestion`, this endpoint is available before AND after draw
- **Read-only:** No validation of budget values needed
- **No database writes:** No transaction needed

### Code Consistency
- Follow pattern from `UpdateBudgetSuggestionCommandHandler` for:
  - User ID extraction
  - Participant query structure
  - Authorization checks (group exists vs not a participant)
  - Error handling and logging
  - Result pattern usage
