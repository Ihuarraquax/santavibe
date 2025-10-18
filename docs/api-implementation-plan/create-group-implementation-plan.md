# API Endpoint Implementation Plan: Create Group

## 1. Endpoint Overview

The `POST /api/groups` endpoint creates a new Secret Santa group with the authenticated user as both the organizer and the first participant. It generates a unique invitation token and returns a shareable invitation link that can be used to invite other participants to join the group.

**Business Logic**:
- The authenticated user automatically becomes the organizer
- The organizer is also automatically added as the first participant
- A cryptographically secure invitation token (UUID) is generated
- The group starts with no budget set and no draw completed
- Returns complete group information including the invitation link

## 2. Request Details

**HTTP Method**: `POST`

**URL Structure**: `/api/groups`

**Authentication**: Required - JWT Bearer token in Authorization header

**Request Headers**:
```
Authorization: Bearer {jwt_token}
Content-Type: application/json
```

**Request Body**:
```json
{
  "name": "Family Secret Santa 2025"
}
```

**Request Parameters**:

*Required*:
- `name` (string): The name of the Secret Santa group
  - Minimum length: 3 characters
  - Maximum length: 200 characters
  - Cannot be null, empty, or only whitespace
  - Should be trimmed before validation

*Optional*: None

## 3. Used Types

### Request DTO
```csharp
namespace SantaVibe.Api.Features.Groups.Create;

using System.ComponentModel.DataAnnotations;

public record CreateGroupRequest
{
    [Required(ErrorMessage = "Group name is required")]
    [MinLength(3, ErrorMessage = "Group name must be at least 3 characters")]
    [MaxLength(200, ErrorMessage = "Group name cannot exceed 200 characters")]
    public required string Name { get; init; }
}
```

### Response DTO
```csharp
namespace SantaVibe.Api.Features.Groups.Create;

public record CreateGroupResponse
{
    public required Guid GroupId { get; init; }
    public required string Name { get; init; }
    public required string OrganizerId { get; init; }
    public required string OrganizerName { get; init; }
    public required Guid InvitationToken { get; init; }
    public required string InvitationLink { get; init; }
    public required int ParticipantCount { get; init; }
    public required decimal? Budget { get; init; }
    public required bool DrawCompleted { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
```

### Command Model
```csharp
namespace SantaVibe.Api.Features.Groups.Create;

public record CreateGroupCommand
{
    public required string Name { get; init; }
    public required string UserId { get; init; }
}
```

## 4. Response Details

### Success Response (201 Created)

**Status Code**: `201 Created`

**Headers**:
```
Location: /api/groups/{groupId}
Content-Type: application/json
```

**Response Body**:
```json
{
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "name": "Family Secret Santa 2025",
  "organizerId": "550e8400-e29b-41d4-a716-446655440000",
  "organizerName": "Jan Kowalski",
  "invitationToken": "a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d",
  "invitationLink": "https://santavibe.com/invite/a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d",
  "participantCount": 1,
  "budget": null,
  "drawCompleted": false,
  "createdAt": "2025-10-15T10:00:00Z"
}
```

**Field Descriptions**:
- `groupId`: Unique identifier for the created group
- `name`: The group name as provided in the request
- `organizerId`: User ID of the group organizer (authenticated user)
- `organizerName`: Full name of the organizer (FirstName + LastName)
- `invitationToken`: UUID that can be used to join the group
- `invitationLink`: Complete shareable URL for invitation
- `participantCount`: Always 1 (the organizer)
- `budget`: Always null for newly created groups
- `drawCompleted`: Always false for newly created groups
- `createdAt`: ISO 8601 timestamp in UTC

### Error Responses

**400 Bad Request - Validation Error**:
```json
{
  "error": "ValidationError",
  "message": "Group name must be at least 3 characters",
  "details": {
    "name": ["Group name must be at least 3 characters"]
  }
}
```

**401 Unauthorized - Missing or Invalid Token**:
```json
{
  "error": "Unauthorized",
  "message": "Missing or invalid authentication token"
}
```

**500 Internal Server Error - Database Failure**:
```json
{
  "error": "InternalServerError",
  "message": "An unexpected error occurred while creating the group"
}
```

