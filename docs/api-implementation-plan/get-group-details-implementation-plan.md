# API Endpoint Implementation Plan: Get Group Details

## 1. Endpoint Overview

The `GET /api/groups/{groupId}` endpoint retrieves detailed information about a specific Secret Santa group. The response structure varies based on whether the draw has been completed:

- **Before Draw**: Returns full participant list, exclusion rule count, and draw validation status to help organizer prepare for the draw
- **After Draw**: Returns simplified group info with the current user's assignment (who they are buying a gift for)

The endpoint enforces authorization to ensure only group participants can access group details.

## 2. Request Details

- **HTTP Method**: GET
- **URL Structure**: `/api/groups/{groupId}`
- **Authentication**: Required (JWT Bearer token via `.RequireAuthorization()`)
- **Authorization**: User must be a participant in the specified group

### Path Parameters
- **groupId** (required, UUID): The unique identifier of the group

### Query Parameters
None

### Request Body
None (GET request)

## 3. Used Types

### Query Model
```csharp
// Features/Groups/GetGroupDetails/GetGroupDetailsQuery.cs
public record GetGroupDetailsQuery(Guid GroupId)
    : IRequest<Result<GetGroupDetailsResponse>>;
```

### Response Models

```csharp
// Features/Groups/GetGroupDetails/GetGroupDetailsResponse.cs
public record GetGroupDetailsResponse
{
    // Common fields (always present)
    public required Guid GroupId { get; init; }
    public required string Name { get; init; }
    public required string OrganizerId { get; init; }
    public required string OrganizerName { get; init; }
    public required bool IsOrganizer { get; init; }
    public decimal? Budget { get; init; }
    public required bool DrawCompleted { get; init; }
    public DateTimeOffset? DrawCompletedAt { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required int ParticipantCount { get; init; }

    // Before draw only (null if draw completed)
    public List<ParticipantDto>? Participants { get; init; }
    public int? ExclusionRuleCount { get; init; }
    public bool? CanDraw { get; init; }
    public DrawValidationDto? DrawValidation { get; init; }

    // After draw only (null if draw not completed)
    public AssignmentDto? MyAssignment { get; init; }
}

public record ParticipantDto
{
    public required string UserId { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required DateTimeOffset JoinedAt { get; init; }
    public required bool HasBudgetSuggestion { get; init; }
    public required bool HasWishlist { get; init; }
}

public record AssignmentDto
{
    public required string RecipientId { get; init; }
    public required string RecipientFirstName { get; init; }
    public required string RecipientLastName { get; init; }
    public required bool HasWishlist { get; init; }
}

public record DrawValidationDto
{
    public required bool IsValid { get; init; }
    public required List<string> Errors { get; init; }
}
```

### Endpoint Registration
```csharp
// Features/Groups/GetGroupDetails/GetGroupDetailsEndpoint.cs
public static class GetGroupDetailsEndpoint
{
    public static void MapGetGroupDetailsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/groups/{groupId:guid}", async (
                Guid groupId,
                ISender sender) =>
            {
                var query = new GetGroupDetailsQuery(groupId);
                var result = await sender.Send(query);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
            })
            .RequireAuthorization()
            .WithTags("Groups")
            .WithName("GetGroupDetails")
            .Produces<GetGroupDetailsResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}
```

### Query Handler
```csharp
// Features/Groups/GetGroupDetails/GetGroupDetailsQueryHandler.cs
public class GetGroupDetailsQueryHandler
    : IRequestHandler<GetGroupDetailsQuery, Result<GetGroupDetailsResponse>>
{
    private readonly ApplicationDbContext _context;
    private readonly IUserAccessor _userAccessor;

    // Constructor with DI
    // Handle method with implementation
}
```

## 4. Response Details

### Success Response (200 OK) - Before Draw
```json
{
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "name": "Family Secret Santa 2025",
  "organizerId": "550e8400-e29b-41d4-a716-446655440000",
  "organizerName": "Jan Kowalski",
  "isOrganizer": true,
  "budget": 100.00,
  "drawCompleted": false,
  "drawCompletedAt": null,
  "createdAt": "2025-10-15T10:00:00Z",
  "participantCount": 2,
  "participants": [
    {
      "userId": "550e8400-e29b-41d4-a716-446655440000",
      "firstName": "Jan",
      "lastName": "Kowalski",
      "joinedAt": "2025-10-15T10:00:00Z",
      "hasBudgetSuggestion": true,
      "hasWishlist": true
    },
    {
      "userId": "660f9511-f30c-52f5-b827-557766551111",
      "firstName": "Anna",
      "lastName": "Nowak",
      "joinedAt": "2025-10-15T11:30:00Z",
      "hasBudgetSuggestion": true,
      "hasWishlist": false
    }
  ],
  "exclusionRuleCount": 0,
  "canDraw": false,
  "drawValidation": {
    "isValid": false,
    "errors": ["Minimum 3 participants required for draw"]
  },
  "myAssignment": null
}
```

