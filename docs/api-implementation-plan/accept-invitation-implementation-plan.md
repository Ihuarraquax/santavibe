# API Endpoint Implementation Plan: Accept Invitation and Join Group

## 1. Endpoint Overview

**Endpoint**: `POST /api/invitations/{token}/accept`

**Purpose**: Allows an authenticated user to join a Secret Santa group using a shareable invitation link. The user can optionally provide a budget suggestion during the join process. This endpoint creates a new GroupParticipant record linking the user to the group.

**Key Features**:
- Validates invitation token existence and validity
- Prevents joining groups where draw has already been completed
- Prevents duplicate membership (user cannot join same group twice)
- Accepts optional budget suggestion from the user
- Returns updated group details with new participant count

**Authentication**: Required (JWT Bearer token)

**Architecture Pattern**: MediatR Command/Handler pattern (consistent with CreateGroup endpoint)

---

## 2. Request Details

### HTTP Method
`POST`

### URL Structure
```
POST /api/invitations/{token}/accept
```

### Path Parameters
| Parameter | Type | Required | Validation | Description |
|-----------|------|----------|------------|-------------|
| `token` | Guid | Yes | Valid UUID format | Invitation token from shareable link |

### Request Headers
```
Authorization: Bearer {jwt-token}
Content-Type: application/json
```

### Request Body
```json
{
  "budgetSuggestion": 80.00
}
```

**Request Body Schema** (`AcceptInvitationRequest`):
| Field | Type | Required | Validation | Description |
|-------|------|----------|------------|-------------|
| `budgetSuggestion` | decimal? | No | Range: 0.01 - 99999999.99 | User's suggested budget in PLN |

### Validation Rules

**Route Validation**:
- `token` must be valid Guid format (handled by ASP.NET Core model binding)

**Request Body Validation** (Data Annotations):
- `budgetSuggestion`: Optional (nullable decimal)
- If provided: Must be between 0.01 and 99999999.99
- Precision: NUMERIC(10,2)

**Business Logic Validation** (in handler):
- Invitation token must exist in database
- Group draw must not be completed (`DrawCompletedAt IS NULL`)
- User must not already be a participant in the group
- User account must be active (`IsDeleted = false`)

---

## 3. Used Types

### Request Types

**AcceptInvitationRequest.cs** (DTO):
```csharp
using System.ComponentModel.DataAnnotations;

namespace SantaVibe.Api.Features.Invitations.AcceptInvitation;

/// <summary>
/// Request DTO for accepting an invitation and joining a group
/// </summary>
public sealed record AcceptInvitationRequest
{
    /// <summary>
    /// Optional budget suggestion in PLN (FR-009)
    /// </summary>
    [Range(0.01, 99999999.99, ErrorMessage = "Budget suggestion must be between 0.01 and 99999999.99")]
    public decimal? BudgetSuggestion { get; init; }
}
```

**AcceptInvitationCommand.cs** (Command Model):
```csharp
using MediatR;
using SantaVibe.Api.Common;

namespace SantaVibe.Api.Features.Invitations.AcceptInvitation;

/// <summary>
/// Command to accept an invitation and join a group
/// </summary>
/// <param name="Token">Invitation token (UUID)</param>
/// <param name="UserId">Current authenticated user ID</param>
/// <param name="BudgetSuggestion">Optional budget suggestion in PLN</param>
public sealed record AcceptInvitationCommand(
    Guid Token,
    string UserId,
    decimal? BudgetSuggestion
) : IRequest<Result<AcceptInvitationResponse>>;
```

### Response Types

**AcceptInvitationResponse.cs** (DTO):
```csharp
namespace SantaVibe.Api.Features.Invitations.AcceptInvitation;

/// <summary>
/// Response DTO for successful invitation acceptance
/// </summary>
/// <param name="GroupId">Group unique identifier</param>
/// <param name="GroupName">Name of the joined group</param>
/// <param name="OrganizerName">Full name of group organizer</param>
/// <param name="ParticipantCount">Updated participant count after join</param>
/// <param name="Budget">Final budget in PLN (null if not set by organizer)</param>
/// <param name="DrawCompleted">Indicates if draw has been completed</param>
/// <param name="JoinedAt">Timestamp when user joined the group</param>
public sealed record AcceptInvitationResponse(
    Guid GroupId,
    string GroupName,
    string OrganizerName,
    int ParticipantCount,
    decimal? Budget,
    bool DrawCompleted,
    DateTimeOffset JoinedAt
);
```

