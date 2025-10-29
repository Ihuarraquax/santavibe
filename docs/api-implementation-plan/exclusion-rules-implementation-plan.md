# API Endpoint Implementation Plan: Exclusion Rules Management

## 1. Endpoint Overview

This plan covers the implementation of three endpoints for managing exclusion rules in Secret Santa groups. Exclusion rules prevent specific pairs of participants from drawing each other during the Secret Santa assignment process.

**Endpoints:**
1. **GET** `/api/groups/{groupId}/exclusion-rules` - Retrieve all exclusion rules for a group
2. **POST** `/api/groups/{groupId}/exclusion-rules` - Create a new exclusion rule
3. **DELETE** `/api/groups/{groupId}/exclusion-rules/{ruleId}` - Delete an exclusion rule

**Key Business Rules:**
- Only the group organizer can manage exclusion rules
- Exclusion rules can only be created/deleted before the draw is completed
- Creating an exclusion rule must not make a valid draw impossible (validated in real-time)
- Exclusion rules are bidirectional (if A cannot draw B, then B cannot draw A)

---

## 2. Request Details

### 2.1 GET Exclusion Rules

- **HTTP Method:** GET
- **URL Structure:** `/api/groups/{groupId}/exclusion-rules`
- **Parameters:**
  - Required:
    - `groupId` (path, UUID): Group identifier
  - Optional: None
- **Request Body:** None
- **Headers:**
  - `Authorization: Bearer {jwt_token}` (required)

### 2.2 CREATE Exclusion Rule

- **HTTP Method:** POST
- **URL Structure:** `/api/groups/{groupId}/exclusion-rules`
- **Parameters:**
  - Required:
    - `groupId` (path, UUID): Group identifier
- **Request Body:**
  ```json
  {
    "userId1": "550e8400-e29b-41d4-a716-446655440000",
    "userId2": "770g0622-g41d-63g6-c938-668877662222"
  }
  ```
- **Headers:**
  - `Authorization: Bearer {jwt_token}` (required)
  - `Content-Type: application/json` (required)

### 2.3 DELETE Exclusion Rule

- **HTTP Method:** DELETE
- **URL Structure:** `/api/groups/{groupId}/exclusion-rules/{ruleId}`
- **Parameters:**
  - Required:
    - `groupId` (path, UUID): Group identifier
    - `ruleId` (path, UUID): Exclusion rule identifier
  - Optional: None
- **Request Body:** None
- **Headers:**
  - `Authorization: Bearer {jwt_token}` (required)

---

## 3. Used Types

### 3.1 Query/Command Models

**GetExclusionRulesQuery.cs** (Features/ExclusionRules/GetExclusionRules/)
```csharp
public record GetExclusionRulesQuery(
    Guid GroupId,
    string CurrentUserId
) : IRequest<Result<GetExclusionRulesResponse>>;
```

**CreateExclusionRuleCommand.cs** (Features/ExclusionRules/CreateExclusionRule/)
```csharp
public record CreateExclusionRuleCommand(
    Guid GroupId,
    string UserId1,
    string UserId2,
    string CurrentUserId
) : IRequest<Result<CreateExclusionRuleResponse>>;
```

**DeleteExclusionRuleCommand.cs** (Features/ExclusionRules/DeleteExclusionRule/)
```csharp
public record DeleteExclusionRuleCommand(
    Guid GroupId,
    Guid RuleId,
    string CurrentUserId
) : IRequest<Result>;
```

### 3.2 Response DTOs

**GetExclusionRulesResponse.cs**
```csharp
public record GetExclusionRulesResponse(
    Guid GroupId,
    List<ExclusionRuleDto> ExclusionRules,
    int TotalCount
);

public record ExclusionRuleDto(
    Guid RuleId,
    UserInfoDto User1,
    UserInfoDto User2,
    DateTime CreatedAt
);

public record UserInfoDto(
    string UserId,
    string FirstName,
    string LastName
);
```

**CreateExclusionRuleResponse.cs**
```csharp
public record CreateExclusionRuleResponse(
    Guid RuleId,
    Guid GroupId,
    UserInfoDto User1,
    UserInfoDto User2,
    DateTime CreatedAt,
    DrawValidationDto DrawValidation
);

public record DrawValidationDto(
    bool IsValid,
    List<string> Errors
);
```

### 3.3 Request DTOs

**CreateExclusionRuleRequest.cs**
```csharp
public record CreateExclusionRuleRequest(
    string UserId1,
    string UserId2
);
```

---

## 4. Response Details

### 4.1 GET Exclusion Rules

**Success Response (200 OK):**
```json
{
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "exclusionRules": [
    {
      "ruleId": "1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d",
      "user1": {
        "userId": "550e8400-e29b-41d4-a716-446655440000",
        "firstName": "Jan",
        "lastName": "Kowalski"
      },
      "user2": {
        "userId": "770g0622-g41d-63g6-c938-668877662222",
        "firstName": "Maria",
        "lastName": "Kowalska"
      },
      "createdAt": "2025-10-15T15:00:00Z"
    }
  ],
  "totalCount": 1
}
```

