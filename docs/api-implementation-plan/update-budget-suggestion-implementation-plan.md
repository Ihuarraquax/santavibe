# API Endpoint Implementation Plan: Update My Budget Suggestion

## 1. Endpoint Overview

**Endpoint**: `PUT /api/groups/{groupId}/participants/me/budget-suggestion`

**Purpose**: Update or set the authenticated user's budget suggestion for a specific group. This endpoint allows participants to propose their preferred budget before the Secret Santa draw is executed. Budget suggestions are stored anonymously and can be viewed only by the organizer to help determine the final budget.

**Key Features**:
- Participants can set, update, or clear their budget suggestion
- Only available before draw completion
- Budget suggestions are displayed anonymously to organizers
- Validates budget range and group participation

**Use Cases**:
- Participant sets initial budget suggestion when joining a group
- Participant updates their budget suggestion after discussion with organizer
- Participant clears their budget suggestion (by setting to null)

---

## 2. Request Details

### HTTP Method
`PUT`

### URL Structure
```
PUT /api/groups/{groupId}/participants/me/budget-suggestion
```

### Path Parameters
| Parameter | Type | Required | Validation | Description |
|-----------|------|----------|------------|-------------|
| `groupId` | UUID | Yes | Valid UUID format | Unique identifier of the group |

### Request Headers
| Header | Value | Required | Description |
|--------|-------|----------|-------------|
| `Authorization` | `Bearer {jwt_token}` | Yes | JWT authentication token |
| `Content-Type` | `application/json` | Yes | Request body format |

### Request Body

**Content-Type**: `application/json`

**Schema**:
```json
{
  "budgetSuggestion": 120.00
}
```

**Field Specifications**:
| Field | Type | Required | Validation | Description |
|-------|------|----------|------------|-------------|
| `budgetSuggestion` | decimal | Yes | Min: 0.01, Max: 99999999.99, Precision: 2 decimal places | Budget amount in PLN |

**Business Rules**:
- Value can be `null` to clear the suggestion
- If provided, must be between 0.01 and 99999999.99
- Stored as NUMERIC(10,2) in database
- Draw must not be completed

### Example Requests

**Set budget suggestion**:
```http
PUT /api/groups/7c9e6679-7425-40de-944b-e07fc1f90ae7/participants/me/budget-suggestion
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
  "budgetSuggestion": 120.00
}
```

**Clear budget suggestion**:
```http
PUT /api/groups/7c9e6679-7425-40de-944b-e07fc1f90ae7/participants/me/budget-suggestion
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
  "budgetSuggestion": null
}
```

---

## 3. Used Types

### Request Types

**UpdateBudgetSuggestionRequest** (HTTP DTO):
```csharp
namespace SantaVibe.Api.Features.Groups.UpdateBudgetSuggestion;

public record UpdateBudgetSuggestionRequest(decimal? BudgetSuggestion);
```

**UpdateBudgetSuggestionCommand** (MediatR Command):
```csharp
namespace SantaVibe.Api.Features.Groups.UpdateBudgetSuggestion;

using MediatR;
using SantaVibe.Api.Common;

public record UpdateBudgetSuggestionCommand(
    Guid GroupId,
    decimal? BudgetSuggestion
) : IRequest<Result<UpdateBudgetSuggestionResponse>>;
```

### Response Types

**UpdateBudgetSuggestionResponse** (DTO):
```csharp
namespace SantaVibe.Api.Features.Groups.UpdateBudgetSuggestion;

public record UpdateBudgetSuggestionResponse
{
    public required Guid GroupId { get; init; }
    public decimal? BudgetSuggestion { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
```

### Entity Types (Existing)

**GroupParticipant** (Data Entity):
- Located at: `SantaVibe.Api.Data.Entities.GroupParticipant`
- Relevant properties:
  - `GroupId` (Guid)
  - `UserId` (string)
  - `BudgetSuggestion` (decimal?)
  - `JoinedAt` (DateTimeOffset)