## 5. Data Flow

### Step-by-Step Data Flow

1. **Request Reception**
   - Minimal API endpoint receives POST request
   - ASP.NET Core deserializes JSON to `CreateGroupRequest` DTO

2. **Authentication & Authorization**
   - JWT authentication middleware validates Bearer token
   - Extract `userId`, `firstName`, `lastName` from JWT claims
   - Return 401 if token is missing, expired, or invalid

3. **Input Validation**
   - Endpoint filter validates request using Data Annotations
   - Validate name length (3-200 characters)
   - Validate name is not null/empty/whitespace
   - Return 400 with validation errors if validation fails

4. **Service Invocation**
   - Map `CreateGroupRequest` + JWT claims → `CreateGroupCommand`
   - Call `CreateGroupService.CreateGroupAsync(command)`

5. **Database Transaction** (within service)
   - **Start transaction**
   - Verify user exists in `AspNetUsers` table
   - Create `Group` entity:
     - `Id` = new Guid
     - `Name` = trimmed request name
     - `OrganizerUserId` = userId from JWT
     - `InvitationToken` = Guid.NewGuid()
     - `Budget` = null
     - `DrawCompletedAt` = null
     - `CreatedAt` = DateTimeOffset.UtcNow
   - Add `Group` to DbContext
   - Create `GroupParticipant` entity:
     - `GroupId` = Group.Id
     - `UserId` = userId from JWT
     - `BudgetSuggestion` = null
     - `WishlistContent` = null
     - `JoinedAt` = DateTimeOffset.UtcNow
   - Add `GroupParticipant` to DbContext
   - **SaveChangesAsync()**
   - **Commit transaction**

6. **Response Construction**
   - Build invitation link: `{baseUrl}/invite/{invitationToken}`
   - Map `Group` entity + user info → `CreateGroupResponse`
   - Set `participantCount` = 1
   - Set `organizerName` = `{firstName} {lastName}`

7. **HTTP Response**
   - Return 201 Created
   - Set `Location` header to `/api/groups/{groupId}`
   - Return `CreateGroupResponse` as JSON body

### Database Interactions

**Tables Modified**:
1. `Groups` - INSERT one row
2. `GroupParticipants` - INSERT one row

**Indexes Used**:
- Primary key on `Groups.Id`
- Primary key on `GroupParticipants` (composite: GroupId, UserId)
- Unique index on `Groups.InvitationToken`

**Transaction Isolation**: Read Committed (default)

## 6. Security Considerations

### Authentication
- **Requirement**: Valid JWT Bearer token in Authorization header
- **Validation**: ASP.NET Core JWT middleware validates signature and expiration
- **Claims Extraction**: Extract `userId`, `firstName`, `lastName` from token
- **Token Expiration**: Honor 24-hour token lifetime

### Authorization
- **Access Control**: Any authenticated user can create a group
- **No Special Permissions Required**: Standard user role sufficient

### Input Validation & Sanitization
- **Name Validation**:
  - Data Annotations enforce length constraints (3-200 characters)
  - Required attribute ensures non-null values
  - Custom validation in endpoint filter for whitespace-only names
  - Trim leading/trailing whitespace before saving to database
- **Validation Approach**:
  - Use Data Annotations attributes on request DTOs
  - Endpoint filter validates all properties using `Validator.TryValidateObject`
  - Additional custom validation logic in filter for edge cases
- **XSS Prevention**:
  - HTML-encode group name in responses
  - Frontend should escape when rendering
- **SQL Injection Prevention**:
  - EF Core uses parameterized queries (automatic protection)

### Token Security
- **Invitation Token Generation**:
  - Use `Guid.NewGuid()` for cryptographically secure randomness
  - Store as UUID in database with UNIQUE constraint
  - No sequential or predictable patterns

### Rate Limiting
- **General Limit**: 100 requests per authenticated user per minute
- **Implementation**: ASP.NET Core rate limiting middleware
- **Headers**: Include `X-RateLimit-*` headers in response

### HTTPS Enforcement
- **Production**: Require HTTPS for all API calls
- **Development**: Use HTTPS on localhost:5001
- **HSTS**: Enable HTTP Strict Transport Security headers