### 4.2 CREATE Exclusion Rule

**Success Response (201 Created):**
```json
{
  "ruleId": "1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d",
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "user1": {
    "userId": "550e8400-e29b-41d4-a716-446655440000",
    "firstName": "Jan",
    "lastName": "Kowalski"
  },
  "user2": {
    "userId": "770g0622-g41d-63g6-c938-668877662222",
    "firstName": "Maria",
    "lastName": "Kowalska"
  },
  "createdAt": "2025-10-15T15:00:00Z",
  "drawValidation": {
    "isValid": true,
    "errors": []
  }
}
```

### 4.3 DELETE Exclusion Rule

**Success Response (204 No Content):** Empty body

---

## 5. Data Flow

### 5.1 GET Exclusion Rules Flow

```
1. Client sends GET request with JWT token and groupId
2. Minimal API endpoint extracts groupId from route
3. JWT middleware validates token and extracts userId
4. Endpoint sends GetExclusionRulesQuery to MediatR
5. Handler validates:
   - Group exists
   - User is organizer of the group
6. Handler queries ExclusionRules with Include for Users
7. Handler maps entities to DTOs with user information
8. Return 200 OK with response
```

**Database Queries:**
- Check if group exists: `Groups.FindAsync(groupId)`
- Verify organizer: Check `group.OrganizerUserId == currentUserId`
- Fetch exclusion rules with users:
  ```csharp
  _context.ExclusionRules
    .Include(er => er.User1)
    .Include(er => er.User2)
    .Where(er => er.GroupId == groupId)
    .OrderBy(er => er.CreatedAt)
    .ToListAsync()
  ```

### 5.2 CREATE Exclusion Rule Flow

```
1. Client sends POST request with JWT token, groupId, and request body
2. Minimal API endpoint extracts groupId and validates request body
3. JWT middleware validates token and extracts userId
4. Endpoint sends CreateExclusionRuleCommand to MediatR
5. Handler validates:
   - Group exists
   - User is organizer
   - Draw not completed
   - userId1 ≠ userId2
   - Both users are participants
   - Rule doesn't already exist
6. Handler creates exclusion rule entity
7. Handler validates draw feasibility with DrawValidationService
8. If validation passes:
   - Save exclusion rule to database
   - Fetch user details
   - Return 201 Created with response
9. If validation fails:
   - Return 400 Bad Request with specific error
```

**Database Operations:**
- Check group: `Groups.FindAsync(groupId)`
- Check participants:
  ```csharp
  _context.GroupParticipants
    .Where(gp => gp.GroupId == groupId &&
                 (gp.UserId == userId1 || gp.UserId == userId2))
    .CountAsync()
  ```
- Check for duplicate:
  ```csharp
  _context.ExclusionRules
    .AnyAsync(er => er.GroupId == groupId &&
                    ((er.UserId1 == userId1 && er.UserId2 == userId2) ||
                     (er.UserId1 == userId2 && er.UserId2 == userId1)))
  ```
- Create rule: `_context.ExclusionRules.Add(newRule)`
- Save changes: `_context.SaveChangesAsync()`

### 5.3 DELETE Exclusion Rule Flow

```
1. Client sends DELETE request with JWT token, groupId, and ruleId
2. Minimal API endpoint extracts groupId and ruleId from route
3. JWT middleware validates token and extracts userId
4. Endpoint sends DeleteExclusionRuleCommand to MediatR
5. Handler validates:
   - Group exists
   - User is organizer
   - Draw not completed
   - Rule exists and belongs to group
6. Handler deletes exclusion rule
7. Return 204 No Content
```

**Database Operations:**
- Check group: `Groups.FindAsync(groupId)`
- Find rule:
  ```csharp
  _context.ExclusionRules
    .FirstOrDefaultAsync(er => er.RuleId == ruleId && er.GroupId == groupId)
  ```
- Delete rule: `_context.ExclusionRules.Remove(rule)`
- Save changes: `_context.SaveChangesAsync()`

---

## 6. Security Considerations

### 6.1 Authentication
- All endpoints require valid JWT Bearer token
- Token must contain `userId` claim
- Use ASP.NET Core Identity authentication middleware
- Return 401 Unauthorized if token is missing or invalid

### 6.2 Authorization
- Only group organizer can access these endpoints
- Verify `group.OrganizerUserId == currentUserId`
- Return 403 Forbidden if user is not organizer

### 6.3 Input Validation
- Validate groupId and ruleId are valid UUIDs (handled by route binding)
- Validate userId1 and userId2 are not null/empty
- Validate userId1 ≠ userId2
- Validate both users are participants in the group
- Sanitize all string inputs to prevent injection attacks

### 6.4 Business Rule Enforcement
- Prevent modifications after draw completion
- Validate exclusion rule feasibility before creation
- Prevent duplicate exclusion rules

---