**Group** (Data Entity):
- Located at: `SantaVibe.Api.Data.Entities.Group`
- Relevant properties:
  - `Id` (Guid)
  - `DrawCompletedAt` (DateTimeOffset?)
  - Method: `IsDrawCompleted()` → bool

---

## 4. Response Details

### Success Response (200 OK)

**Status Code**: `200 OK`

**Response Body**:
```json
{
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "budgetSuggestion": 120.00,
  "updatedAt": "2025-10-16T10:30:00Z"
}
```

**Field Descriptions**:
| Field | Type | Description |
|-------|------|-------------|
| `groupId` | UUID | The group identifier (matches path parameter) |
| `budgetSuggestion` | decimal \| null | The updated budget suggestion value |
| `updatedAt` | DateTimeOffset (ISO 8601) | Timestamp when the update occurred (UTC) |

### Error Responses

#### 400 Bad Request - Validation Error
**Scenario**: Invalid budget suggestion value (outside allowed range)

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "ValidationError",
  "status": 400,
  "detail": "Budget suggestion validation failed",
  "errors": {
    "budgetSuggestion": ["Budget suggestion must be between 0.01 and 99999999.99"]
  }
}
```

#### 400 Bad Request - Draw Already Completed
**Scenario**: Attempting to update budget after draw has been executed

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "DrawAlreadyCompleted",
  "status": 400,
  "detail": "Cannot modify budget suggestion after draw has been completed"
}
```

#### 401 Unauthorized
**Scenario**: Missing or invalid JWT token

```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Authentication required"
}
```

#### 403 Forbidden - Not a Participant
**Scenario**: Authenticated user is not a participant in the group

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
  "title": "NotParticipant",
  "status": 403,
  "detail": "You are not a participant in this group"
}
```

#### 404 Not Found - Group Not Found
**Scenario**: Group with specified ID does not exist

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "GroupNotFound",
  "status": 404,
  "detail": "Group not found"
}
```

#### 500 Internal Server Error
**Scenario**: Unexpected database or system error

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "InternalServerError",
  "status": 500,
  "detail": "An unexpected error occurred"
}
```

---

## 5. Data Flow

### Sequence Diagram

```
Client                  Endpoint                Handler                Database
  |                        |                       |                      |
  |--PUT /api/groups/...-->|                       |                      |
  |  [JWT + Request Body]  |                       |                      |
  |                        |                       |                      |
  |                        |--Send Command-------->|                      |
  |                        |  (MediatR)            |                      |
  |                        |                       |                      |
  |                        |                       |--Get User ID-------->|
  |                        |                       |  (IUserAccessor)     |
  |                        |                       |                      |
  |                        |                       |--Query Group-------->|
  |                        |                       |  + Participant       |
  |                        |                       |<--Return Entities----|
  |                        |                       |                      |
  |                        |                       |--Validate----------->|
  |                        |                       |  (Group exists)      |
  |                        |                       |  (User participant)  |
  |                        |                       |  (Draw not done)     |
  |                        |                       |                      |
  |                        |                       |--Update Budget------>|
  |                        |                       |  Suggestion          |
  |                        |                       |<--Success------------|
  |                        |                       |                      |
  |                        |<--Return Result-------|                      |
  |                        |  (Success/Failure)    |                      |
  |                        |                       |                      |
  |<--200 OK + Response----|                       |                      |
  |  or Error              |                       |                      |
