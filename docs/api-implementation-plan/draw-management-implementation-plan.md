# API Endpoint Implementation Plan: Draw Management

## 1. Endpoint Overview

This plan covers two related endpoints for managing the Secret Santa draw process:

1. **Validate Draw Feasibility** (`GET /api/groups/{groupId}/draw/validate`) - Provides real-time validation feedback to organizers before executing the draw, checking participant count and exclusion rule feasibility.

2. **Execute Draw** (`POST /api/groups/{groupId}/draw`) - Executes the irreversible Secret Santa draw algorithm, creating assignments for all participants while respecting exclusion rules and setting the final budget.

Both endpoints require JWT authentication and organizer authorization. The validation endpoint is designed for UI feedback, while the execution endpoint performs the actual draw within a database transaction.

## 2. Request Details

### Validate Draw Endpoint

- **HTTP Method**: GET
- **URL Structure**: `/api/groups/{groupId}/draw/validate`
- **Parameters**:
  - **Required**:
    - `groupId` (path, UUID) - Group identifier
  - **Optional**: None
- **Request Body**: None
- **Headers**:
  - `Authorization: Bearer {JWT_TOKEN}` (required)

### Execute Draw Endpoint

- **HTTP Method**: POST
- **URL Structure**: `/api/groups/{groupId}/draw`
- **Parameters**:
  - **Required**:
    - `groupId` (path, UUID) - Group identifier
  - **Optional**: None
- **Request Body**:
```json
{
  "budget": 100.00
}
```
- **Request Body Validation**:
  - `budget`: Required, decimal, min 0.01, max 99999999.99, exactly 2 decimal places
- **Headers**:
  - `Authorization: Bearer {JWT_TOKEN}` (required)
  - `Content-Type: application/json` (required)

## 3. Used Types

### DTOs (SantaVibe.Api/Features/Groups/ValidateDraw/ and ExecuteDraw/)

```csharp
// ValidateDraw/ValidateDrawResponse.cs
public sealed record ValidateDrawResponse(
    Guid GroupId,
    bool IsValid,
    bool CanDraw,
    int ParticipantCount,
    int ExclusionRuleCount,
    List<string> Errors,
    List<string> Warnings
);

// ExecuteDraw/ExecuteDrawRequest.cs
public sealed record ExecuteDrawRequest(
    decimal Budget
);

// ExecuteDraw/ExecuteDrawResponse.cs
public sealed record ExecuteDrawResponse(
    Guid GroupId,
    decimal Budget,
    bool DrawCompleted,
    DateTime DrawCompletedAt,
    int ParticipantCount,
    int AssignmentsCreated,
    int EmailNotificationsScheduled,
    AssignmentDto MyAssignment
);

public sealed record AssignmentDto(
    string RecipientId,
    string RecipientFirstName,
    string RecipientLastName
);
```

### MediatR Commands/Queries

```csharp
// ValidateDraw/ValidateDrawQuery.cs
public sealed record ValidateDrawQuery(
    Guid GroupId,
    string UserId
) : IRequest<Result<ValidateDrawResponse>>;

// ValidateDraw/ValidateDrawHandler.cs
public sealed class ValidateDrawHandler(
    IRepository<Group> groupRepository,
    IRepository<GroupParticipant> participantRepository,
    IRepository<ExclusionRule> exclusionRuleRepository,
    IDrawAlgorithmService drawAlgorithmService,
    ILogger<ValidateDrawHandler> logger
) : IRequestHandler<ValidateDrawQuery, Result<ValidateDrawResponse>>;

// ExecuteDraw/ExecuteDrawCommand.cs
public sealed record ExecuteDrawCommand(
    Guid GroupId,
    string UserId,
    decimal Budget
) : IRequest<Result<ExecuteDrawResponse>>;

// ExecuteDraw/ExecuteDrawHandler.cs
public sealed class ExecuteDrawHandler(
    IRepository<Group> groupRepository,
    IRepository<GroupParticipant> participantRepository,
    IRepository<ExclusionRule> exclusionRuleRepository,
    IRepository<Assignment> assignmentRepository,
    IRepository<EmailNotification> emailNotificationRepository,
    IDrawAlgorithmService drawAlgorithmService,
    IUnitOfWork unitOfWork,
    ILogger<ExecuteDrawHandler> logger
) : IRequestHandler<ExecuteDrawCommand, Result<ExecuteDrawResponse>>;
```

