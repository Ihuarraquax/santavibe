# API Endpoint Implementation Plan: Get My Assignment

## 1. Endpoint Overview

This endpoint retrieves the authenticated user's Secret Santa assignment for a specific group, showing them who they are buying a gift for. This is only accessible after the draw has been completed and enforces privacy by ensuring users can only see their own assignment, not anyone else's.

**Key Business Rules**:
- User must be authenticated via JWT
- User must be a participant in the specified group
- Draw must be completed for the group
- User can only see their own assignment (SantaUserId = authenticated user)
- Organizers have no special privileges - they see only their own assignment

## 2. Request Details

- **HTTP Method**: GET
- **URL Structure**: `/api/groups/{groupId}/my-assignment`
- **Authentication**: Required (JWT Bearer token)

### Parameters

**Required**:
- `groupId` (path parameter, Guid/UUID): The unique identifier of the Secret Santa group

**Implicit**:
- `userId` (extracted from JWT claims): The authenticated user's identifier

**Optional**:
- None

### Request Headers
```
Authorization: Bearer {jwt_token}
```

## 3. Used Types

### Response DTOs

**GetMyAssignmentResponse.cs**
```csharp
public record GetMyAssignmentResponse(
    Guid GroupId,
    string GroupName,
    decimal Budget,
    DateTime DrawCompletedAt,
    RecipientDto Recipient
);

public record RecipientDto(
    string UserId,
    string FirstName,
    string LastName,
    bool HasWishlist,
    DateTime? WishlistLastModified
);
```

### Command/Query Model

**GetMyAssignmentQuery.cs**
```csharp
public record GetMyAssignmentQuery(
    Guid GroupId,
    string UserId
) : IRequest<GetMyAssignmentResponse>;
```

### Error Response DTO

**ErrorResponse.cs** (shared across endpoints)
```csharp
public record ErrorResponse(
    string Error,
    string Message,
    object? Details = null
);
```

## 4. Response Details

### Success Response (200 OK)
```json
{
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "groupName": "Family Secret Santa 2025",
  "budget": 100.00,
  "drawCompletedAt": "2025-10-20T15:30:00Z",
  "recipient": {
    "userId": "770g0622-g41d-63g6-c938-668877662222",
    "firstName": "Maria",
    "lastName": "Wiśniewska",
    "hasWishlist": true,
    "wishlistLastModified": "2025-10-20T16:00:00Z"
  }
}
```

### Error Responses

**401 Unauthorized**
```json
{
  "error": "Unauthorized",
  "message": "Authentication required"
}
```

**403 Forbidden - Draw Not Completed**
```json
{
  "error": "DrawNotCompleted",
  "message": "Draw has not been completed yet"
}
```

**403 Forbidden - Not a Participant**
```json
{
  "error": "NotAParticipant",
  "message": "You are not a participant in this group"
}
```

**404 Not Found - Group**
```json
{
  "error": "GroupNotFound",
  "message": "Group does not exist"
}
```

**404 Not Found - Assignment**
```json
{
  "error": "AssignmentNotFound",
  "message": "No assignment found for this group"
}
```

## 5. Data Flow

### Database Query Flow

1. **Extract User Identity**
   - Extract `userId` from JWT claims (`ClaimTypes.NameIdentifier` or custom claim)
   - Parse `groupId` from route parameter

2. **Validate Group Existence and Draw Completion**
   ```csharp
   var group = await dbContext.Groups
       .AsNoTracking()
       .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);

   if (group == null)
       throw new NotFoundException("Group does not exist");

   if (group.DrawCompletedAt == null)
       throw new ForbiddenException("DrawNotCompleted", "Draw has not been completed yet");
   ```

3. **Verify User Participation**
   ```csharp
   var isParticipant = await dbContext.GroupParticipants
       .AsNoTracking()
       .AnyAsync(gp => gp.GroupId == groupId && gp.UserId == userId, cancellationToken);

   if (!isParticipant)
       throw new ForbiddenException("NotAParticipant", "You are not a participant in this group");
   ```