### CORS Configuration
- **Allowed Origins**:
  - Development: `http://localhost:4200`
  - Production: `https://santavibe.com`
- **Allowed Methods**: POST
- **Allowed Headers**: Authorization, Content-Type

### Logging Security
- **Safe Logging**:
  - Log group creation events with userId and groupId
  - Do NOT log invitation tokens in plain text
  - Mask or hash sensitive data in logs
- **PII Protection**:
  - User names are not considered highly sensitive
  - Use structured logging with Serilog

## 7. Error Handling

### Validation Errors (400 Bad Request)

| Error Scenario | Error Code | Message | Details |
|----------------|------------|---------|---------|
| Name is null/empty | `ValidationError` | "Group name is required" | `{ "name": ["Group name is required"] }` |
| Name too short | `ValidationError` | "Group name must be at least 3 characters" | `{ "name": ["Group name must be at least 3 characters"] }` |
| Name too long | `ValidationError` | "Group name cannot exceed 200 characters" | `{ "name": ["Group name cannot exceed 200 characters"] }` |
| Name is whitespace | `ValidationError` | "Group name cannot be only whitespace" | `{ "name": ["Group name cannot be only whitespace"] }` |

**Error Response Format**:
```json
{
  "error": "ValidationError",
  "message": "Group name must be at least 3 characters",
  "details": {
    "name": ["Group name must be at least 3 characters"]
  }
}
```

### Authentication Errors (401 Unauthorized)

| Error Scenario | Error Code | Message |
|----------------|------------|---------|
| Missing Authorization header | `Unauthorized` | "Missing or invalid authentication token" |
| Invalid JWT signature | `Unauthorized` | "Missing or invalid authentication token" |
| Expired JWT token | `Unauthorized` | "Missing or invalid authentication token" |
| User not found (invalid userId in token) | `Unauthorized` | "Missing or invalid authentication token" |

**Error Response Format**:
```json
{
  "error": "Unauthorized",
  "message": "Missing or invalid authentication token"
}
```

**Note**: Use generic message to avoid revealing whether user exists

### Server Errors (500 Internal Server Error)

| Error Scenario | Error Code | Message | Logging |
|----------------|------------|---------|---------|
| Database connection failure | `InternalServerError` | "An unexpected error occurred while creating the group" | Log full exception with stack trace |
| Unique constraint violation (unlikely) | `InternalServerError` | "An unexpected error occurred while creating the group" | Log constraint details |
| Transaction rollback failure | `InternalServerError` | "An unexpected error occurred while creating the group" | Log transaction state |

**Error Response Format**:
```json
{
  "error": "InternalServerError",
  "message": "An unexpected error occurred while creating the group"
}
```

### Error Handling Implementation

**Endpoint Filter for Validation**:
```csharp
using System.ComponentModel.DataAnnotations;

public class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<T>().FirstOrDefault();
        if (request == null)
        {
            return Results.BadRequest(new
            {
                error = "ValidationError",
                message = "Invalid request body"
            });
        }

        // Validate using Data Annotations
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(request);

        if (!Validator.TryValidateObject(request, validationContext, validationResults, validateAllProperties: true))
        {
            // Additional check for whitespace-only names
            if (request is CreateGroupRequest groupRequest &&
                !string.IsNullOrWhiteSpace(groupRequest.Name) == false)
            {
                validationResults.Add(new ValidationResult(
                    "Group name cannot be only whitespace",
                    new[] { "Name" }));
            }

            var errors = validationResults
                .GroupBy(e => e.MemberNames.FirstOrDefault() ?? "")
                .ToDictionary(
                    g => char.ToLowerInvariant(g.Key[0]) + g.Key.Substring(1),
                    g => g.Select(e => e.ErrorMessage ?? "Validation error").ToArray()
                );

            return Results.BadRequest(new
            {
                error = "ValidationError",
                message = validationResults.First().ErrorMessage ?? "Validation failed",
                details = errors
            });
        }

        // Additional whitespace validation
        if (request is CreateGroupRequest createGroupRequest)
        {
            if (string.IsNullOrWhiteSpace(createGroupRequest.Name))
            {
                return Results.BadRequest(new
                {
                    error = "ValidationError",
                    message = "Group name cannot be only whitespace",
                    details = new Dictionary<string, string[]>
                    {
                        { "name", new[] { "Group name cannot be only whitespace" } }
                    }
                });
            }
        }

        return await next(context);
    }
}
```