### Domain Service Interface

```csharp
// Domain/Services/IDrawAlgorithmService.cs
public interface IDrawAlgorithmService
{
    /// <summary>
    /// Validates whether a valid assignment graph can be created with given constraints
    /// </summary>
    DrawValidationResult ValidateDrawFeasibility(
        List<Guid> participantIds,
        List<(Guid userId1, Guid userId2)> exclusionPairs
    );

    /// <summary>
    /// Executes the draw algorithm to generate assignments
    /// </summary>
    /// <returns>Dictionary mapping each Santa to their recipient</returns>
    Dictionary<Guid, Guid> ExecuteDrawAlgorithm(
        List<Guid> participantIds,
        List<(Guid userId1, Guid userId2)> exclusionPairs
    );
}

public sealed record DrawValidationResult(
    bool IsValid,
    List<string> Errors
);
```

### FluentValidation Validator

```csharp
// ExecuteDraw/ExecuteDrawRequestValidator.cs
public sealed class ExecuteDrawRequestValidator : AbstractValidator<ExecuteDrawRequest>
{
    public ExecuteDrawRequestValidator()
    {
        RuleFor(x => x.Budget)
            .NotEmpty()
            .WithMessage("Budget is required")
            .GreaterThanOrEqualTo(0.01m)
            .WithMessage("Budget must be at least 0.01")
            .LessThanOrEqualTo(99999999.99m)
            .WithMessage("Budget must not exceed 99999999.99")
            .ScalePrecision(2, 10)
            .WithMessage("Budget must have at most 2 decimal places");
    }
}
```

## 4. Response Details

### Validate Draw Endpoint

**Success Response (200 OK) - Valid:**
```json
{
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "isValid": true,
  "canDraw": true,
  "participantCount": 5,
  "exclusionRuleCount": 2,
  "errors": [],
  "warnings": []
}
```

**Success Response (200 OK) - Invalid:**
```json
{
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "isValid": false,
  "canDraw": false,
  "participantCount": 2,
  "exclusionRuleCount": 1,
  "errors": [
    "Minimum 3 participants required for draw",
    "Current exclusion rules prevent valid assignments"
  ],
  "warnings": []
}
```

**Error Responses:**
- `401 Unauthorized` - Missing or invalid JWT token (handled by authentication middleware)
- `403 Forbidden` - User is not the group organizer
- `404 Not Found` - Group does not exist

### Execute Draw Endpoint

**Success Response (200 OK):**
```json
{
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "budget": 100.00,
  "drawCompleted": true,
  "drawCompletedAt": "2025-10-20T15:30:00Z",
  "participantCount": 5,
  "assignmentsCreated": 5,
  "emailNotificationsScheduled": 5,
  "myAssignment": {
    "recipientId": "770g0622-g41d-63g6-c938-668877662222",
    "recipientFirstName": "Maria",
    "recipientLastName": "Wiśniewska"
  }
}
```

**Error Responses:**
- `400 Bad Request - ValidationError`:
```json
{
  "error": "ValidationError",
  "message": "Budget is required",
  "details": {
    "budget": ["Budget must be between 0.01 and 99999999.99"]
  }
}
```

- `400 Bad Request - DrawValidationFailed`:
```json
{
  "error": "DrawValidationFailed",
  "message": "Cannot execute draw: validation errors found",
  "details": {
    "errors": ["Minimum 3 participants required"]
  }
}
```

- `400 Bad Request - DrawAlreadyCompleted`:
```json
{
  "error": "DrawAlreadyCompleted",
  "message": "Draw has already been completed for this group"
}
```