4. **Load Assignment with Recipient Details**
   ```csharp
   var assignment = await dbContext.Assignments
       .AsNoTracking()
       .Include(a => a.RecipientUser)
       .Include(a => a.Group)
       .Where(a => a.GroupId == groupId && a.SantaUserId == userId)
       .Select(a => new {
           a.Group.Id,
           a.Group.Name,
           a.Group.Budget,
           a.Group.DrawCompletedAt,
           RecipientUserId = a.RecipientUserId,
           RecipientFirstName = a.RecipientUser.FirstName,
           RecipientLastName = a.RecipientUser.LastName,
           RecipientParticipant = dbContext.GroupParticipants
               .FirstOrDefault(gp => gp.GroupId == groupId && gp.UserId == a.RecipientUserId)
       })
       .FirstOrDefaultAsync(cancellationToken);

   if (assignment == null)
       throw new NotFoundException("No assignment found for this group");
   ```

5. **Map to Response DTO**
   ```csharp
   return new GetMyAssignmentResponse(
       GroupId: assignment.Id,
       GroupName: assignment.Name,
       Budget: assignment.Budget!.Value, // Budget is set during draw
       DrawCompletedAt: assignment.DrawCompletedAt!.Value,
       Recipient: new RecipientDto(
           UserId: assignment.RecipientUserId,
           FirstName: assignment.RecipientFirstName,
           LastName: assignment.RecipientLastName,
           HasWishlist: !string.IsNullOrEmpty(assignment.RecipientParticipant?.WishlistContent),
           WishlistLastModified: assignment.RecipientParticipant?.WishlistLastModified
       )
   );
   ```

## 6. Security Considerations

### Authentication
- Endpoint must be protected with `[Authorize]` attribute or `.RequireAuthorization()` for minimal APIs
- JWT token validated by ASP.NET Core Identity middleware
- User identity extracted from `ClaimsPrincipal`

### Authorization
- User must be a participant in the group (checked in handler)
- Draw must be completed (checked in handler)
- User can only see their own assignment (enforced by query filter: `SantaUserId = userId`)

### Data Privacy
- Response includes only the recipient's basic information (name, wishlist status)
- Wishlist content is NOT included (separate endpoint for that)
- User cannot see who is buying for them (reverse lookup not allowed)
- Organizer has no special access - sees only their own assignment

### Input Validation
- `groupId` validated as valid Guid by model binding
- `userId` validated by authentication middleware
- Business validations in handler

## 7. Error Handling

### Error Scenarios and Status Codes

| Scenario | Status Code | Error Code | Message |
|----------|-------------|------------|---------|
| Missing/invalid JWT token | 401 | Unauthorized | Authentication required |
| User not a participant | 403 | NotAParticipant | You are not a participant in this group |
| Draw not completed | 403 | DrawNotCompleted | Draw has not been completed yet |
| Group doesn't exist | 404 | GroupNotFound | Group does not exist |
| Assignment not found | 404 | AssignmentNotFound | No assignment found for this group |
| Database error | 500 | InternalServerError | An unexpected error occurred |

### Exception Handling Strategy

Use custom exceptions that map to HTTP status codes:
- `NotFoundException` → 404
- `ForbiddenException` → 403
- `UnauthorizedException` → 401

Implement global exception handling middleware or use endpoint filters to catch and map exceptions to appropriate HTTP responses.

### Logging

Use Serilog for structured logging:
- Log info: Successful assignment retrieval
- Log warning: Authorization failures (not participant, draw not completed)
- Log error: Database errors, unexpected exceptions

```csharp
_logger.LogInformation(
    "User {UserId} retrieved assignment for group {GroupId}",
    userId, groupId);

_logger.LogWarning(
    "User {UserId} attempted to access assignment for group {GroupId} but is not a participant",
    userId, groupId);
```

## 8. Performance Considerations

### Query Optimization
- Use `AsNoTracking()` for read-only queries
- Use projection with `Select()` to load only required fields
- Consider using a compiled query if this endpoint is frequently accessed
- Ensure indexes exist on:
  - `Assignments(GroupId, SantaUserId)` - composite index for fast lookup
  - `GroupParticipants(GroupId, UserId)` - composite index for participation check
  - `Groups(Id)` - primary key index (already exists)

### Caching Strategy (Future Enhancement)
- Consider caching group details (name, budget, drawCompletedAt) as they don't change after draw
- Assignment data could be cached with invalidation on wishlist updates
- Cache key pattern: `assignment:{groupId}:{userId}`

### Database Considerations
- Single query approach preferred over multiple round trips
- Use eager loading with `Include()` for related entities
- Limit data transfer by projecting to DTO shape in query

## 9. Implementation Steps