**Global Exception Handler Middleware**:
- Catch unhandled exceptions
- Log with Serilog including correlation ID
- Return 500 with generic message
- Never expose stack traces or internal details

## 8. Performance Considerations

### Database Performance

**Query Optimization**:
- Single transaction for both INSERT operations
- No SELECT queries needed (user info from JWT)
- Primary key and unique indexes automatically created
- Expected execution time: <50ms

**Connection Management**:
- Use connection pooling (Npgsql default)
- Scoped DbContext lifetime per request
- No long-running transactions

**Potential Bottlenecks**:
- None expected for single group creation
- Unique constraint check on InvitationToken (negligible)

### API Response Time

**Expected Latency**:
- Authentication: ~5ms (JWT validation)
- Validation: ~1ms (in-memory checks)
- Database operations: ~30-50ms
- Response serialization: ~5ms
- **Total**: <100ms under normal conditions

### Scalability Considerations

**Concurrent Requests**:
- No locking required (each user creates independent groups)
- No shared state between requests
- Safe for horizontal scaling

**Memory Usage**:
- Request DTO: ~1KB
- Response DTO: ~1KB
- No caching needed for creation endpoint

### Caching Strategy

**Not Applicable**:
- Creation endpoints should not be cached
- POST requests are non-idempotent
- No ETag or Last-Modified headers needed

## 9. Implementation Steps

### Step 1: Create Project Structure
```
SantaVibe.Api/
└── Features/
    └── Groups/
        └── Create/
            ├── CreateGroupRequest.cs
            ├── CreateGroupResponse.cs
            ├── CreateGroupCommand.cs
            ├── CreateGroupService.cs
            └── CreateGroupEndpoint.cs
```

### Step 2: Implement Request DTO

**File**: `CreateGroupRequest.cs`
```csharp
namespace SantaVibe.Api.Features.Groups.Create;

using System.ComponentModel.DataAnnotations;

public record CreateGroupRequest
{
    [Required(ErrorMessage = "Group name is required")]
    [MinLength(3, ErrorMessage = "Group name must be at least 3 characters")]
    [MaxLength(200, ErrorMessage = "Group name cannot exceed 200 characters")]
    public required string Name { get; init; }
}
```

### Step 3: Implement Response DTO

**File**: `CreateGroupResponse.cs`
```csharp
namespace SantaVibe.Api.Features.Groups.Create;

public record CreateGroupResponse
{
    public required Guid GroupId { get; init; }
    public required string Name { get; init; }
    public required string OrganizerId { get; init; }
    public required string OrganizerName { get; init; }
    public required Guid InvitationToken { get; init; }
    public required string InvitationLink { get; init; }
    public required int ParticipantCount { get; init; }
    public required decimal? Budget { get; init; }
    public required bool DrawCompleted { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
```

### Step 4: Implement Command Model

**File**: `CreateGroupCommand.cs`
```csharp
namespace SantaVibe.Api.Features.Groups.Create;

public record CreateGroupCommand
{
    public required string Name { get; init; }
    public required string UserId { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
}
```

### Step 5: Implement Service Layer