```

### Step-by-Step Flow

1. **HTTP Request Processing**
   - Client sends PUT request with JWT token and budget suggestion
   - ASP.NET Core authentication middleware validates JWT token
   - Endpoint receives authenticated request

2. **Command Creation**
   - Endpoint extracts `groupId` from route parameters
   - Endpoint extracts `budgetSuggestion` from request body
   - Creates `UpdateBudgetSuggestionCommand` with parameters
   - Sends command to MediatR

3. **Handler Execution**
   - Handler receives command
   - Extracts current user ID from JWT claims via `IUserAccessor`

4. **Data Retrieval**
   - Query database for `Group` entity with `groupId`
   - Include `GroupParticipants` navigation property
   - Use `.Include()` for eager loading

5. **Validation**
   - **Group Existence**: Verify group exists (404 if not)
   - **Participant Check**: Verify current user is in `GroupParticipants` (403 if not)
   - **Draw Status**: Verify `DrawCompletedAt` is null (400 if draw completed)
   - **Input Validation**: Verify budget suggestion is in valid range (400 if invalid)

6. **Update Operation**
   - Locate `GroupParticipant` record for current user
   - Update `BudgetSuggestion` field with new value (or null)
   - Update timestamp (track modification time)

7. **Persistence**
   - Call `_context.SaveChangesAsync()` to persist changes
   - Handle database exceptions (deadlocks, constraints, etc.)

8. **Response Generation**
   - Create `UpdateBudgetSuggestionResponse` with updated values
   - Set `updatedAt` to current UTC timestamp
   - Return `Result<UpdateBudgetSuggestionResponse>.Success()`

9. **HTTP Response**
   - Endpoint converts success result to `200 OK`
   - Or converts failure result to appropriate error response via `result.ToProblem()`
   - Returns JSON response to client

### Database Interactions

**Query Pattern**:
```csharp
var group = await _context.Groups
    .Include(g => g.GroupParticipants)
    .FirstOrDefaultAsync(g => g.Id == command.GroupId, cancellationToken);
```

**Update Pattern**:
```csharp
var participant = group.GroupParticipants
    .FirstOrDefault(gp => gp.UserId == userId);

participant.BudgetSuggestion = command.BudgetSuggestion;