### Handler Type

**AcceptInvitationCommandHandler.cs**:
- Implements `IRequestHandler<AcceptInvitationCommand, Result<AcceptInvitationResponse>>`
- Dependencies:
  - `ApplicationDbContext` (database access)
  - `ILogger<AcceptInvitationCommandHandler>` (logging)
- Returns `Result<AcceptInvitationResponse>`

### Endpoint Type

**AcceptInvitationEndpoint.cs**:
- Static class with `MapAcceptInvitationEndpoint` extension method
- Uses minimal API pattern
- Integrates with MediatR `ISender`
- Applies `ValidationFilter<AcceptInvitationRequest>`

---

## 4. Response Details

### Success Response (201 Created)

**Status Code**: 201 Created

**Headers**:
```
Location: /api/groups/{groupId}
Content-Type: application/json
```

**Body**:
```json
{
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "groupName": "Family Secret Santa 2025",
  "organizerName": "Jan Kowalski",
  "participantCount": 6,
  "budget": null,
  "drawCompleted": false,
  "joinedAt": "2025-10-15T13:00:00Z"
}
```

### Error Responses

#### 400 Bad Request - Validation Error
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Validation Error",
  "status": 400,
  "detail": "Budget suggestion must be between 0.01 and 99999999.99",
  "errors": {
    "budgetSuggestion": [
      "Budget suggestion must be between 0.01 and 99999999.99"
    ]
  }
}
```

#### 401 Unauthorized - Missing/Invalid JWT
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401
}
```

#### 404 Not Found - Invalid Invitation Token
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "InvalidInvitation",
  "status": 404,
  "detail": "This invitation link is invalid or has expired"
}
```

#### 409 Conflict - Already Participant
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.8",
  "title": "AlreadyParticipant",
  "status": 409,
  "detail": "You are already a participant in this group"
}
```

#### 410 Gone - Draw Already Completed
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.9",
  "title": "InvitationExpired",
  "status": 410,
  "detail": "This group has already completed the draw and is no longer accepting participants"
}
```

#### 500 Internal Server Error
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "InternalServerError",
  "status": 500,
  "detail": "An unexpected error occurred while joining the group"
}
```

---

## 5. Data Flow

### Request Flow
1. **HTTP Request Received**
   - Client sends POST to `/api/invitations/{token}/accept` with optional budgetSuggestion
   - JWT Bearer token in Authorization header

2. **ASP.NET Core Pipeline**
   - Route parameter binding: Extract `token` as Guid
   - JWT authentication middleware: Validate token, extract userId claim
   - Model binding: Deserialize request body to `AcceptInvitationRequest`

3. **Validation Layer**
   - `ValidationFilter<AcceptInvitationRequest>` executes
   - Validates budgetSuggestion range if provided
   - Returns 400 Bad Request if validation fails

4. **Endpoint Handler**
   - Extract userId from JWT claims via `IUserAccessor`
   - Create `AcceptInvitationCommand` with token, userId, budgetSuggestion
   - Send command to MediatR `ISender`

5. **Command Handler Execution**
   - Begin database transaction with execution strategy
   - Query Groups table for matching InvitationToken
   - Eager load Organizer and GroupParticipants
   - Validate business rules:
     - Token exists → else 404
     - Draw not completed → else 410
     - User not already participant → else 409
   - Create new `GroupParticipant` record
   - Save changes to database
   - Commit transaction
   - Build and return response DTO

6. **Response Mapping**
   - Handler returns `Result<AcceptInvitationResponse>`
   - Endpoint maps Result to IResult:
     - Success → 201 Created with Location header
     - Failure → ProblemDetails with appropriate status code