**File**: `CreateGroupService.cs`
```csharp
namespace SantaVibe.Api.Features.Groups.Create;

using SantaVibe.Api.Data;
using SantaVibe.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

public class CreateGroupService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CreateGroupService> _logger;

    public CreateGroupService(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<CreateGroupService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<CreateGroupResponse> CreateGroupAsync(
        CreateGroupCommand command,
        CancellationToken cancellationToken = default)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Verify user exists
            var userExists = await _context.Users
                .AnyAsync(u => u.Id == command.UserId, cancellationToken);

            if (!userExists)
            {
                _logger.LogWarning("User {UserId} not found when creating group", command.UserId);
                throw new UnauthorizedAccessException("User not found");
            }

            // Create group
            var group = new Group
            {
                Id = Guid.NewGuid(),
                Name = command.Name.Trim(),
                OrganizerUserId = command.UserId,
                InvitationToken = Guid.NewGuid(),
                Budget = null,
                DrawCompletedAt = null,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = null
            };

            _context.Groups.Add(group);

            // Add organizer as first participant
            var participant = new GroupParticipant
            {
                GroupId = group.Id,
                UserId = command.UserId,
                BudgetSuggestion = null,
                WishlistContent = null,
                JoinedAt = DateTimeOffset.UtcNow
            };

            _context.GroupParticipants.Add(participant);

            // Save changes
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Group {GroupId} created by user {UserId}",
                group.Id,
                command.UserId);

            // Build invitation link
            var baseUrl = _configuration["App:BaseUrl"] ?? "https://santavibe.com";
            var invitationLink = $"{baseUrl}/invite/{group.InvitationToken}";

            // Map to response
            return new CreateGroupResponse
            {
                GroupId = group.Id,
                Name = group.Name,
                OrganizerId = command.UserId,
                OrganizerName = $"{command.FirstName} {command.LastName}",
                InvitationToken = group.InvitationToken,
                InvitationLink = invitationLink,
                ParticipantCount = 1,
                Budget = null,
                DrawCompleted = false,
                CreatedAt = group.CreatedAt
            };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
```

### Step 6: Implement Minimal API Endpoint

**File**: `CreateGroupEndpoint.cs`
```csharp
namespace SantaVibe.Api.Features.Groups.Create;

using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

public static class CreateGroupEndpoint
{
    public static void MapCreateGroupEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/groups", async (
            [FromBody] CreateGroupRequest request,
            ClaimsPrincipal user,
            CreateGroupService service,
            CancellationToken cancellationToken) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var firstName = user.FindFirstValue(ClaimTypes.GivenName);
            var lastName = user.FindFirstValue(ClaimTypes.Surname);

            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            var command = new CreateGroupCommand
            {
                Name = request.Name,
                UserId = userId,
                FirstName = firstName ?? "",
                LastName = lastName ?? ""
            };

            var response = await service.CreateGroupAsync(command, cancellationToken);

            return Results.Created($"/api/groups/{response.GroupId}", response);
        })
        .RequireAuthorization()
        .WithName("CreateGroup")
        .WithTags("Groups")
        .Produces<CreateGroupResponse>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status500InternalServerError)
        .AddEndpointFilter<ValidationFilter<CreateGroupRequest>>();
    }
}
```

### Step 7: Implement Validation Filter

**File**: `Common/ValidationFilter.cs`
```csharp
namespace SantaVibe.Api.Common;

using System.ComponentModel.DataAnnotations;
using SantaVibe.Api.Features.Groups.Create;

public class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<T>().FirstOrDefault();
        if (request == null)
        {
            return Results.BadRequest(new
            {
                error = "ValidationError",
                message = "Invalid request body"
            });
        }

        // Validate using Data Annotations
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(request);

        if (!Validator.TryValidateObject(request, validationContext, validationResults, validateAllProperties: true))
        {
            var errors = validationResults
                .GroupBy(e => e.MemberNames.FirstOrDefault() ?? "")
                .Where(g => !string.IsNullOrEmpty(g.Key))
                .ToDictionary(
                    g => char.ToLowerInvariant(g.Key[0]) + g.Key.Substring(1),
                    g => g.Select(e => e.ErrorMessage ?? "Validation error").ToArray()
                );

            return Results.BadRequest(new
            {
                error = "ValidationError",
                message = validationResults.First().ErrorMessage ?? "Validation failed",
                details = errors
            });
        }

        // Additional whitespace validation for CreateGroupRequest
        if (request is CreateGroupRequest createGroupRequest)
        {
            if (string.IsNullOrWhiteSpace(createGroupRequest.Name))
            {
                return Results.BadRequest(new
                {
                    error = "ValidationError",
                    message = "Group name cannot be only whitespace",
                    details = new Dictionary<string, string[]>
                    {
                        { "name", new[] { "Group name cannot be only whitespace" } }
                    }
                });
            }
        }

        return await next(context);
    }
}
```

### Step 8: Register Services in DI Container

