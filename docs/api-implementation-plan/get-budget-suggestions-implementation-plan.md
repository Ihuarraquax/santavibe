# API Endpoint Implementation Plan: Get Anonymous Budget Suggestions

## 1. Endpoint Overview

This endpoint retrieves an anonymous, sorted list of budget suggestions from all participants in a Secret Santa group. Only the group organizer can access this endpoint to review participant budget preferences before setting the final budget for the draw. The response maintains participant anonymity by only returning the suggestion amounts without any user identification.

## 2. Request Details

- **HTTP Method**: GET
- **URL Structure**: `/api/groups/{groupId}/budget/suggestions`
- **Parameters**:
  - **Required**:
    - `groupId` (UUID, path parameter): The unique identifier of the group
  - **Optional**: None
- **Request Body**: None (GET request)
- **Authentication**: JWT Bearer token required in Authorization header
- **Authorization**: User must be the organizer of the specified group

## 3. Used Types

### Request Types

```csharp
// Query record for MediatR
public record GetBudgetSuggestionsQuery(Guid GroupId, string UserId) : IRequest<BudgetSuggestionsResponse>;
```

### Response DTOs

```csharp
public record BudgetSuggestionsResponse
{
    public required Guid GroupId { get; init; }
    public required List<decimal> Suggestions { get; init; }
    public required int Count { get; init; }
    public required int ParticipantCount { get; init; }
    public required int SuggestionsReceived { get; init; }
    public required decimal? CurrentBudget { get; init; }
}
```

### Entity Types (Existing)

- `Group` entity (from Data/Entities/Group.cs)
  - Properties needed: Id, OrganizerUserId, Budget
- `GroupParticipant` entity (from Data/Entities/GroupParticipant.cs)
  - Properties needed: GroupId, UserId, BudgetSuggestion

## 4. Response Details

### Success Response (200 OK)

```json
{
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "suggestions": [50.00, 75.00, 80.00, 100.00, 100.00],
  "count": 5,
  "participantCount": 6,
  "suggestionsReceived": 5,
  "currentBudget": null
}
```

**Response Fields**:
- `groupId`: The group identifier (matches request parameter)
- `suggestions`: Array of budget amounts sorted in ascending order (anonymous)
- `count`: Number of suggestions in the array (equal to suggestionsReceived)
- `participantCount`: Total number of participants in the group
- `suggestionsReceived`: Number of participants who provided a budget suggestion
- `currentBudget`: The finalized budget set by organizer (null before draw execution)

### Error Responses

- **401 Unauthorized**: Missing or invalid JWT token
- **403 Forbidden**: User is authenticated but is not the group organizer
  ```json
  {
    "error": "Forbidden",
    "message": "Only the group organizer can view budget suggestions"
  }
  ```
- **404 Not Found**: Group does not exist
  ```json
  {
    "error": "NotFound",
    "message": "Group not found"
  }
  ```

## 5. Data Flow

```
1. HTTP Request arrives at minimal API endpoint
   ↓
2. Endpoint extracts groupId from route and userId from JWT claims
   ↓
3. Create GetBudgetSuggestionsQuery and send to MediatR
   ↓
4. Handler validates group exists (via repository)
   ↓
5. Handler verifies user is organizer (compare OrganizerUserId)
   ↓
6. Handler queries GroupParticipants with:
   - Filter: GroupId = groupId
   - Select: BudgetSuggestion (where not null)
   - Count: Total participants
   ↓
7. Handler sorts suggestions in ascending order
   ↓
8. Handler retrieves current budget from Group entity
   ↓
9. Handler maps data to BudgetSuggestionsResponse DTO
   ↓
10. Return 200 OK with response body
```

### Database Interactions

- **Query 1**: Retrieve Group entity
  ```csharp
  var group = await _context.Groups
      .AsNoTracking()
      .FirstOrDefaultAsync(g => g.Id == groupId);
  ```

- **Query 2**: Retrieve budget suggestions and participant count
  ```csharp
  var participants = await _context.GroupParticipants
      .AsNoTracking()
      .Where(gp => gp.GroupId == groupId)
      .Select(gp => gp.BudgetSuggestion)
      .ToListAsync();
  ```