### Database Interaction Flow

**Read Operations**:
```csharp
var group = await _context.Groups
    .Include(g => g.Organizer)
    .Include(g => g.GroupParticipants)
    .FirstOrDefaultAsync(g => g.InvitationToken == token);
```

**Write Operations**:
```csharp
var participant = new GroupParticipant
{
    GroupId = group.Id,
    UserId = userId,
    BudgetSuggestion = request.BudgetSuggestion,
    JoinedAt = DateTimeOffset.UtcNow,
    WishlistContent = null,
    WishlistLastModified = null
};

_context.GroupParticipants.Add(participant);
await _context.SaveChangesAsync(cancellationToken);
```

**Transaction Scope**:
- Use `ExecutionStrategy` for retry logic (transient error handling)
- Wrap in explicit transaction for atomicity
- Rollback on any exception

---

## 6. Security Considerations

### Authentication
- **Requirement**: JWT Bearer token required in Authorization header
- **Implementation**: `.RequireAuthorization()` on endpoint
- **Claims**: Extract userId from `ClaimTypes.NameIdentifier` via `IUserAccessor`
- **Validation**: User account must be active (`IsDeleted = false`)

### Authorization
- **User Scope**: Users can only join groups on their own behalf
- **No Privilege Escalation**: UserId from JWT claim cannot be manipulated
- **Token Privacy**: Invitation tokens are UUIDs (hard to guess, not sequential)

### Input Validation
- **Route Parameter**: ASP.NET Core validates Guid format
- **Request Body**: Data Annotations validate decimal range
- **SQL Injection**: EF Core uses parameterized queries (protected)
- **XSS**: Not applicable (no HTML rendering in API)

### Business Logic Security
- **Prevent Double Join**: Check GroupParticipants for existing membership
- **Prevent Post-Draw Join**: Validate `DrawCompletedAt IS NULL`
- **Account Status**: Verify `IsDeleted = false` to prevent deleted users from joining

### Data Privacy
- **Response Filtering**: Only return data user is authorized to see
- **Logging**: Log userId for audit trail, avoid logging sensitive data
- **Error Messages**: Generic messages to avoid information disclosure

---

## 7. Error Handling

### Error Scenarios Matrix

| Error Type | Condition | Status Code | Error Code | Handler Location | Logging Level |
|------------|-----------|-------------|------------|------------------|---------------|
| Invalid token format | Token not valid Guid | 400 | ValidationError | ASP.NET Core model binding | Warning |
| Missing JWT | No Authorization header | 401 | Unauthorized | Authentication middleware | Warning |
| Invalid JWT | Token expired/invalid | 401 | Unauthorized | Authentication middleware | Warning |
| User deleted/inactive | IsDeleted = true | 401 | Unauthorized | Command handler | Warning |
| Token not found | No matching InvitationToken | 404 | InvalidInvitation | Command handler | Warning |
| Already participant | UserId exists in GroupParticipants | 409 | AlreadyParticipant | Command handler | Warning |
| Draw completed | DrawCompletedAt NOT NULL | 410 | InvitationExpired | Command handler | Warning |
| Budget out of range | Value < 0.01 or > 99999999.99 | 400 | ValidationError | ValidationFilter | Warning |
| Database error | EF Core exception | 500 | InternalServerError | Command handler | Error |
| Unexpected error | Any other exception | 500 | InternalServerError | Command handler | Error |

### Error Handling Strategy

**Validation Errors (400)**:
- Return ProblemDetails with field-level errors
- Use ValidationFilter for Data Annotations
- Include helpful error messages

**Authentication Errors (401)**:
- Handled by ASP.NET Core authentication middleware
- Generic message to avoid information disclosure

**Not Found (404)**:
- Check if group with InvitationToken exists
- Log warning with token for debugging
- Return generic "invalid or expired" message

**Conflict (409)**:
- Query GroupParticipants for (GroupId, UserId) combination
- Return specific error code: "AlreadyParticipant"

**Gone (410)**:
- Check if DrawCompletedAt is NOT NULL
- Return specific error code: "InvitationExpired"