### Step 1: Create Feature Folder Structure
```
Features/
└── Assignments/
    └── GetMyAssignment/
        ├── GetMyAssignmentEndpoint.cs
        ├── GetMyAssignmentQuery.cs
        ├── GetMyAssignmentHandler.cs
        └── GetMyAssignmentResponse.cs
```

### Step 2: Define Response DTOs
Create `GetMyAssignmentResponse.cs` with the response structure defined in section 3.

### Step 3: Define Query Model
Create `GetMyAssignmentQuery.cs` implementing `IRequest<GetMyAssignmentResponse>` with GroupId and UserId properties.

### Step 4: Implement MediatR Handler
Create `GetMyAssignmentHandler.cs` implementing `IRequestHandler<GetMyAssignmentQuery, GetMyAssignmentResponse>`:
1. Inject `ApplicationDbContext` and `ILogger`
2. Implement validation logic:
   - Check group exists
   - Check draw is completed
   - Check user is participant
3. Query assignment with recipient details
4. Map to response DTO
5. Add structured logging

### Step 5: Create Minimal API Endpoint
Create `GetMyAssignmentEndpoint.cs`:
1. Define endpoint with `MapGet()`
2. Extract groupId from route
3. Extract userId from `ClaimsPrincipal`
4. Create and send query via MediatR
5. Return response with appropriate status code
6. Add endpoint metadata (tags, summary, produces)

### Step 6: Register Endpoint
In `Program.cs` or endpoint registration class:
```csharp
app.MapGet("/api/groups/{groupId}/my-assignment",
    async (Guid groupId, ClaimsPrincipal user, IMediator mediator) =>
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var query = new GetMyAssignmentQuery(groupId, userId);
        var result = await mediator.Send(query);
        return Results.Ok(result);
    })
    .RequireAuthorization()
    .WithTags("Assignments")
    .WithName("GetMyAssignment")
    .Produces<GetMyAssignmentResponse>(StatusCodes.Status200OK)
    .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized)
    .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
    .Produces<ErrorResponse>(StatusCodes.Status404NotFound);
```

### Step 7: Implement Global Exception Handling
Create or update exception handling middleware/filter to handle:
- `NotFoundException` → 404 response
- `ForbiddenException` → 403 response
- General exceptions → 500 response

### Step 8: Add Database Indexes
Create migration to add composite indexes if not already present:
```csharp
migrationBuilder.CreateIndex(
    name: "IX_Assignments_GroupId_SantaUserId",
    table: "Assignments",
    columns: new[] { "GroupId", "SantaUserId" });

migrationBuilder.CreateIndex(
    name: "IX_GroupParticipants_GroupId_UserId",
    table: "GroupParticipants",
    columns: new[] { "GroupId", "UserId" });
```

### Step 9: Write Unit Tests
Create test class `GetMyAssignmentHandlerTests.cs`:
- Test successful assignment retrieval
- Test group not found scenario
- Test draw not completed scenario
- Test user not participant scenario
- Test assignment not found scenario

### Step 10: Write Integration Tests
Create integration test using TestContainers:
- Test full request/response cycle
- Test authentication enforcement
- Test authorization rules
- Test error responses

### Step 11: Add Swagger Documentation
Ensure endpoint has XML comments and OpenAPI metadata:
```csharp
/// <summary>
/// Get my Secret Santa assignment for a group
/// </summary>
/// <param name="groupId">The group identifier</param>
/// <returns>Assignment details with recipient information</returns>
/// <response code="200">Returns the assignment details</response>
/// <response code="401">Unauthorized - missing or invalid token</response>
/// <response code="403">Forbidden - not a participant or draw not completed</response>
/// <response code="404">Not found - group or assignment doesn't exist</response>
```

### Step 12: Manual Testing
1. Test with Postman/Insomnia/REST Client
2. Verify JWT authentication works
3. Test all error scenarios
4. Verify response format matches specification
5. Check database query performance with profiling

### Step 13: Code Review Checklist
- [ ] Follows vertical slice architecture
- [ ] Uses minimal API pattern correctly
- [ ] Implements proper authentication/authorization
- [ ] Handles all error scenarios per specification
- [ ] Includes structured logging
- [ ] Uses AsNoTracking for read-only queries
- [ ] Maps correctly to response DTO
- [ ] Has appropriate unit and integration tests
- [ ] Documented with XML comments for Swagger