- `401 Unauthorized` - Missing or invalid JWT token
- `403 Forbidden` - User is not the organizer
- `404 Not Found` - Group does not exist
- `500 Internal Server Error - DrawExecutionFailed`:
```json
{
  "error": "DrawExecutionFailed",
  "message": "An unexpected error occurred during draw execution. Please contact support."
}
```

## 5. Data Flow

### Validate Draw Flow

```
1. Client Request
   ↓
2. Authentication Middleware (JWT validation)
   ↓
3. Minimal API Endpoint
   ↓
4. Send ValidateDrawQuery to MediatR
   ↓
5. ValidateDrawHandler
   ├─→ Query Group (with organizer check)
   ├─→ Query GroupParticipants (count)
   ├─→ Query ExclusionRules
   ├─→ Check if draw already completed
   ├─→ Validate minimum 3 participants
   └─→ Call DrawAlgorithmService.ValidateDrawFeasibility()
   ↓
6. DrawAlgorithmService (Domain Layer)
   ├─→ Build constraint graph
   ├─→ Check for impossible configurations
   └─→ Return validation result
   ↓
7. Handler builds ValidateDrawResponse
   ↓
8. Return 200 OK with validation result
```

### Execute Draw Flow

```
1. Client Request (with budget)
   ↓
2. Authentication Middleware (JWT validation)
   ↓
3. Minimal API Endpoint
   ↓
4. FluentValidation (validate budget)
   ↓
5. Send ExecuteDrawCommand to MediatR
   ↓
6. ExecuteDrawHandler
   ├─→ BEGIN TRANSACTION
   ├─→ Query Group (with organizer check)
   ├─→ Verify draw not completed
   ├─→ Query GroupParticipants (get all participant IDs)
   ├─→ Query ExclusionRules
   ├─→ Validate preconditions (min 3 participants, etc.)
   ├─→ Call DrawAlgorithmService.ExecuteDrawAlgorithm()
   │   └─→ Returns Dictionary<Guid, Guid> (Santa → Recipient)
   ├─→ Update Group.Budget
   ├─→ Update Group.DrawCompletedAt = DateTime.UtcNow
   ├─→ Create Assignment records for each Santa→Recipient pair
   ├─→ Create EmailNotification records (DrawCompleted type) for all participants
   ├─→ COMMIT TRANSACTION
   └─→ Query organizer's assignment to return in response
   ↓
7. Return 200 OK with draw result
```

**Transaction Boundary:**
The execute draw operation must be atomic:
- Budget update
- DrawCompletedAt timestamp
- All assignment creations
- All email notification scheduling

If any step fails, the entire transaction must be rolled back.

## 6. Security Considerations

### Authentication
- Both endpoints require valid JWT Bearer token
- Token validated by ASP.NET Core authentication middleware
- User identity extracted from JWT claims (`userId`)

### Authorization
- **Organizer-Only Access**: Both endpoints require user to be the group organizer
- Authorization check performed in handler:
  ```csharp
  if (group.OrganizerId != query.UserId)
  {
      return Result<ValidateDrawResponse>.Forbidden("Only the group organizer can validate/execute the draw");
  }
  ```

### Data Validation
- **Input Validation**: FluentValidation for budget validation
- **Business Rule Validation**: Minimum participants, exclusion rules, draw not completed
- **SQL Injection Prevention**: Use EF Core parameterized queries
- **UUID Validation**: Route parameter binding validates UUID format

### Privacy
- Execute draw response only returns organizer's own assignment
- Other participants' assignments are never exposed to organizer
- Email notifications sent asynchronously (not exposed in response)

## 7. Error Handling

### Error Scenarios