## 7. Error Handling

### 7.1 Error Scenarios and Responses

| Scenario | HTTP Status | Error Code | Response Body |
|----------|-------------|------------|---------------|
| Missing JWT token | 401 | N/A | Standard 401 response |
| Invalid JWT token | 401 | N/A | Standard 401 response |
| User not organizer | 403 | Forbidden | `{"error": "Forbidden", "message": "You are not authorized to manage exclusion rules for this group"}` |
| Group not found | 404 | NotFound | `{"error": "NotFound", "message": "Group not found"}` |
| User not found (POST) | 404 | NotFound | `{"error": "NotFound", "message": "One or both users not found in this group"}` |
| Rule not found (DELETE) | 404 | NotFound | `{"error": "NotFound", "message": "Exclusion rule not found"}` |
| Same user (POST) | 400 | SameUser | `{"error": "SameUser", "message": "Cannot create exclusion rule for the same user"}` |
| Draw completed | 400 | DrawAlreadyCompleted | `{"error": "DrawAlreadyCompleted", "message": "Cannot [add/remove] exclusion rules after draw has been completed"}` |
| Invalid exclusion (POST) | 400 | InvalidExclusionRule | `{"error": "InvalidExclusionRule", "message": "This exclusion rule would make a valid draw impossible", "details": {...}}` |
| Duplicate rule (POST) | 409 | DuplicateExclusionRule | `{"error": "DuplicateExclusionRule", "message": "This exclusion rule already exists"}` |
| Database error | 500 | InternalServerError | `{"error": "InternalServerError", "message": "An unexpected error occurred"}` |

### 7.2 Logging Strategy

Use Serilog with structured logging:

```csharp
// Success operations
_logger.LogInformation(
    "Exclusion rule {RuleId} created for group {GroupId} by user {UserId}",
    ruleId, groupId, currentUserId);

// Validation failures
_logger.LogWarning(
    "Failed to create exclusion rule for group {GroupId}: {Reason}",
    groupId, "Draw already completed");

// Authorization failures
_logger.LogWarning(
    "User {UserId} attempted to access exclusion rules for group {GroupId} without authorization",
    currentUserId, groupId);

// Not found scenarios
_logger.LogInformation(
    "Exclusion rule {RuleId} not found in group {GroupId}",
    ruleId, groupId);

// Exceptions
_logger.LogError(
    exception,
    "Error creating exclusion rule for group {GroupId}",
    groupId);
```

---

## 8. Performance Considerations

### 8.1 Database Query Optimization
- Use `Include()` to eager load related user entities (avoid N+1 queries)
- Add indexes on frequently queried columns:
  - `ExclusionRules.GroupId`
  - `ExclusionRules.UserId1`
  - `ExclusionRules.UserId2`
  - Composite index on `(GroupId, UserId1, UserId2)` for uniqueness

### 8.2 Draw Validation Performance
- Draw validation using graph theory can be computationally expensive
- For groups with < 30 participants, validation should complete in < 100ms
- Consider caching validation results temporarily during rule creation
- Use efficient graph algorithms (e.g., maximum bipartite matching)

### 8.3 Caching Considerations
- Exclusion rules change infrequently after initial setup
- Consider caching exclusion rules list per group (invalidate on create/delete)
- TTL: Until draw completion or rule modification

---

## 9. Implementation Steps

### Step 1: Create Database Entities and Migrations
**File:** `SantaVibe.Api/Data/Entities/ExclusionRule.cs`

Verify or update the ExclusionRule entity:
```csharp
public class ExclusionRule
{
    public Guid RuleId { get; set; }
    public Guid GroupId { get; set; }
    public string UserId1 { get; set; } = null!;
    public string UserId2 { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Group Group { get; set; } = null!;
    public ApplicationUser User1 { get; set; } = null!;
    public ApplicationUser User2 { get; set; } = null!;
}
```

**File:** `SantaVibe.Api/Data/ApplicationDbContext.cs`

Add DbSet and configure entity:
```csharp
public DbSet<ExclusionRule> ExclusionRules { get; set; }

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<ExclusionRule>(entity =>
    {
        entity.HasKey(e => e.RuleId);

        entity.HasIndex(e => e.GroupId);
        entity.HasIndex(e => new { e.GroupId, e.UserId1, e.UserId2 })
              .IsUnique();

        entity.HasCheckConstraint("CK_ExclusionRule_DifferentUsers",
                                  "UserId1 <> UserId2");

        entity.HasOne(e => e.Group)
              .WithMany()
              .HasForeignKey(e => e.GroupId)
              .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(e => e.User1)
              .WithMany()
              .HasForeignKey(e => e.UserId1)
              .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.User2)
              .WithMany()
              .HasForeignKey(e => e.UserId2)
              .OnDelete(DeleteBehavior.Restrict);
    });
}
```

Create and apply migration:
```bash
dotnet ef migrations add AddExclusionRules
dotnet ef database update
```

---

### Step 2: Create Draw Validation Service