**Server Errors (500)**:
- Catch all exceptions in handler
- Log error with full context (userId, token, exception)
- Return generic message to user
- Roll back transaction

### Logging Strategy

**Information Level**:
- Successful invitation acceptance
- Log: userId, groupId, groupName, participantCount

**Warning Level**:
- Business rule violations (already participant, draw completed, token not found)
- Log: userId, token, violation type

**Error Level**:
- Database errors
- Unexpected exceptions
- Log: userId, token, exception details, stack trace

---

## 8. Performance Considerations

### Database Optimization

**Query Optimization**:
- Use `Include()` for eager loading to avoid N+1 queries:
  - `Include(g => g.Organizer)` - for OrganizerName
  - `Include(g => g.GroupParticipants)` - for participant count and duplicate check
- Use single query to fetch all required data

**Transaction Management**:
- Use explicit transaction for atomicity
- Use `ExecutionStrategy` for transient error retry
- Keep transaction scope minimal (only write operations)

**Indexing**:
- Groups.InvitationToken should have unique index (already defined in schema)
- GroupParticipants (GroupId, UserId) composite primary key ensures fast lookups

### Potential Bottlenecks

**Database Contention**:
- Multiple users joining same group simultaneously
- Mitigation: Transactions ensure consistency, retries handle contention

**Large Participant Lists**:
- Eager loading GroupParticipants could be expensive for large groups
- MVP assumption: Groups have 5-30 participants (acceptable)
- Future: Consider projection instead of full entity loading

### Optimization Strategies

**Read Optimization**:
- Consider using `.AsNoTracking()` for initial group lookup if not modifying group entity
- Use `.Select()` projection to only fetch required fields for organizer name

**Write Optimization**:
- Single `SaveChangesAsync()` call for participant insert
- Avoid multiple round-trips to database

**Caching Considerations**:
- Not applicable for MVP (write operation, data changes frequently)
- Future: Cache group metadata for read-heavy scenarios

---

## 9. Implementation Steps

### Step 1: Create Request and Response DTOs
**File**: `SantaVibe.Api/Features/Invitations/AcceptInvitation/AcceptInvitationRequest.cs`
- Define record with optional `BudgetSuggestion` property
- Add `[Range(0.01, 99999999.99)]` validation attribute

**File**: `SantaVibe.Api/Features/Invitations/AcceptInvitation/AcceptInvitationResponse.cs`
- Define record with all response properties matching API spec
- Use appropriate types (Guid, string, int, decimal?, bool, DateTimeOffset)

### Step 2: Create Command Model
**File**: `SantaVibe.Api/Features/Invitations/AcceptInvitation/AcceptInvitationCommand.cs`
- Define record implementing `IRequest<Result<AcceptInvitationResponse>>`
- Include Token (Guid), UserId (string), BudgetSuggestion (decimal?)

### Step 3: Implement Command Handler
**File**: `SantaVibe.Api/Features/Invitations/AcceptInvitation/AcceptInvitationCommandHandler.cs`

**Constructor Dependencies**:
```csharp
public AcceptInvitationCommandHandler(
    ApplicationDbContext context,
    ILogger<AcceptInvitationCommandHandler> logger)
```

**Handler Logic**:
1. Get execution strategy: `_context.Database.CreateExecutionStrategy()`
2. Execute within strategy for retry logic
3. Begin transaction: `await _context.Database.BeginTransactionAsync()`
4. Query group with includes:
   ```csharp
   var group = await _context.Groups
       .Include(g => g.Organizer)
       .Include(g => g.GroupParticipants)
       .FirstOrDefaultAsync(g => g.InvitationToken == command.Token);
   ```
5. Validate token exists:
   - If null, log warning and return 404 failure
6. Validate draw not completed:
   - If `DrawCompletedAt` has value, log warning and return 410 failure
7. Check if user already participant:
   ```csharp
   if (group.GroupParticipants.Any(gp => gp.UserId == command.UserId))
   {
       return Failure with 409 error code
   }
   ```