| Error Scenario | Status Code | Error Response | Logging Level |
|----------------|-------------|----------------|---------------|
| Missing/invalid JWT token | 401 | N/A (middleware) | Warning |
| User is not organizer | 403 | Forbidden | Warning |
| Group not found | 404 | NotFound | Warning |
| Budget validation failed | 400 | ValidationError | Warning |
| Draw already completed | 400 | DrawAlreadyCompleted | Warning |
| Less than 3 participants | 400 | DrawValidationFailed | Warning |
| Exclusion rules prevent draw | 400 | DrawValidationFailed | Warning |
| Draw algorithm failure | 500 | DrawExecutionFailed | Error |
| Database transaction failure | 500 | InternalServerError | Error |
| Email scheduling failure | 500 | InternalServerError | Error |

### Error Handling Implementation

**In Handler:**
```csharp
try
{
    await unitOfWork.BeginTransactionAsync();

    // ... business logic ...

    await unitOfWork.CommitAsync();
}
catch (DrawAlgorithmException ex)
{
    await unitOfWork.RollbackAsync();
    logger.LogError(ex, "Draw algorithm failed for group {GroupId}", groupId);
    return Result<ExecuteDrawResponse>.Failure("DrawExecutionFailed",
        "An unexpected error occurred during draw execution. Please contact support.");
}
catch (Exception ex)
{
    await unitOfWork.RollbackAsync();
    logger.LogError(ex, "Unexpected error executing draw for group {GroupId}", groupId);
    return Result<ExecuteDrawResponse>.Failure("InternalServerError",
        "An unexpected error occurred. Please try again later.");
}
```

**Error Response Format:**
All error responses follow the standard error format defined in API plan:
```csharp
public sealed record ErrorResponse(
    string Error,
    string Message,
    Dictionary<string, List<string>>? Details = null
);
```

### Logging Strategy

**Structured Logging with Serilog:**
- Include `groupId`, `userId`, `participantCount`, `exclusionRuleCount` in all logs
- Log validation failures: `logger.LogWarning("Draw validation failed for group {GroupId}: {Errors}", groupId, errors)`
- Log algorithm execution: `logger.LogInformation("Executing draw for group {GroupId} with {ParticipantCount} participants", groupId, count)`
- Log successful draw: `logger.LogInformation("Draw completed for group {GroupId}, created {AssignmentCount} assignments", groupId, count)`
- Log failures: `logger.LogError(ex, "Draw execution failed for group {GroupId}", groupId)`

## 8. Performance Considerations

### Potential Bottlenecks