**File:** `SantaVibe.Api/Features/ExclusionRules/Services/DrawValidationService.cs`

```csharp
public interface IDrawValidationService
{
    Task<DrawValidationResult> ValidateDrawFeasibilityAsync(
        Guid groupId,
        CancellationToken cancellationToken = default);
}

public record DrawValidationResult(
    bool IsValid,
    List<string> Errors
);

public class DrawValidationService : IDrawValidationService
{
    private readonly ApplicationDbContext _context;

    public DrawValidationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DrawValidationResult> ValidateDrawFeasibilityAsync(
        Guid groupId,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        // Get all participants
        var participants = await _context.GroupParticipants
            .Where(gp => gp.GroupId == groupId)
            .Select(gp => gp.UserId)
            .ToListAsync(cancellationToken);

        if (participants.Count < 3)
        {
            errors.Add("Minimum 3 participants required for draw");
            return new DrawValidationResult(false, errors);
        }

        // Get all exclusion rules
        var exclusionRules = await _context.ExclusionRules
            .Where(er => er.GroupId == groupId)
            .Select(er => new { er.UserId1, er.UserId2 })
            .ToListAsync(cancellationToken);

        // Build adjacency graph (who can be assigned to whom)
        var validAssignments = new Dictionary<string, HashSet<string>>();

        foreach (var santa in participants)
        {
            validAssignments[santa] = new HashSet<string>(participants);
            validAssignments[santa].Remove(santa); // Cannot draw self

            foreach (var rule in exclusionRules)
            {
                if (rule.UserId1 == santa)
                    validAssignments[santa].Remove(rule.UserId2);
                if (rule.UserId2 == santa)
                    validAssignments[santa].Remove(rule.UserId1);
            }
        }

        // Check if valid assignment exists using maximum matching
        if (!HasValidAssignment(validAssignments, participants))
        {
            errors.Add("Current exclusion rules prevent valid assignments");
        }

        return new DrawValidationResult(errors.Count == 0, errors);
    }

    private bool HasValidAssignment(
        Dictionary<string, HashSet<string>> validAssignments,
        List<string> participants)
    {
        // Simple validation: each participant must have at least one valid recipient
        foreach (var santa in participants)
        {
            if (validAssignments[santa].Count == 0)
                return false;
        }

        // Additional validation: check for deadlock scenarios
        // For MVP, use simplified validation
        // For production, implement proper maximum bipartite matching

        return true;
    }
}
```

Register service in Program.cs:
```csharp
builder.Services.AddScoped<IDrawValidationService, DrawValidationService>();
```

---

### Step 3: Create GET Exclusion Rules Feature

**Folder Structure:**
```
Features/ExclusionRules/GetExclusionRules/
  ├── GetExclusionRulesQuery.cs
  ├── GetExclusionRulesHandler.cs
  ├── GetExclusionRulesResponse.cs
  └── GetExclusionRulesEndpoint.cs
```

**File:** `GetExclusionRulesQuery.cs`
```csharp
public record GetExclusionRulesQuery(
    Guid GroupId,
    string CurrentUserId
) : IRequest<Result<GetExclusionRulesResponse>>;
```

**File:** `GetExclusionRulesResponse.cs`
```csharp
public record GetExclusionRulesResponse(
    Guid GroupId,
    List<ExclusionRuleDto> ExclusionRules,
    int TotalCount
);

public record ExclusionRuleDto(
    Guid RuleId,
    UserInfoDto User1,
    UserInfoDto User2,
    DateTime CreatedAt
);

public record UserInfoDto(
    string UserId,
    string FirstName,
    string LastName
);
```

**File:** `GetExclusionRulesHandler.cs`
```csharp
public class GetExclusionRulesHandler
    : IRequestHandler<GetExclusionRulesQuery, Result<GetExclusionRulesResponse>>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GetExclusionRulesHandler> _logger;

    public GetExclusionRulesHandler(
        ApplicationDbContext context,
        ILogger<GetExclusionRulesHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<GetExclusionRulesResponse>> Handle(
        GetExclusionRulesQuery request,
        CancellationToken cancellationToken)
    {
        // Check if group exists
        var group = await _context.Groups
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.GroupId == request.GroupId, cancellationToken);

        if (group == null)
        {
            _logger.LogInformation(
                "Group {GroupId} not found",
                request.GroupId);
            return Result<GetExclusionRulesResponse>.Failure("NotFound", "Group not found");
        }

        // Check if user is organizer
        if (group.OrganizerUserId != request.CurrentUserId)
        {
            _logger.LogWarning(
                "User {UserId} attempted to access exclusion rules for group {GroupId} without authorization",
                request.CurrentUserId, request.GroupId);
            return Result<GetExclusionRulesResponse>.Failure(
                "Forbidden",
                "You are not authorized to view exclusion rules for this group");
        }

        // Fetch exclusion rules with user details
        var exclusionRules = await _context.ExclusionRules
            .AsNoTracking()
            .Include(er => er.User1)
            .Include(er => er.User2)
            .Where(er => er.GroupId == request.GroupId)
            .OrderBy(er => er.CreatedAt)
            .Select(er => new ExclusionRuleDto(
                er.RuleId,
                new UserInfoDto(er.UserId1, er.User1.FirstName, er.User1.LastName),
                new UserInfoDto(er.UserId2, er.User2.FirstName, er.User2.LastName),
                er.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Retrieved {Count} exclusion rules for group {GroupId}",
            exclusionRules.Count, request.GroupId);

        var response = new GetExclusionRulesResponse(
            request.GroupId,
            exclusionRules,
            exclusionRules.Count
        );

        return Result<GetExclusionRulesResponse>.Success(response);
    }
}
```