### Success Response (200 OK) - After Draw
```json
{
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "name": "Family Secret Santa 2025",
  "organizerId": "550e8400-e29b-41d4-a716-446655440000",
  "organizerName": "Jan Kowalski",
  "isOrganizer": true,
  "budget": 100.00,
  "drawCompleted": true,
  "drawCompletedAt": "2025-10-20T15:30:00Z",
  "createdAt": "2025-10-15T10:00:00Z",
  "participantCount": 5,
  "participants": null,
  "exclusionRuleCount": null,
  "canDraw": null,
  "drawValidation": null,
  "myAssignment": {
    "recipientId": "770g0622-g41d-63g6-c938-668877662222",
    "recipientFirstName": "Maria",
    "recipientLastName": "Wiśniewska",
    "hasWishlist": true
  }
}
```

### Error Responses

**401 Unauthorized**
- Triggered by: Missing or invalid JWT token
- Handled by: ASP.NET Core authorization middleware
- No custom handling needed in handler

**403 Forbidden**
```json
{
  "error": "Forbidden",
  "message": "You are not a participant in this group"
}
```

**404 Not Found**
```json
{
  "error": "GroupNotFound",
  "message": "Group does not exist"
}
```

**500 Internal Server Error**
- Triggered by: Unexpected database errors
- Handled by: Global exception handling middleware
- Logged via Serilog

## 5. Data Flow

### High-Level Flow
1. **Request Processing**
   - ASP.NET Core extracts `groupId` from route
   - Authorization middleware validates JWT token
   - MediatR dispatches query to handler

2. **Handler Execution**
   - Extract current user ID from `IUserAccessor`
   - Query group from database with appropriate includes
   - Verify group exists (404 if not)
   - Verify user is participant (403 if not)
   - Branch based on `DrawCompletedAt` status:
     - **Before Draw**: Load participants, exclusion rules, validate draw
     - **After Draw**: Load user's assignment
   - Map to response DTO
   - Return `Result<GetGroupDetailsResponse>.Success()`

3. **Response Mapping**
   - Handler returns success result
   - Endpoint maps to 200 OK with response body

### Database Queries

**Initial Group Query** (single query with conditional includes):
```csharp
var group = await _context.Groups
    .AsNoTracking()
    .Include(g => g.Organizer)
    .Include(g => g.GroupParticipants)
        .ThenInclude(gp => gp.User)
    .FirstOrDefaultAsync(g => g.Id == request.GroupId, cancellationToken);
```

**Participant Check**:
```csharp
var isParticipant = group.GroupParticipants
    .Any(gp => gp.UserId == currentUserId.ToString());
```

**Conditional Loading - Before Draw**:
```csharp
var exclusionRuleCount = await _context.ExclusionRules
    .CountAsync(er => er.GroupId == request.GroupId, cancellationToken);
```

**Conditional Loading - After Draw**:
```csharp
var assignment = await _context.Assignments
    .AsNoTracking()
    .Include(a => a.Recipient)
    .FirstOrDefaultAsync(a =>
        a.GroupId == request.GroupId &&
        a.SantaUserId == currentUserId.ToString(),
        cancellationToken);
```

### Draw Validation Logic (Before Draw Only)

Validation rules:
1. **Minimum participants**: At least 3 participants required
2. **Exclusion rules feasibility**: Check if exclusion rules allow valid draw (future enhancement - for MVP, just check participant count)

```csharp
var errors = new List<string>();

if (participantCount < 3)
{
    errors.Add("Minimum 3 participants required for draw");
}

var validation = new DrawValidationDto
{
    IsValid = errors.Count == 0,
    Errors = errors
};
```

## 6. Security Considerations

### Authentication
- JWT Bearer token required (enforced via `.RequireAuthorization()`)
- Token validated by ASP.NET Core Identity middleware
- User ID extracted from claims via `IUserAccessor`

### Authorization
- **Group Participation Check**: User must be in `GroupParticipants` table for the requested group
- Return 403 Forbidden if not a participant
- No special organizer-only restrictions for this endpoint