8. Create GroupParticipant entity:
   ```csharp
   var participant = new GroupParticipant
   {
       GroupId = group.Id,
       UserId = command.UserId,
       BudgetSuggestion = command.BudgetSuggestion,
       JoinedAt = DateTimeOffset.UtcNow
   };
   ```
9. Add to context: `_context.GroupParticipants.Add(participant)`
10. Save changes: `await _context.SaveChangesAsync()`
11. Commit transaction: `await transaction.CommitAsync()`
12. Build response DTO with updated participant count
13. Return success result
14. Catch exceptions, rollback, log error, return 500 failure

### Step 4: Create Endpoint Definition
**File**: `SantaVibe.Api/Features/Invitations/AcceptInvitation/AcceptInvitationEndpoint.cs`

**Extension Method**:
```csharp
public static void MapAcceptInvitationEndpoint(this IEndpointRouteBuilder app)
{
    app.MapPost("/api/invitations/{token:guid}/accept", async (
            [FromRoute] Guid token,
            [FromBody] AcceptInvitationRequest request,
            ISender sender,
            IUserAccessor userAccessor,
            CancellationToken cancellationToken) =>
        {
            var userId = userAccessor.GetCurrentUserId();
            var command = new AcceptInvitationCommand(
                token,
                userId.ToString(),
                request.BudgetSuggestion
            );

            var result = await sender.Send(command, cancellationToken);

            if (result.IsSuccess)
            {
                return Results.Created(
                    $"/api/groups/{result.Value!.GroupId}",
                    result.Value
                );
            }

            return result.ToProblem();
        })
        .RequireAuthorization()
        .WithName("AcceptInvitation")
        .WithTags("Invitations")
        .Produces<AcceptInvitationResponse>(StatusCodes.Status201Created)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
        .Produces<ProblemDetails>(StatusCodes.Status410Gone)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
        .AddEndpointFilter<ValidationFilter<AcceptInvitationRequest>>();
}
```

### Step 5: Register Endpoint in Program.cs
**File**: `SantaVibe.Api/Program.cs`
- Add endpoint registration in endpoint mapping section:
```csharp
app.MapAcceptInvitationEndpoint();
```

### Step 6: Add Result Extension Method for Error Mapping
**File**: `SantaVibe.Api/Common/ResultExtensions.cs` (or add to existing Result.cs)

Create `ToProblem()` extension method if not already exists:
```csharp
public static IResult ToProblem<T>(this Result<T> result)
{
    if (result.IsSuccess)
        throw new InvalidOperationException("Cannot convert success result to problem");

    var statusCode = result.Error switch
    {
        "Unauthorized" => StatusCodes.Status401Unauthorized,
        "InvalidInvitation" => StatusCodes.Status404NotFound,
        "AlreadyParticipant" => StatusCodes.Status409Conflict,
        "InvitationExpired" => StatusCodes.Status410Gone,
        "ValidationError" => StatusCodes.Status400BadRequest,
        "InternalServerError" => StatusCodes.Status500InternalServerError,
        _ => StatusCodes.Status500InternalServerError
    };

    var problemDetails = new ProblemDetails
    {
        Title = result.Error,
        Detail = result.Message,
        Status = statusCode
    };

    if (result.ValidationErrors != null)
    {
        problemDetails.Extensions["errors"] = result.ValidationErrors;
    }

    return Results.Problem(problemDetails);
}
```

### Step 7: Register Handler with MediatR
**File**: `SantaVibe.Api/Program.cs`
- Ensure MediatR is scanning the Invitations assembly for handlers
- Should be covered by existing MediatR registration: `services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()))`

### Step 8: Write Unit Tests
**File**: `SantaVibe.Tests/Features/Invitations/AcceptInvitation/AcceptInvitationIntegrationTests.cs`

**Test Cases**:
1. `AcceptInvitation_WithValidToken_ReturnsCreated`
   - Setup: Create group with invitation token
   - Act: Send request with valid token
   - Assert: 201 Created, participant added, count incremented

2. `AcceptInvitation_WithBudgetSuggestion_StoresBudget`
   - Setup: Create group, send request with budget
   - Assert: Budget stored in GroupParticipants table