**File:** `GetExclusionRulesEndpoint.cs`
```csharp
public static class GetExclusionRulesEndpoint
{
    public static void MapGetExclusionRules(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/groups/{groupId:guid}/exclusion-rules",
            async (
                Guid groupId,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userId))
                    return Results.Unauthorized();

                var query = new GetExclusionRulesQuery(groupId, userId);
                var result = await sender.Send(query, cancellationToken);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error switch
                    {
                        "NotFound" => Results.NotFound(new { error = result.Error, message = result.ErrorMessage }),
                        "Forbidden" => Results.StatusCode(403),
                        _ => Results.StatusCode(500)
                    };
            })
            .RequireAuthorization()
            .WithName("GetExclusionRules")
            .WithTags("ExclusionRules")
            .Produces<GetExclusionRulesResponse>(200)
            .Produces(401)
            .Produces(403)
            .Produces(404);
    }
}
```

---

### Step 4: Create POST Exclusion Rule Feature

**Folder Structure:**
```
Features/ExclusionRules/CreateExclusionRule/
  ├── CreateExclusionRuleCommand.cs
  ├── CreateExclusionRuleHandler.cs
  ├── CreateExclusionRuleRequest.cs
  ├── CreateExclusionRuleResponse.cs
  └── CreateExclusionRuleEndpoint.cs
```

**File:** `CreateExclusionRuleRequest.cs`
```csharp
public record CreateExclusionRuleRequest(
    string UserId1,
    string UserId2
);
```

**File:** `CreateExclusionRuleCommand.cs`
```csharp
public record CreateExclusionRuleCommand(
    Guid GroupId,
    string UserId1,
    string UserId2,
    string CurrentUserId
) : IRequest<Result<CreateExclusionRuleResponse>>;
```

**File:** `CreateExclusionRuleResponse.cs`
```csharp
public record CreateExclusionRuleResponse(
    Guid RuleId,
    Guid GroupId,
    UserInfoDto User1,
    UserInfoDto User2,
    DateTime CreatedAt,
    DrawValidationDto DrawValidation
);

public record DrawValidationDto(
    bool IsValid,
    List<string> Errors
);
```