## 6. Security Considerations

### Authentication
- Validate JWT token via ASP.NET Core authentication middleware
- Extract `userId` claim from validated token (ClaimTypes.NameIdentifier)
- Return 401 if token is missing or invalid

### Authorization
- Compare authenticated userId with Group.OrganizerUserId
- Return 403 Forbidden if user is not the organizer
- Do not expose whether the group exists if user is not authorized (return 404 for both cases)

### Data Privacy
- **Critical**: Ensure no user identities are included in the response
- Only return budget amounts, never include UserId or user names
- Suggestions array should contain only decimal values

### Input Validation
- Validate groupId is a valid GUID format
- Validate groupId is not empty or null

## 7. Performance Considerations

### Optimization Strategies

1. **Use AsNoTracking()**: Since this is a read-only operation, disable change tracking
   ```csharp
   .AsNoTracking()
   ```

2. **Consider Single Query**: Potentially combine group and participant queries using Include()
   ```csharp
   var group = await _context.Groups
       .AsNoTracking()
       .Include(g => g.Participants)
       .FirstOrDefaultAsync(g => g.Id == groupId);
   ```

3. **In-Memory Sorting**: Sort the suggestions list in memory after retrieval (small dataset)

4. **Projection**: Use Select() to retrieve only BudgetSuggestion field, not entire entities

### Expected Performance
- **Database Queries**: 1-2 queries (optimized) or 2 queries (separate)
- **Response Time**: < 100ms for groups with up to 30 participants
- **Data Volume**: Minimal (array of decimals + metadata)

### Potential Bottlenecks
- None expected for MVP scope (small group sizes of 5-30 participants)
- If group sizes grow significantly, consider caching organizer checks

## 8. Error Handling

### Validation Errors
- **Invalid GUID format**: Return 400 Bad Request (handled by ASP.NET Core routing)

### Authentication Errors
- **Missing token**: Return 401 Unauthorized (handled by authentication middleware)
- **Invalid/expired token**: Return 401 Unauthorized (handled by authentication middleware)

### Authorization Errors
- **User not organizer**: Return 403 Forbidden with clear message

### Not Found Errors
- **Group does not exist**: Return 404 Not Found
- **Security consideration**: Return 404 (not 403) if group doesn't exist AND user is not authorized

### Server Errors
- **Database connection failure**: Return 500 Internal Server Error
- **Unexpected exceptions**: Log error details, return generic 500 message to client

## 9. Implementation Steps

### Step 1: Create Feature Folder Structure
```
SantaVibe.Api/
└── Features/
    └── Groups/
        └── GetBudgetSuggestions/
            ├── GetBudgetSuggestionsEndpoint.cs
            ├── GetBudgetSuggestionsQuery.cs
            ├── GetBudgetSuggestionsHandler.cs
            └── BudgetSuggestionsResponse.cs
```

### Step 2: Define Response DTO
- Create `BudgetSuggestionsResponse.cs` with required properties
- Use `init` accessors for immutability
- Add `required` keyword for non-nullable properties
- Include XML documentation comments

### Step 3: Define Query Record
- Create `GetBudgetSuggestionsQuery.cs` record
- Include `GroupId` (Guid) and `UserId` (string) properties
- Implement `IRequest<BudgetSuggestionsResponse>`

### Step 4: Implement Query Handler
- Create `GetBudgetSuggestionsHandler.cs` class
- Implement `IRequestHandler<GetBudgetSuggestionsQuery, BudgetSuggestionsResponse>`
- Inject `ApplicationDbContext` (or repository) via constructor
- Implement validation logic:
  - Check if group exists
  - Verify user is organizer
  - Throw appropriate exceptions for error cases
- Implement data retrieval logic:
  - Query GroupParticipants for budget suggestions
  - Count total participants
  - Filter and collect non-null budget suggestions
  - Sort suggestions ascending
  - Retrieve current budget from Group
- Map data to response DTO