1. **Graph Validation Algorithm**: For groups with many participants and complex exclusion rules, validating feasibility could be computationally expensive
   - **Mitigation**: Implement efficient graph algorithm (e.g., maximum matching, Hall's theorem)
   - **Limit**: Consider adding warning for groups with >30 participants

2. **Database Queries**: Multiple queries to fetch participants, exclusion rules, and assignments
   - **Mitigation**: Use `.Include()` for eager loading related entities
   - **Mitigation**: Consider single query with projections instead of multiple round trips

3. **Transaction Lock Duration**: Long-running transaction during draw execution
   - **Mitigation**: Keep transaction scope minimal - only include database writes
   - **Mitigation**: Perform draw algorithm calculation outside transaction, only lock for writes

4. **Email Notification Creation**: Creating N notification records (one per participant)
   - **Mitigation**: Use bulk insert with `AddRange()` instead of individual `Add()` calls
   - **Optimization**: Consider using stored procedure for bulk operations

### Optimization Strategies

**Query Optimization:**
```csharp
// Efficient single query with eager loading
var group = await groupRepository
    .Query()
    .Include(g => g.Participants)
    .Include(g => g.ExclusionRules)
    .FirstOrDefaultAsync(g => g.Id == groupId);
```

**Bulk Insert Optimization:**
```csharp
// Use AddRange for bulk operations
var assignments = santaToRecipientMap.Select(pair =>
    new Assignment
    {
        GroupId = groupId,
        SantaUserId = pair.Key,
        RecipientUserId = pair.Value
    }
).ToList();

await assignmentRepository.AddRangeAsync(assignments);
```

**Algorithm Complexity:**
- Target: O(n²) or better for n participants
- Maximum realistic group size: 30-50 participants
- Timeout: Consider adding timeout (5 seconds) for draw algorithm

## 9. Implementation Steps

### Step 1: Create Domain Service Interface and Implementation

**Location**: `SantaVibe.Api/Domain/Services/`

1. Create `IDrawAlgorithmService.cs` interface
2. Create `DrawAlgorithmService.cs` implementation
3. Implement graph-based validation algorithm:
   - Build directed graph with exclusion constraints
   - Check for valid Hamiltonian path/cycle possibility
   - Use Hall's marriage theorem or maximum matching
4. Implement randomized assignment algorithm:
   - Shuffle participants for randomization
   - Use backtracking or constraint satisfaction
   - Ensure no self-assignments
   - Ensure no 2-person circles (A→B and B→A)
   - Respect all exclusion rules
5. Add unit tests for draw algorithm edge cases

**Files to create:**
- `Domain/Services/IDrawAlgorithmService.cs`
- `Domain/Services/DrawAlgorithmService.cs`
- `Domain/Services/DrawAlgorithmServiceTests.cs` (test project)

### Step 2: Create Validate Draw Feature (Vertical Slice)

**Location**: `SantaVibe.Api/Features/Groups/ValidateDraw/`

1. Create `ValidateDrawResponse.cs` DTO
2. Create `ValidateDrawQuery.cs` MediatR query
3. Create `ValidateDrawHandler.cs` with:
   - Repository injections (Group, GroupParticipant, ExclusionRule)
   - DrawAlgorithmService injection
   - Logger injection
4. Implement handler logic:
   - Query group with eager loading
   - Check user is organizer (403 if not)
   - Check group exists (404 if not)
   - Check if draw already completed
   - Count participants (error if < 3)
   - Get exclusion rules
   - Call DrawAlgorithmService.ValidateDrawFeasibility()
   - Build response with errors/warnings
5. Create minimal API endpoint in `Endpoints.cs`:
   - Route: `GET /api/groups/{groupId}/draw/validate`
   - Add `[Authorize]` attribute
   - Send query to MediatR
   - Map result to HTTP response

**Files to create:**
- `Features/Groups/ValidateDraw/ValidateDrawResponse.cs`
- `Features/Groups/ValidateDraw/ValidateDrawQuery.cs`
- `Features/Groups/ValidateDraw/ValidateDrawHandler.cs`
- Update `Features/Groups/Endpoints.cs`

### Step 3: Create Execute Draw Feature (Vertical Slice)

**Location**: `SantaVibe.Api/Features/Groups/ExecuteDraw/`

1. Create `ExecuteDrawRequest.cs` DTO
2. Create `ExecuteDrawRequestValidator.cs` with FluentValidation
3. Create `ExecuteDrawResponse.cs` and `AssignmentDto.cs` DTOs
4. Create `ExecuteDrawCommand.cs` MediatR command
5. Create `ExecuteDrawHandler.cs` with:
   - All necessary repository injections
   - DrawAlgorithmService injection
   - IUnitOfWork injection for transaction
   - Logger injection
6. Implement handler logic following data flow in section 5
7. Create minimal API endpoint in `Endpoints.cs`:
   - Route: `POST /api/groups/{groupId}/draw`
   - Add `[Authorize]` attribute
   - Validate request body with FluentValidation
   - Send command to MediatR
   - Map result to HTTP response

**Files to create:**
- `Features/Groups/ExecuteDraw/ExecuteDrawRequest.cs`
- `Features/Groups/ExecuteDraw/ExecuteDrawRequestValidator.cs`
- `Features/Groups/ExecuteDraw/ExecuteDrawResponse.cs`
- `Features/Groups/ExecuteDraw/ExecuteDrawCommand.cs`
- `Features/Groups/ExecuteDraw/ExecuteDrawHandler.cs`
- Update `Features/Groups/Endpoints.cs`

### Step 4: Register Dependencies in DI Container

**Location**: `SantaVibe.Api/Program.cs` or `ServiceCollectionExtensions.cs`

1. Register `IDrawAlgorithmService` with scoped lifetime:
   ```csharp
   services.AddScoped<IDrawAlgorithmService, DrawAlgorithmService>();
   ```
2. Register FluentValidation validators:
   ```csharp
   services.AddValidatorsFromAssemblyContaining<ExecuteDrawRequestValidator>();
   ```
3. Ensure MediatR is registered with handlers from assembly
4. Ensure repositories and unit of work are registered

### Step 5: Implement Error Handling Middleware/Filter

**Location**: `SantaVibe.Api/Middleware/` or use endpoint filters

1. Create endpoint filter for consistent error response mapping
2. Map Result<T> failures to appropriate HTTP status codes
3. Format error responses according to API specification
4. Add structured logging for errors

**Alternative**: Use built-in problem details middleware with custom configuration

### Step 6: Create Integration Tests

**Location**: `SantaVibe.Api.Tests/Features/Groups/`

1. Create `ValidateDrawTests.cs`:
   - Test successful validation (valid group)
   - Test validation failure (< 3 participants)
   - Test validation failure (impossible exclusion rules)
   - Test 403 for non-organizer
   - Test 404 for non-existent group
   - Test validation returns false if draw already completed

2. Create `ExecuteDrawTests.cs`:
   - Test successful draw execution
   - Test budget validation errors
   - Test draw already completed error
   - Test minimum participants error
   - Test exclusion rules validation
   - Test 403 for non-organizer
   - Test 404 for non-existent group
   - Test transaction rollback on failure
   - Test email notifications are created
   - Test organizer's assignment is returned
   - Use TestContainers for integration testing with real PostgreSQL

**Files to create:**
- `Tests/Features/Groups/ValidateDrawTests.cs`
- `Tests/Features/Groups/ExecuteDrawTests.cs`

### Step 7: Update OpenAPI/Swagger Documentation

**Location**: `SantaVibe.Api/Program.cs`

1. Add XML comments to endpoint definitions
2. Configure Swagger to show request/response examples
3. Document authentication requirements
4. Document all possible error responses with examples
5. Add operation descriptions from API specification

### Step 8: Manual Testing and Refinement

1. Test with Postman/Thunder Client:
   - Valid draw scenarios
   - All error scenarios
   - Authorization edge cases
2. Verify transaction rollback behavior
3. Check email notifications are scheduled correctly
4. Verify logging output is structured correctly
5. Performance test with 30+ participant groups
6. Test randomization (execute draw multiple times, verify different results)

### Step 9: Code Review Checklist

- [ ] All validation rules from API spec implemented
- [ ] Transaction boundary correctly implemented
- [ ] Error responses match API specification exactly
- [ ] Logging includes all relevant context (groupId, userId, etc.)
- [ ] Authorization checks prevent non-organizers from access
- [ ] Draw algorithm respects all constraints (no self-assignment, no 2-person circles, exclusion rules)
- [ ] Budget is set correctly during draw execution
- [ ] Email notifications are scheduled for all participants
- [ ] Only organizer's assignment is returned in response
- [ ] Unit tests cover edge cases
- [ ] Integration tests use TestContainers
- [ ] OpenAPI documentation is complete and accurate

### Step 10: Documentation and Deployment

1. Update API documentation with endpoint details
2. Create ADR (Architecture Decision Record) for draw algorithm choice
3. Document draw algorithm complexity and limitations
4. Add monitoring/alerting for draw execution failures
5. Deploy to staging environment
6. Verify in staging with real-world scenarios
7. Deploy to production

---

**Estimated Implementation Time**: 2-3 days for experienced developer
- Step 1 (Draw algorithm): 6-8 hours
- Steps 2-3 (Endpoints): 4-6 hours
- Steps 4-5 (Configuration): 1-2 hours
- Step 6 (Tests): 4-6 hours
- Steps 7-10 (Documentation and deployment): 2-3 hours

**Critical Path**: Draw algorithm implementation → Execute draw handler → Integration tests