**File**: `Program.cs` or `ServiceCollectionExtensions.cs`
```csharp
// Register services
builder.Services.AddScoped<CreateGroupService>();

// Register validation filter
builder.Services.AddScoped(typeof(ValidationFilter<>));
```

### Step 9: Map Endpoint in Program.cs

**File**: `Program.cs`
```csharp
// Map endpoints
app.MapCreateGroupEndpoint();
```

### Step 10: Add Configuration for Base URL

**File**: `appsettings.json`
```json
{
  "App": {
    "BaseUrl": "https://localhost:5001"
  }
}
```

**File**: `appsettings.Production.json`
```json
{
  "App": {
    "BaseUrl": "https://santavibe.com"
  }
}
```

### Step 11: Implement Global Exception Handler

**File**: `GlobalExceptionHandler.cs`
```csharp
namespace SantaVibe.Api.Middleware;

using Microsoft.AspNetCore.Diagnostics;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(
            exception,
            "An unexpected error occurred: {Message}",
            exception.Message);

        var response = new
        {
            error = "InternalServerError",
            message = "An unexpected error occurred while processing your request"
        };

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

        return true;
    }
}
```

**Register in Program.cs**:
```csharp
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
app.UseExceptionHandler();
```

### Step 12: Add Unit Tests

**File**: `CreateGroupServiceTests.cs`
```csharp
namespace SantaVibe.Api.Tests.Features.Groups.Create;

using Xunit;
using NSubstitute;
using SantaVibe.Api.Features.Groups.Create;
using SantaVibe.Api.Data;

public class CreateGroupServiceTests
{
    [Fact]
    public async Task CreateGroupAsync_ValidCommand_ReturnsResponse()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var config = CreateConfiguration();
        var logger = Substitute.For<ILogger<CreateGroupService>>();
        var service = new CreateGroupService(context, config, logger);

        var command = new CreateGroupCommand
        {
            Name = "Test Group",
            UserId = "user-123",
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var result = await service.CreateGroupAsync(command);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Group", result.Name);
        Assert.Equal(1, result.ParticipantCount);
        Assert.False(result.DrawCompleted);
    }

    // Additional tests for error scenarios...
}
```

### Step 13: Add Integration Tests

**File**: `CreateGroupEndpointTests.cs`
```csharp
namespace SantaVibe.Api.Tests.Integration.Groups;

using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;

public class CreateGroupEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CreateGroupEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateGroup_ValidRequest_Returns201()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new { name = "Test Group" };

        // Act
        var response = await client.PostAsJsonAsync("/api/groups", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CreateGroupResponse>();
        Assert.NotNull(result);
        Assert.Equal("Test Group", result.Name);
    }

    // Additional integration tests...
}
```

### Step 14: Update OpenAPI/Swagger Documentation

**File**: `Program.cs`
```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SantaVibe API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
```

### Step 15: Test End-to-End

1. Start backend: `dotnet run` in SantaVibe.Api
2. Navigate to Swagger UI: `https://localhost:5001/swagger`
3. Authenticate with valid JWT token
4. Test POST /api/groups endpoint with valid and invalid inputs
5. Verify 201 response with correct data
6. Verify 400 response for validation errors
7. Verify 401 response for missing/invalid token
8. Check database to confirm Group and GroupParticipant records created

### Step 16: Performance Testing

1. Use load testing tool (e.g., k6, Apache JMeter)
2. Simulate 100 concurrent users creating groups
3. Verify average response time < 100ms
4. Verify no database deadlocks or transaction failures
5. Monitor memory and CPU usage
6. Verify rate limiting works correctly

### Step 17: Security Testing

1. Test with expired JWT token → 401
2. Test with tampered JWT signature → 401
3. Test with XSS payload in group name → sanitized
4. Test with SQL injection in group name → handled by EF Core
5. Verify HTTPS enforcement in production
6. Verify CORS headers are correct
7. Test rate limiting with rapid requests → 429

---

## Summary

This implementation plan provides comprehensive guidance for implementing the `POST /api/groups` endpoint following clean architecture principles, minimal API patterns, and ASP.NET Core 9 best practices. The implementation includes proper validation, error handling, security measures, and performance considerations as specified in the API plan and backend guidelines.