### Step 5: Create Minimal API Endpoint
- Create `GetBudgetSuggestionsEndpoint.cs`
- Define endpoint using `MapGet()`
- Apply `[Authorize]` attribute for authentication
- Extract `groupId` from route parameter
- Extract `userId` from `ClaimsPrincipal` (HttpContext.User)
- Send query to MediatR mediator
- Handle exceptions and map to appropriate HTTP status codes:
  - `UnauthorizedAccessException` → 403 Forbidden
  - `KeyNotFoundException` → 404 Not Found
  - Generic exceptions → 500 Internal Server Error
- Return `Results.Ok()` with response

### Step 6: Register Endpoint in Program.cs
- Add endpoint registration in `Program.cs`:
  ```csharp
  app.MapGet("/api/groups/{groupId}/budget/suggestions", GetBudgetSuggestionsEndpoint.Handle)
      .RequireAuthorization();
  ```

### Step 7: Add Unit Tests
- Test handler with valid organizer request
- Test handler with non-organizer request (should throw)
- Test handler with non-existent group (should throw)
- Test suggestions sorting (ascending order)
- Test participant count vs suggestions received calculation
- Test with null budget suggestions (should be excluded)

### Step 8: Add Integration Tests
- Test full endpoint with authenticated organizer
- Test endpoint with authenticated non-organizer (expect 403)
- Test endpoint with unauthenticated request (expect 401)
- Test endpoint with invalid groupId (expect 404)
- Test response structure and data accuracy
- Test with various budget suggestion scenarios (all null, some null, all provided)

### Step 9: Implement Logging
- Add structured logging in handler:
  - Log query execution start
  - Log authorization check
  - Log data retrieval
  - Log any exceptions with contextual information
- Use Serilog with appropriate log levels (Information, Warning, Error)

### Step 10: Review and Refine
- Ensure code follows backend guidelines (Clean Architecture, DDD principles)
- Verify vertical slice organization
- Check that user identities are not exposed in response
- Validate error messages are user-friendly
- Review performance with AsNoTracking() and optimized queries
- Ensure proper exception handling throughout

## 10. Testing Checklist

### Unit Tests
- [ ] Handler returns correct response for valid organizer request
- [ ] Handler throws UnauthorizedAccessException for non-organizer
- [ ] Handler throws KeyNotFoundException for non-existent group
- [ ] Suggestions are sorted in ascending order
- [ ] ParticipantCount includes all participants
- [ ] SuggestionsReceived counts only non-null suggestions
- [ ] Null budget suggestions are excluded from array
- [ ] Current budget is correctly retrieved from Group entity

### Integration Tests
- [ ] Endpoint returns 200 OK with correct data for organizer
- [ ] Endpoint returns 401 Unauthorized without JWT token
- [ ] Endpoint returns 403 Forbidden for non-organizer participant
- [ ] Endpoint returns 404 Not Found for non-existent group
- [ ] Response structure matches specification
- [ ] Suggestions array contains only decimal values (no user data)
- [ ] Count equals length of suggestions array
- [ ] SuggestionsReceived equals count of non-null suggestions

### Security Tests
- [ ] No user identities exposed in response
- [ ] JWT claims correctly extracted
- [ ] Authorization check prevents non-organizers from accessing
- [ ] Invalid tokens are rejected

### Performance Tests
- [ ] Response time < 100ms for group with 30 participants
- [ ] Queries use AsNoTracking() for read-only operations
- [ ] No N+1 query issues

## 11. Additional Notes

- **Future Enhancement**: Consider caching budget suggestions if frequently accessed
- **Accessibility**: Organizer can access this endpoint multiple times to review suggestions before setting final budget
- **Business Logic**: This endpoint is only useful before draw execution; after draw, the final budget is already set
- **Related Endpoints**:
  - `POST /api/groups/{groupId}/draw` - Sets the final budget during draw execution
  - `PUT /api/groups/{groupId}/participants/me/budget-suggestion` - Participants update their suggestions

---

**Implementation Priority**: Medium (needed before draw execution feature)

**Estimated Effort**: 4-6 hours (including tests and documentation)