await _context.SaveChangesAsync(cancellationToken);
```

---

## 6. Security Considerations

### Authentication
- **Mechanism**: JWT Bearer token authentication
- **Enforcement**: `.RequireAuthorization()` on endpoint
- **Validation**: Handled by ASP.NET Core authentication middleware
- **User Identification**: User ID extracted from JWT claims via `IUserAccessor`

### Authorization
- **Participant Verification**: Only group participants can update budget suggestions
- **Self-Service Only**: Users can only update their own budget suggestion
- **No Privilege Escalation**: No way to update other participants' suggestions
- **Organizer Restriction**: Organizer has same permissions as regular participants

### Input Validation
- **Type Safety**: Decimal type ensures numeric input
- **Range Validation**: Min 0.01, Max 99999999.99
- **SQL Injection Protection**: EF Core uses parameterized queries
- **XSS Protection**: Not applicable (numeric input only)

### Data Privacy
- **Anonymous Suggestions**: Budget suggestions are displayed anonymously to organizer
- **No Identity Disclosure**: Endpoint doesn't reveal who suggested what budget
- **Access Control**: Only participants can modify their own suggestion

### Rate Limiting
- **General Limit**: 100 requests per user per minute (as per API plan)
- **Specific Limit**: Not specifically rate-limited (not an expensive operation)

---

## 7. Error Handling

### Error Handling Strategy

#### Validation Errors (400 Bad Request)

**Scenario 1: Invalid Budget Range**
- **Trigger**: `budgetSuggestion < 0.01` or `budgetSuggestion > 99999999.99`
- **Error Code**: `ValidationError`
- **Response**: 400 with field-level validation details
- **Implementation**:
  ```csharp
  if (command.BudgetSuggestion.HasValue &&
      (command.BudgetSuggestion.Value < 0.01m || command.BudgetSuggestion.Value > 99999999.99m))
  {
      var errors = new Dictionary<string, string[]>
      {
          ["budgetSuggestion"] = new[] { "Budget suggestion must be between 0.01 and 99999999.99" }
      };
      return Result<UpdateBudgetSuggestionResponse>.ValidationFailure(
          "Budget suggestion validation failed", errors);
  }
  ```

**Scenario 2: Draw Already Completed**
- **Trigger**: `group.DrawCompletedAt != null`
- **Error Code**: `DrawAlreadyCompleted`
- **Response**: 400 with business rule violation message
- **Implementation**:
  ```csharp
  if (group.IsDrawCompleted())
  {
      return Result<UpdateBudgetSuggestionResponse>.Failure(
          "DrawAlreadyCompleted",
          "Cannot modify budget suggestion after draw has been completed");
  }
  ```

#### Authorization Errors (403 Forbidden)

**Scenario: Not a Participant**
- **Trigger**: User ID not found in `group.GroupParticipants`
- **Error Code**: `NotParticipant`
- **Response**: 403 Forbidden
- **Implementation**:
  ```csharp
  var participant = group.GroupParticipants
      .FirstOrDefault(gp => gp.UserId == userId);

  if (participant == null)
  {
      return Result<UpdateBudgetSuggestionResponse>.Failure(
          "NotParticipant",
          "You are not a participant in this group");
  }
  ```

#### Not Found Errors (404)

**Scenario: Group Not Found**
- **Trigger**: `group == null` after database query
- **Error Code**: `GroupNotFound`
- **Response**: 404 Not Found
- **Implementation**:
  ```csharp
  var group = await _context.Groups
      .Include(g => g.GroupParticipants)
      .FirstOrDefaultAsync(g => g.Id == command.GroupId, cancellationToken);

  if (group == null)
  {
      return Result<UpdateBudgetSuggestionResponse>.Failure(
          "GroupNotFound",
          "Group not found");
  }
  ```

#### Server Errors (500)

**Scenario: Database Exception**
- **Trigger**: Unexpected database errors (connection issues, deadlocks, etc.)
- **Error Code**: `InternalServerError`
- **Response**: 500 Internal Server Error
- **Implementation**:
  ```csharp
  try
  {
      await _context.SaveChangesAsync(cancellationToken);
  }
  catch (DbUpdateException ex)
  {
      _logger.LogError(ex,
          "Database error updating budget suggestion for user {UserId} in group {GroupId}",
          userId, command.GroupId);
      return Result<UpdateBudgetSuggestionResponse>.Failure(
          "InternalServerError",
          "An unexpected error occurred while updating budget suggestion");
  }
  ```

### Logging Strategy

**Log Levels**:
- **Information**: Successful budget suggestion updates
- **Warning**: Business rule violations (draw completed, not participant)
- **Error**: Database exceptions, unexpected errors

**Example Logging**:
```csharp
// Success
_logger.LogInformation(
    "User {UserId} updated budget suggestion to {BudgetSuggestion} for group {GroupId}",
    userId, command.BudgetSuggestion, command.GroupId);

// Business rule violation
_logger.LogWarning(
    "User {UserId} attempted to update budget suggestion for group {GroupId} after draw completion",
    userId, command.GroupId);

// Error
_logger.LogError(ex,
    "Failed to update budget suggestion for user {UserId} in group {GroupId}",
    userId, command.GroupId);