### Data Privacy
- **Before Draw**: All participants can see participant list (necessary for draw preparation)
- **After Draw**: User only sees their own assignment (not others' assignments)
- Budget suggestions and wishlists not exposed in this endpoint (separate endpoints)

### Input Validation
- `groupId` format validated by ASP.NET Core routing (`:guid` constraint)
- No additional validation needed

## 7. Error Handling

### Error Scenarios and Handling

| Scenario | Status Code | Error Code | Message | Handler Action |
|----------|-------------|------------|---------|----------------|
| Missing/invalid JWT | 401 | - | Handled by middleware | N/A |
| Group not found | 404 | GroupNotFound | "Group does not exist" | `Result<T>.Failure()` |
| User not participant | 403 | Forbidden | "You are not a participant in this group" | `Result<T>.Failure()` |
| Database error | 500 | - | Generic error | Exception propagates to global handler |

### Handler Error Handling Pattern
```csharp
if (group == null)
{
    return Result<GetGroupDetailsResponse>.Failure(
        "GroupNotFound",
        "Group does not exist"
    );
}

if (!isParticipant)
{
    return Result<GetGroupDetailsResponse>.Failure(
        "Forbidden",
        "You are not a participant in this group"
    );
}
```

### Endpoint Error Mapping
```csharp
return result.IsSuccess
    ? Results.Ok(result.Value)
    : result.ToProblem();
```

The `.ToProblem()` extension converts:
- `GroupNotFound` → 404 Not Found
- `Forbidden` → 403 Forbidden
- Other errors → Appropriate status codes

## 8. Performance Considerations

### Optimization Strategies

1. **Use AsNoTracking()**: Read-only query, no change tracking overhead
2. **Eager Loading**: Use `Include()` to prevent N+1 queries
3. **Projection**: Select only needed fields to reduce data transfer
4. **Conditional Queries**: Only load participants/assignments when needed based on draw status
5. **Computed Fields**: Calculate `participantCount` from already-loaded collection instead of separate query

### Query Performance
- Expected query time: < 100ms for groups with up to 30 participants
- Single database roundtrip for main query
- Optional second query for exclusion rule count (only before draw)
- Indexes on foreign keys (GroupId, UserId) ensure fast lookups

### Potential Bottlenecks
- Large participant lists (mitigated by projection to DTO)
- Complex exclusion rule validation (deferred to future enhancement)

## 9. Implementation Steps

### Step 1: Create Directory Structure
```
SantaVibe.Api/Features/Groups/GetGroupDetails/
├── GetGroupDetailsEndpoint.cs
├── GetGroupDetailsQuery.cs
├── GetGroupDetailsQueryHandler.cs
└── GetGroupDetailsResponse.cs
```

### Step 2: Define Response DTOs
Create `GetGroupDetailsResponse.cs` with:
- `GetGroupDetailsResponse` record (main response)
- `ParticipantDto` record
- `AssignmentDto` record
- `DrawValidationDto` record

### Step 3: Create Query Model
Create `GetGroupDetailsQuery.cs`:
```csharp
public record GetGroupDetailsQuery(Guid GroupId)
    : IRequest<Result<GetGroupDetailsResponse>>;
```

### Step 4: Implement Query Handler
Create `GetGroupDetailsQueryHandler.cs`:

1. **Constructor**: Inject `ApplicationDbContext` and `IUserAccessor`
2. **Handle Method**:
   - Get current user ID
   - Query group with organizer and participants
   - Check if group exists → return 404 if not
   - Check if user is participant → return 403 if not
   - Determine if draw completed
   - **If draw not completed**:
     - Map participants to DTOs
     - Count exclusion rules
     - Validate draw feasibility
     - Build response with participant data
   - **If draw completed**:
     - Query user's assignment
     - Map assignment to DTO
     - Build response with assignment data
   - Return success result

### Step 5: Create Endpoint Registration
Create `GetGroupDetailsEndpoint.cs`:
- Map GET route with `:guid` constraint
- Add authorization requirement
- Configure Swagger metadata
- Map query parameter to MediatR query
- Return result or problem details

### Step 6: Register Endpoint in Program.cs
Add to endpoint configuration:
```csharp
app.MapGetGroupDetailsEndpoint();
```

### Step 7: Unit Tests (Optional but Recommended)
Create test file `GetGroupDetailsQueryHandlerTests.cs`:
- Test successful retrieval before draw
- Test successful retrieval after draw
- Test 404 when group not found
- Test 403 when user not participant
- Test draw validation errors
- Test organizer vs non-organizer perspective

### Step 8: Integration Testing
- Test with Postman/REST Client
- Verify JWT authentication works
- Verify authorization checks work
- Verify response structure matches specification
- Test with different draw states
- Verify performance with realistic data

### Step 9: Update API Documentation
- Ensure Swagger UI shows correct request/response schemas
- Add XML documentation comments
- Update any API documentation files if needed

### Step 10: Code Review Checklist
- [ ] Follows vertical slice architecture
- [ ] Uses minimal API pattern
- [ ] Implements proper authorization checks
- [ ] Returns correct status codes
- [ ] Uses `AsNoTracking()` for read queries
- [ ] Avoids N+1 query problems
- [ ] Handles all error scenarios
- [ ] Response DTOs match API specification exactly
- [ ] Uses primary constructors (C# 13)
- [ ] Logged errors appropriately
- [ ] Includes XML documentation

---

## Notes for Implementation

### Vertical Slice Organization
Following the established pattern in `GetUserGroups`, all related files should be in the `Features/Groups/GetGroupDetails/` directory to maintain cohesion.

### MediatR Pattern
Use the CQRS pattern with MediatR as demonstrated in existing endpoints. The query handler should be the single source of business logic for this operation.

### Draw Validation
For MVP, simple validation (participant count) is sufficient. Complex graph-based exclusion rule validation can be enhanced in future iterations and should be extracted to a reusable service when the draw execution endpoint is implemented.

### Testing Strategy
Focus integration tests on the authorization logic (participant check) and conditional response structure (before/after draw), as these are the unique aspects of this endpoint.