**File:** `CreateExclusionRuleHandler.cs`
```csharp
public class CreateExclusionRuleHandler
    : IRequestHandler<CreateExclusionRuleCommand, Result<CreateExclusionRuleResponse>>
{
    private readonly ApplicationDbContext _context;
    private readonly IDrawValidationService _drawValidationService;
    private readonly ILogger<CreateExclusionRuleHandler> _logger;

    public CreateExclusionRuleHandler(
        ApplicationDbContext context,
        IDrawValidationService drawValidationService,
        ILogger<CreateExclusionRuleHandler> logger)
    {
        _context = context;
        _drawValidationService = drawValidationService;
        _logger = logger;
    }

    public async Task<Result<CreateExclusionRuleResponse>> Handle(
        CreateExclusionRuleCommand request,
        CancellationToken cancellationToken)
    {
        // Validate userId1 and userId2 are different
        if (request.UserId1 == request.UserId2)
        {
            _logger.LogWarning(
                "Attempted to create exclusion rule with same user for group {GroupId}",
                request.GroupId);
            return Result<CreateExclusionRuleResponse>.Failure(
                "SameUser",
                "Cannot create exclusion rule for the same user");
        }

        // Check if group exists
        var group = await _context.Groups
            .FirstOrDefaultAsync(g => g.GroupId == request.GroupId, cancellationToken);

        if (group == null)
        {
            _logger.LogInformation("Group {GroupId} not found", request.GroupId);
            return Result<CreateExclusionRuleResponse>.Failure("NotFound", "Group not found");
        }

        // Check if user is organizer
        if (group.OrganizerUserId != request.CurrentUserId)
        {
            _logger.LogWarning(
                "User {UserId} attempted to create exclusion rule for group {GroupId} without authorization",
                request.CurrentUserId, request.GroupId);
            return Result<CreateExclusionRuleResponse>.Failure(
                "Forbidden",
                "You are not authorized to create exclusion rules for this group");
        }

        // Check if draw is already completed
        if (group.DrawCompletedAt.HasValue)
        {
            _logger.LogWarning(
                "Attempted to create exclusion rule for group {GroupId} after draw completion",
                request.GroupId);
            return Result<CreateExclusionRuleResponse>.Failure(
                "DrawAlreadyCompleted",
                "Cannot add exclusion rules after draw has been completed");
        }

        // Check if both users are participants
        var participants = await _context.GroupParticipants
            .Where(gp => gp.GroupId == request.GroupId &&
                        (gp.UserId == request.UserId1 || gp.UserId == request.UserId2))
            .Select(gp => gp.UserId)
            .ToListAsync(cancellationToken);

        if (participants.Count != 2)
        {
            _logger.LogWarning(
                "One or both users not found as participants in group {GroupId}",
                request.GroupId);
            return Result<CreateExclusionRuleResponse>.Failure(
                "NotFound",
                "One or both users not found in this group");
        }

        // Check for duplicate exclusion rule (bidirectional)
        var duplicateExists = await _context.ExclusionRules
            .AnyAsync(er => er.GroupId == request.GroupId &&
                           ((er.UserId1 == request.UserId1 && er.UserId2 == request.UserId2) ||
                            (er.UserId1 == request.UserId2 && er.UserId2 == request.UserId1)),
                     cancellationToken);

        if (duplicateExists)
        {
            _logger.LogWarning(
                "Duplicate exclusion rule for group {GroupId} between users {UserId1} and {UserId2}",
                request.GroupId, request.UserId1, request.UserId2);
            return Result<CreateExclusionRuleResponse>.Failure(
                "DuplicateExclusionRule",
                "This exclusion rule already exists");
        }

        // Create new exclusion rule
        var exclusionRule = new ExclusionRule
        {
            RuleId = Guid.NewGuid(),
            GroupId = request.GroupId,
            UserId1 = request.UserId1,
            UserId2 = request.UserId2,
            CreatedAt = DateTime.UtcNow
        };

        _context.ExclusionRules.Add(exclusionRule);
        await _context.SaveChangesAsync(cancellationToken);

        // Validate draw feasibility
        var drawValidation = await _drawValidationService
            .ValidateDrawFeasibilityAsync(request.GroupId, cancellationToken);

        if (!drawValidation.IsValid)
        {
            // Rollback: remove the rule if it makes draw impossible
            _context.ExclusionRules.Remove(exclusionRule);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogWarning(
                "Exclusion rule for group {GroupId} would make draw impossible",
                request.GroupId);

            return Result<CreateExclusionRuleResponse>.Failure(
                "InvalidExclusionRule",
                "This exclusion rule would make a valid draw impossible");
        }

        // Fetch user details
        var users = await _context.Users
            .Where(u => u.Id == request.UserId1 || u.Id == request.UserId2)
            .Select(u => new { u.Id, u.FirstName, u.LastName })
            .ToListAsync(cancellationToken);

        var user1Info = users.First(u => u.Id == request.UserId1);
        var user2Info = users.First(u => u.Id == request.UserId2);

        _logger.LogInformation(
            "Exclusion rule {RuleId} created for group {GroupId}",
            exclusionRule.RuleId, request.GroupId);

        var response = new CreateExclusionRuleResponse(
            exclusionRule.RuleId,
            exclusionRule.GroupId,
            new UserInfoDto(user1Info.Id, user1Info.FirstName, user1Info.LastName),
            new UserInfoDto(user2Info.Id, user2Info.FirstName, user2Info.LastName),
            exclusionRule.CreatedAt,
            new DrawValidationDto(drawValidation.IsValid, drawValidation.Errors)
        );

        return Result<CreateExclusionRuleResponse>.Success(response);
    }
}
```

**File:** `CreateExclusionRuleEndpoint.cs`
```csharp
public static class CreateExclusionRuleEndpoint
{
    public static void MapCreateExclusionRule(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/groups/{groupId:guid}/exclusion-rules",
            async (
                Guid groupId,
                CreateExclusionRuleRequest request,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userId))
                    return Results.Unauthorized();

                var command = new CreateExclusionRuleCommand(
                    groupId,
                    request.UserId1,
                    request.UserId2,
                    userId);

                var result = await sender.Send(command, cancellationToken);

                return result.IsSuccess
                    ? Results.Created(
                        $"/api/groups/{groupId}/exclusion-rules/{result.Value.RuleId}",
                        result.Value)
                    : result.Error switch
                    {
                        "NotFound" => Results.NotFound(new { error = result.Error, message = result.ErrorMessage }),
                        "Forbidden" => Results.StatusCode(403),
                        "SameUser" => Results.BadRequest(new { error = result.Error, message = result.ErrorMessage }),
                        "DrawAlreadyCompleted" => Results.BadRequest(new { error = result.Error, message = result.ErrorMessage }),
                        "InvalidExclusionRule" => Results.BadRequest(new { error = result.Error, message = result.ErrorMessage }),
                        "DuplicateExclusionRule" => Results.Conflict(new { error = result.Error, message = result.ErrorMessage }),
                        _ => Results.StatusCode(500)
                    };
            })
            .RequireAuthorization()
            .WithName("CreateExclusionRule")
            .WithTags("ExclusionRules")
            .Produces<CreateExclusionRuleResponse>(201)
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .Produces(409);
    }
}
```