```

---

## 8. Performance Considerations

### Database Query Optimization

**Potential Bottlenecks**:
1. Loading entire `GroupParticipants` collection when only one is needed
2. N+1 query problem if not using `.Include()`

**Optimization Strategies**:

1. **Use AsNoTracking for Read Operations** (if not modifying group itself):
   ```csharp
   // Not applicable here since we're modifying the participant
   ```

2. **Load Only Required Participant**:
   ```csharp
   // Option 1: Direct query (most efficient)
   var participant = await _context.GroupParticipants
       .FirstOrDefaultAsync(gp =>
           gp.GroupId == command.GroupId &&
           gp.UserId == userId,
           cancellationToken);

   if (participant == null)
   {
       // Check if group exists for proper error message
       var groupExists = await _context.Groups
           .AnyAsync(g => g.Id == command.GroupId, cancellationToken);

       return groupExists
           ? Failure("NotParticipant", "You are not a participant in this group")
           : Failure("GroupNotFound", "Group not found");
   }

   // Then check draw status
   var group = await _context.Groups
       .AsNoTracking()
       .FirstAsync(g => g.Id == command.GroupId, cancellationToken);

   if (group.IsDrawCompleted())
   {
       return Failure("DrawAlreadyCompleted", "Cannot modify budget suggestion after draw");
   }
   ```

3. **Single Query Approach** (simpler, slightly less optimal):
   ```csharp
   var group = await _context.Groups
       .Include(g => g.GroupParticipants.Where(gp => gp.UserId == userId))
       .FirstOrDefaultAsync(g => g.Id == command.GroupId, cancellationToken);
   ```

**Recommended Approach**: Use Option 1 (direct query) for best performance, as it:
- Minimizes data transferred from database
- Provides clear error messages (group not found vs. not participant)
- Uses indexes effectively (composite index on GroupId + UserId)

### Concurrency Considerations

**Potential Issues**:
- Multiple simultaneous updates to same participant record (unlikely in practice)
- User updates suggestion while organizer performs draw

**Mitigation**:
- EF Core optimistic concurrency not required (last write wins is acceptable)
- Draw completion check happens in transaction, preventing race condition
- No complex business logic requiring pessimistic locking

### Response Time Targets
- **Expected**: < 200ms (simple update operation)
- **Acceptable**: < 500ms
- **Investigation Threshold**: > 1000ms

### Scalability
- **Database**: Single row update, minimal impact
- **Memory**: Minimal allocation (single participant record)
- **CPU**: Negligible (simple validation and update)
- **Horizontal Scaling**: Fully stateless, scales horizontally

---

## 9. Implementation Steps

### Step 1: Create Feature Folder Structure
Create the vertical slice folder structure:
```
/SantaVibe.Backend/SantaVibe.Api/Features/Groups/UpdateBudgetSuggestion/
├── UpdateBudgetSuggestionEndpoint.cs
├── UpdateBudgetSuggestionCommand.cs
├── UpdateBudgetSuggestionCommandHandler.cs
├── UpdateBudgetSuggestionRequest.cs
└── UpdateBudgetSuggestionResponse.cs
```

### Step 2: Create Response DTO
**File**: `UpdateBudgetSuggestionResponse.cs`

```csharp
namespace SantaVibe.Api.Features.Groups.UpdateBudgetSuggestion;