3. `AcceptInvitation_WithInvalidToken_ReturnsNotFound`
   - Act: Send request with random Guid
   - Assert: 404 Not Found, error code "InvalidInvitation"

4. `AcceptInvitation_WhenDrawCompleted_ReturnsGone`
   - Setup: Create group, set DrawCompletedAt
   - Assert: 410 Gone, error code "InvitationExpired"

5. `AcceptInvitation_WhenAlreadyParticipant_ReturnsConflict`
   - Setup: User joins group, tries to join again
   - Assert: 409 Conflict, error code "AlreadyParticipant"

6. `AcceptInvitation_WithInvalidBudget_ReturnsBadRequest`
   - Act: Send request with budget = 0 or > 99999999.99
   - Assert: 400 Bad Request, validation error

7. `AcceptInvitation_WithoutAuth_ReturnsUnauthorized`
   - Act: Send request without JWT token
   - Assert: 401 Unauthorized

### Step 9: Manual Testing with Swagger
- Run application, navigate to `/swagger`
- Test endpoint with various scenarios:
  - Valid invitation token
  - Invalid token
  - Already participant
  - Draw completed
  - Budget validation edge cases

### Step 10: Update API Documentation
**File**: `docs/api-plan.md`
- Verify implementation matches specification
- Update any deviations or clarifications discovered during implementation

---

## 10. Testing Checklist

### Unit Test Coverage
- [ ] Valid invitation acceptance with budget suggestion
- [ ] Valid invitation acceptance without budget suggestion
- [ ] Invalid invitation token returns 404
- [ ] Draw already completed returns 410
- [ ] User already participant returns 409
- [ ] Budget out of range returns 400
- [ ] Missing authentication returns 401
- [ ] Successful transaction commit
- [ ] Transaction rollback on error

### Integration Test Coverage
- [ ] End-to-end flow with database
- [ ] Concurrent join attempts (race condition)
- [ ] Participant count incremented correctly
- [ ] Budget suggestion stored correctly
- [ ] JoinedAt timestamp recorded

### Manual Test Scenarios
- [ ] Test with Swagger UI
- [ ] Test with real JWT token
- [ ] Test with expired JWT token
- [ ] Test with Postman/curl
- [ ] Verify Location header in 201 response

### Edge Cases
- [ ] Budget = 0.01 (minimum valid)
- [ ] Budget = 99999999.99 (maximum valid)
- [ ] Budget = 0 (invalid)
- [ ] Budget = null (valid, optional)
- [ ] Token = empty Guid (00000000-0000-0000-0000-000000000000)
- [ ] Very large group (30+ participants)

---

## 11. Deployment Checklist

### Pre-Deployment
- [ ] All tests passing (unit + integration)
- [ ] Code review completed
- [ ] Database migrations applied (if any schema changes)
- [ ] Swagger documentation generated and reviewed
- [ ] Performance testing completed for expected load

### Monitoring
- [ ] Log aggregation configured (Serilog)
- [ ] Error tracking enabled
- [ ] Monitor 409 conflict rate (may indicate UX issues)
- [ ] Monitor 410 gone rate (users trying to join completed draws)

### Rollback Plan
- [ ] Endpoint can be disabled via feature flag if needed
- [ ] Database changes are backward compatible
- [ ] No breaking changes to existing endpoints

---

## 12. Future Enhancements

### Potential Improvements
1. **Email Notification**: Send welcome email when user joins group
2. **Real-time Updates**: Notify organizer when new participant joins (WebSocket/SignalR)
3. **Invitation Limits**: Limit number of participants per group
4. **Invitation Expiry**: Add expiration date to invitation tokens
5. **Invite Tracking**: Track who invited each participant
6. **Budget Recommendations**: Show average/median budget suggestions to new joiners

### API Evolution
- Consider adding `PATCH /api/groups/{groupId}/participants/me` to update budget suggestion later
- Consider adding `GET /api/groups/{groupId}/invitation` to regenerate/view invitation link (organizer only)