---

### Step 5: Create DELETE Exclusion Rule Feature

**Folder Structure:**
```
Features/ExclusionRules/DeleteExclusionRule/
  ├── DeleteExclusionRuleCommand.cs
  ├── DeleteExclusionRuleHandler.cs
  └── DeleteExclusionRuleEndpoint.cs
```

**File:** `DeleteExclusionRuleCommand.cs`
```csharp
public record DeleteExclusionRuleCommand(
    Guid GroupId,
    Guid RuleId,
    string CurrentUserId
) : IRequest<Result>;
```

**File:** `DeleteExclusionRuleHandler.cs`
```csharp
public class DeleteExclusionRuleHandler
    : IRequestHandler<DeleteExclusionRuleCommand, Result>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DeleteExclusionRuleHandler> _logger;

    public DeleteExclusionRuleHandler(
        ApplicationDbContext context,
        ILogger<DeleteExclusionRuleHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result> Handle(
        DeleteExclusionRuleCommand request,
        CancellationToken cancellationToken)
    {
        // Check if group exists
        var group = await _context.Groups
            .FirstOrDefaultAsync(g => g.GroupId == request.GroupId, cancellationToken);

        if (group == null)
        {
            _logger.LogInformation("Group {GroupId} not found", request.GroupId);
            return Result.Failure("NotFound", "Group not found");
        }

        // Check if user is organizer
        if (group.OrganizerUserId != request.CurrentUserId)
        {
            _logger.LogWarning(
                "User {UserId} attempted to delete exclusion rule for group {GroupId} without authorization",
                request.CurrentUserId, request.GroupId);
            return Result.Failure(
                "Forbidden",
                "You are not authorized to delete exclusion rules for this group");
        }

        // Check if draw is already completed
        if (group.DrawCompletedAt.HasValue)
        {
            _logger.LogWarning(
                "Attempted to delete exclusion rule for group {GroupId} after draw completion",
                request.GroupId);
            return Result.Failure(
                "DrawAlreadyCompleted",
                "Cannot remove exclusion rules after draw has been completed");
        }

        // Find the exclusion rule
        var exclusionRule = await _context.ExclusionRules
            .FirstOrDefaultAsync(
                er => er.RuleId == request.RuleId && er.GroupId == request.GroupId,
                cancellationToken);

        if (exclusionRule == null)
        {
            _logger.LogInformation(
                "Exclusion rule {RuleId} not found in group {GroupId}",
                request.RuleId, request.GroupId);
            return Result.Failure("NotFound", "Exclusion rule not found");
        }

        // Delete the exclusion rule
        _context.ExclusionRules.Remove(exclusionRule);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Exclusion rule {RuleId} deleted from group {GroupId}",
            request.RuleId, request.GroupId);

        return Result.Success();
    }
}
```

**File:** `DeleteExclusionRuleEndpoint.cs`
```csharp
public static class DeleteExclusionRuleEndpoint
{
    public static void MapDeleteExclusionRule(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/groups/{groupId:guid}/exclusion-rules/{ruleId:guid}",
            async (
                Guid groupId,
                Guid ruleId,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userId))
                    return Results.Unauthorized();

                var command = new DeleteExclusionRuleCommand(groupId, ruleId, userId);
                var result = await sender.Send(command, cancellationToken);

                return result.IsSuccess
                    ? Results.NoContent()
                    : result.Error switch
                    {
                        "NotFound" => Results.NotFound(new { error = result.Error, message = result.ErrorMessage }),
                        "Forbidden" => Results.StatusCode(403),
                        "DrawAlreadyCompleted" => Results.BadRequest(new { error = result.Error, message = result.ErrorMessage }),
                        _ => Results.StatusCode(500)
                    };
            })
            .RequireAuthorization()
            .WithName("DeleteExclusionRule")
            .WithTags("ExclusionRules")
            .Produces(204)
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404);
    }
}
```

---

### Step 6: Create Result Helper Class

**File:** `SantaVibe.Api/Common/Result.cs`

```csharp
public class Result
{
    public bool IsSuccess { get; }
    public string Error { get; }
    public string ErrorMessage { get; }

    protected Result(bool isSuccess, string error, string errorMessage)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorMessage = errorMessage;
    }

    public static Result Success() => new(true, string.Empty, string.Empty);

    public static Result Failure(string error, string errorMessage) =>
        new(false, error, errorMessage);
}

public class Result<T> : Result
{
    public T? Value { get; }

    private Result(bool isSuccess, T? value, string error, string errorMessage)
        : base(isSuccess, error, errorMessage)
    {
        Value = value;
    }

    public static Result<T> Success(T value) =>
        new(true, value, string.Empty, string.Empty);

    public static new Result<T> Failure(string error, string errorMessage) =>
        new(false, default, error, errorMessage);
}
```