public record UpdateBudgetSuggestionResponse
{
    public required Guid GroupId { get; init; }
    public decimal? BudgetSuggestion { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
```

### Step 3: Create Request DTO
**File**: `UpdateBudgetSuggestionRequest.cs`

```csharp
namespace SantaVibe.Api.Features.Groups.UpdateBudgetSuggestion;

public record UpdateBudgetSuggestionRequest
{
    public decimal? BudgetSuggestion { get; init; }
}
```

### Step 4: Create MediatR Command
**File**: `UpdateBudgetSuggestionCommand.cs`

```csharp
using MediatR;
using SantaVibe.Api.Common;

namespace SantaVibe.Api.Features.Groups.UpdateBudgetSuggestion;

public record UpdateBudgetSuggestionCommand(
    Guid GroupId,
    decimal? BudgetSuggestion
) : IRequest<Result<UpdateBudgetSuggestionResponse>>;
```

### Step 5: Create Command Handler
**File**: `UpdateBudgetSuggestionCommandHandler.cs`

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using SantaVibe.Api.Common;
using SantaVibe.Api.Data;
using SantaVibe.Api.Services;

namespace SantaVibe.Api.Features.Groups.UpdateBudgetSuggestion;

public class UpdateBudgetSuggestionCommandHandler(
    ApplicationDbContext context,
    IUserAccessor userAccessor,
    ILogger<UpdateBudgetSuggestionCommandHandler> logger)
    : IRequestHandler<UpdateBudgetSuggestionCommand, Result<UpdateBudgetSuggestionResponse>>
{
    public async Task<Result<UpdateBudgetSuggestionResponse>> Handle(
        UpdateBudgetSuggestionCommand command,
        CancellationToken cancellationToken)
    {
        // Get current user ID from JWT
        var userId = userAccessor.GetCurrentUserId().ToString();

        // Validate budget suggestion range
        if (command.BudgetSuggestion.HasValue)
        {
            const decimal minBudget = 0.01m;
            const decimal maxBudget = 99999999.99m;

            if (command.BudgetSuggestion.Value < minBudget ||
                command.BudgetSuggestion.Value > maxBudget)
            {
                var errors = new Dictionary<string, string[]>
                {
                    ["budgetSuggestion"] = new[]
                    {
                        $"Budget suggestion must be between {minBudget} and {maxBudget}"
                    }
                };

                logger.LogWarning(
                    "Invalid budget suggestion {BudgetSuggestion} for user {UserId} in group {GroupId}",
                    command.BudgetSuggestion, userId, command.GroupId);

                return Result<UpdateBudgetSuggestionResponse>.ValidationFailure(
                    "Budget suggestion validation failed", errors);
            }
        }

        // Query for participant record directly (most efficient)
        var participant = await context.GroupParticipants
            .Include(gp => gp.Group)
            .FirstOrDefaultAsync(gp =>
                gp.GroupId == command.GroupId &&
                gp.UserId == userId,
                cancellationToken);

        // Check if participant exists
        if (participant == null)
        {
            // Distinguish between group not found and not a participant
            var groupExists = await context.Groups
                .AnyAsync(g => g.Id == command.GroupId, cancellationToken);

            if (!groupExists)
            {
                logger.LogWarning(
                    "Group {GroupId} not found for budget suggestion update by user {UserId}",
                    command.GroupId, userId);

                return Result<UpdateBudgetSuggestionResponse>.Failure(
                    "GroupNotFound",
                    "Group not found");
            }

            logger.LogWarning(
                "User {UserId} is not a participant in group {GroupId}",
                userId, command.GroupId);

            return Result<UpdateBudgetSuggestionResponse>.Failure(
                "NotParticipant",
                "You are not a participant in this group");
        }

        // Check if draw has been completed
        if (participant.Group.IsDrawCompleted())
        {
            logger.LogWarning(
                "User {UserId} attempted to update budget suggestion for group {GroupId} after draw completion",
                userId, command.GroupId);

            return Result<UpdateBudgetSuggestionResponse>.Failure(
                "DrawAlreadyCompleted",
                "Cannot modify budget suggestion after draw has been completed");
        }

        // Update budget suggestion
        participant.BudgetSuggestion = command.BudgetSuggestion;

        // Save changes
        try
        {
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "User {UserId} updated budget suggestion to {BudgetSuggestion} for group {GroupId}",
                userId, command.BudgetSuggestion, command.GroupId);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex,
                "Database error updating budget suggestion for user {UserId} in group {GroupId}",
                userId, command.GroupId);

            return Result<UpdateBudgetSuggestionResponse>.Failure(
                "InternalServerError",
                "An unexpected error occurred while updating budget suggestion");
        }

        // Create response
        var response = new UpdateBudgetSuggestionResponse
        {
            GroupId = command.GroupId,
            BudgetSuggestion = command.BudgetSuggestion,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        return Result<UpdateBudgetSuggestionResponse>.Success(response);
    }
}
```

### Step 6: Create Endpoint Mapping
**File**: `UpdateBudgetSuggestionEndpoint.cs`

```csharp
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SantaVibe.Api.Common;

namespace SantaVibe.Api.Features.Groups.UpdateBudgetSuggestion;

public static class UpdateBudgetSuggestionEndpoint
{
    public static void MapUpdateBudgetSuggestionEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPut(
            "/api/groups/{groupId}/participants/me/budget-suggestion",
            async (
                Guid groupId,
                UpdateBudgetSuggestionRequest request,
                ISender sender) =>
            {
                var command = new UpdateBudgetSuggestionCommand(
                    groupId,
                    request.BudgetSuggestion);

                var result = await sender.Send(command);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.ToProblem();
            })
            .RequireAuthorization()
            .WithTags("Groups", "Budget")
            .WithName("UpdateBudgetSuggestion")
            .WithDescription("Update or set the authenticated user's budget suggestion for a group")
            .Produces<UpdateBudgetSuggestionResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}
```

### Step 7: Register Endpoint
**File**: Update the main endpoint registration file (e.g., `Program.cs` or endpoint registration module)

```csharp
// Add to endpoint registration section
app.MapUpdateBudgetSuggestionEndpoint();
```

### Step 8: Test the Endpoint

**Unit Tests** (create `UpdateBudgetSuggestionCommandHandlerTests.cs`):
- Test successful budget suggestion update
- Test clearing budget suggestion (null value)
- Test validation errors (out of range)
- Test group not found (404)
- Test not a participant (403)
- Test draw already completed (400)

**Integration Tests**:
- Test full HTTP request/response cycle
- Test JWT authentication enforcement
- Test database persistence

**Manual Testing with Swagger**:
1. Navigate to `/swagger` endpoint
2. Authenticate with valid JWT token
3. Execute PUT request with valid groupId and budget suggestion
4. Verify 200 OK response
5. Test error scenarios (invalid budget, completed draw, etc.)

### Step 9: Add to API Documentation
Update Swagger/OpenAPI documentation with:
- Request/response schemas
- Authentication requirements
- Error responses
- Example requests

---

## 10. Testing Checklist

### Unit Tests
- [ ] Valid budget suggestion update succeeds
- [ ] Null budget suggestion clears value
- [ ] Budget below minimum (0.01) returns validation error
- [ ] Budget above maximum (99999999.99) returns validation error
- [ ] Non-existent group returns 404
- [ ] Non-participant user returns 403
- [ ] Draw completed returns 400
- [ ] Database exception returns 500
- [ ] Logging statements executed correctly

### Integration Tests
- [ ] Full request/response cycle with authenticated user
- [ ] JWT authentication required
- [ ] Participant can update own suggestion
- [ ] Non-participant cannot update
- [ ] Database updated correctly
- [ ] Timestamp reflects update time
- [ ] Organizer can see anonymous suggestions (separate test)

### Manual Testing
- [ ] Swagger UI endpoint accessible
- [ ] Request validation feedback in UI
- [ ] Error messages are user-friendly
- [ ] Response format matches specification

---

## 11. Dependencies

### Required Services
- `ApplicationDbContext` - Database access
- `IUserAccessor` - Get current user ID from JWT
- `ILogger<UpdateBudgetSuggestionCommandHandler>` - Structured logging

### Required Packages
- `MediatR` - CQRS pattern
- `Microsoft.EntityFrameworkCore` - ORM
- `Microsoft.AspNetCore.Authentication.JwtBearer` - JWT authentication

### Entity Dependencies
- `Group` entity
- `GroupParticipant` entity

---

## 12. Future Enhancements

### Potential Improvements
1. **Optimistic Concurrency**: Add `RowVersion` to prevent lost updates
2. **Budget Suggestion History**: Track all budget suggestion changes over time
3. **Notifications**: Notify organizer when participants update budget suggestions
4. **Budget Range Suggestions**: Provide UI feedback on suggested ranges based on group
5. **Currency Support**: Support multiple currencies (currently PLN only)
6. **Batch Updates**: Allow organizer to set default suggestion for all participants

### Performance Optimizations
1. **Caching**: Cache group draw status to reduce database queries
2. **Compiled Queries**: Use EF Core compiled queries for frequently executed queries
3. **Database Indexes**: Ensure composite index on (GroupId, UserId) in GroupParticipants

---

## Document Metadata

**Version**: 1.0
**Date**: 2025-10-28
**Author**: Implementation Planning Team
**Status**: Ready for Implementation
**Related Documents**:
- API Plan: `/docs/api-plan.md`
- Backend Guidelines: `/docs/backend-guidelines.md`
- Tech Stack: `/docs/tech-stack.md`