---

### Step 7: Register Endpoints in Program.cs

**File:** `SantaVibe.Api/Program.cs`

```csharp
// Register endpoints
app.MapGetExclusionRules();
app.MapCreateExclusionRule();
app.MapDeleteExclusionRule();
```

---

### Step 8: Add Unit Tests

**File:** `SantaVibe.Api.Tests/Features/ExclusionRules/GetExclusionRulesHandlerTests.cs`

```csharp
public class GetExclusionRulesHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserIsOrganizer_ReturnsExclusionRules()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var handler = new GetExclusionRulesHandler(context, Mock.Of<ILogger<GetExclusionRulesHandler>>());

        var groupId = Guid.NewGuid();
        var organizerId = "organizer-123";

        // Seed test data
        context.Groups.Add(new Group
        {
            GroupId = groupId,
            OrganizerUserId = organizerId,
            Name = "Test Group"
        });

        context.ExclusionRules.Add(new ExclusionRule
        {
            RuleId = Guid.NewGuid(),
            GroupId = groupId,
            UserId1 = "user1",
            UserId2 = "user2",
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var query = new GetExclusionRulesQuery(groupId, organizerId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalCount);
    }

    [Fact]
    public async Task Handle_WhenUserIsNotOrganizer_ReturnsForbidden()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var handler = new GetExclusionRulesHandler(context, Mock.Of<ILogger<GetExclusionRulesHandler>>());

        var groupId = Guid.NewGuid();
        var organizerId = "organizer-123";
        var otherUserId = "other-456";

        context.Groups.Add(new Group
        {
            GroupId = groupId,
            OrganizerUserId = organizerId,
            Name = "Test Group"
        });

        await context.SaveChangesAsync();

        var query = new GetExclusionRulesQuery(groupId, otherUserId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Forbidden", result.Error);
    }
}
```

Add similar tests for CreateExclusionRuleHandler and DeleteExclusionRuleHandler.

---

### Step 9: Add Integration Tests

**File:** `SantaVibe.Api.Tests/Integration/ExclusionRulesEndpointsTests.cs`

```csharp
public class ExclusionRulesEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ExclusionRulesEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetExclusionRules_WithValidOrganizerToken_Returns200()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var token = GenerateJwtToken("organizer-123");
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync(
            $"/api/groups/{groupId}/exclusion-rules");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateExclusionRule_WithValidData_Returns201()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var token = GenerateJwtToken("organizer-123");
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateExclusionRuleRequest("user1", "user2");
        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync(
            $"/api/groups/{groupId}/exclusion-rules",
            content);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
```

---

### Step 10: Documentation and Testing

1. **Update Swagger Documentation:**
   - Verify all endpoints appear in Swagger UI
   - Test endpoints using Swagger interface
   - Validate request/response schemas

2. **Manual Testing Checklist:**
   - [ ] GET exclusion rules as organizer (200)
   - [ ] GET exclusion rules as non-organizer (403)
   - [ ] GET exclusion rules for non-existent group (404)
   - [ ] CREATE exclusion rule with valid data (201)
   - [ ] CREATE exclusion rule with same user (400)
   - [ ] CREATE exclusion rule after draw (400)
   - [ ] CREATE exclusion rule with non-participants (404)
   - [ ] CREATE duplicate exclusion rule (409)
   - [ ] CREATE exclusion rule that makes draw impossible (400)
   - [ ] DELETE exclusion rule as organizer (204)
   - [ ] DELETE exclusion rule after draw (400)
   - [ ] DELETE non-existent rule (404)

3. **Performance Testing:**
   - Test with groups of various sizes (3, 10, 30 participants)
   - Verify draw validation completes within acceptable time
   - Monitor database query performance

4. **Security Testing:**
   - Verify JWT authentication on all endpoints
   - Test authorization boundaries (non-organizer access)
   - Validate input sanitization

---

## 10. Rollback Plan

If issues arise during deployment:

1. **Database Rollback:**
   ```bash
   dotnet ef database update [PreviousMigration]
   ```

2. **Code Rollback:**
   - Revert Git commit
   - Redeploy previous version

3. **Data Integrity:**
   - Exclusion rules can be safely deleted without cascade issues
   - No impact on existing assignments (rules only affect future draws)

---

## 11. Future Enhancements

1. **Advanced Draw Validation:**
   - Implement proper maximum bipartite matching algorithm
   - Provide suggestions for which rules to remove to make draw possible

2. **Bulk Operations:**
   - Batch create multiple exclusion rules
   - Import exclusion rules from CSV

3. **Rule Templates:**
   - Common patterns (couples, family members)
   - Save and reuse rule sets across groups

4. **Audit Trail:**
   - Track who created/deleted each rule
   - Maintain history of rule changes

---

## Document Metadata

**Version:** 1.0
**Date:** 2025-10-29
**Author:** Development Team
**Status:** Ready for Implementation
