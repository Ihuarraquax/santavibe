# API Endpoint Implementation Plan: Get Invitation Details

## 1. Endpoint Overview

This endpoint retrieves group information for a given invitation token. It's a public endpoint used to display group details to potential participants before they decide to join. The endpoint validates that the invitation token exists and checks whether the group is still accepting participants (draw not completed).

**Key Business Rules:**
- Public endpoint (no authentication required)
- Returns 404 if invitation token is invalid
- Returns 410 if group has completed the draw
- Provides enough information for user to decide whether to join

## 2. Request Details

- **HTTP Method**: GET
- **URL Structure**: `/api/invitations/{token}`
- **Authentication**: None (public endpoint)

**Parameters:**
- **Path Parameters (Required)**:
  - `token` (Guid): The invitation token from the shareable link

**Request Body**: None

**Example Request:**
```
GET /api/invitations/a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d
```

## 3. Used Types

### Response DTO

**File**: `SantaVibe.Api/Features/Invitations/GetInvitationDetails/GetInvitationDetailsResponse.cs`

```csharp
namespace SantaVibe.Api.Features.Invitations.GetInvitationDetails;

public sealed record GetInvitationDetailsResponse(
    Guid InvitationToken,
    Guid GroupId,
    string GroupName,
    string OrganizerName,
    int ParticipantCount,
    bool DrawCompleted,
    bool IsValid
);
```

### No Command/Request Model Needed
This is a simple GET endpoint with only a path parameter, so no request model is required.

## 4. Response Details

### Success Response (200 OK)

```json
{
  "invitationToken": "a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d",
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "groupName": "Family Secret Santa 2025",
  "organizerName": "Jan Kowalski",
  "participantCount": 5,
  "drawCompleted": false,
  "isValid": true
}
```

### Error Response - Invalid Token (404 Not Found)

```json
{
  "error": "InvalidInvitation",
  "message": "This invitation link is invalid or has expired"
}
```

### Error Response - Draw Completed (410 Gone)

```json
{
  "error": "InvitationExpired",
  "message": "This group has already completed the draw and is no longer accepting participants"
}
```

## 5. Data Flow

1. **Request Reception**: Minimal API endpoint receives GET request with token parameter
2. **Token Parsing**: Token is automatically parsed as Guid by route constraint
3. **Service Call**: Call `InvitationService.GetInvitationDetailsAsync(token)`
4. **Database Query**:
   - Query `Groups` table by `InvitationToken`
   - Eager load `Organizer` navigation property
   - Eager load `GroupParticipants` collection for count
5. **Validation**:
   - If group not found → return 404
   - If `DrawCompletedAt` is not null → return 410
6. **Response Mapping**: Map entity to `GetInvitationDetailsResponse`
7. **Response**: Return 200 OK with response DTO

**Database Interactions:**
- **Read**: Groups table (with Organizer and GroupParticipants navigation properties)
- **No Writes**: This is a read-only operation

**External Services:**
- None

## 6. Security Considerations

### Authentication
- **None required** - This is a public endpoint
- Users must be authenticated to actually join the group (different endpoint)

### Authorization
- No authorization checks needed
- Invitation tokens are UUIDs (non-guessable, secure)

### Data Validation
- Token must be valid UUID format (enforced by route constraint)
- No user input to sanitize beyond token validation

### Data Exposure
- Only safe, public information is exposed:
  - Group name
  - Organizer name (first + last)
  - Participant count
  - Draw completion status
- No sensitive data like wishlists, assignments, or email addresses

### Rate Limiting
- Should apply general rate limiting (100 requests per IP per minute)
- Prevent abuse of public endpoint

## 7. Error Handling

### Error Scenarios

| Scenario | Status Code | Error Code | Message |
|----------|-------------|------------|---------|
| Invalid token format | 400 | BadRequest | Automatic validation by route constraint |
| Token not found in database | 404 | InvalidInvitation | "This invitation link is invalid or has expired" |
| Draw already completed | 410 | InvitationExpired | "This group has already completed the draw and is no longer accepting participants" |
| Database connection error | 500 | InternalServerError | "An unexpected error occurred" |
| Unexpected exception | 500 | InternalServerError | "An unexpected error occurred" |

### Error Response Format

All errors follow the standard error format:

```json
{
  "error": "ErrorCode",
  "message": "User-friendly error message"
}
```

### Logging Strategy

- **Info**: Log all successful requests with token (for analytics)
- **Warning**: Log 404 errors (track invalid invitation attempts)
- **Warning**: Log 410 errors (users trying to join completed draws)
- **Error**: Log 500 errors with full exception details

## 8. Performance Considerations

### Optimization Strategies

1. **Eager Loading**: Use `.Include()` for Organizer and GroupParticipants to avoid N+1 queries
2. **Select Only Required Fields**: Project to DTO directly in query if possible
3. **Indexing**: Ensure InvitationToken column has unique index (should be defined in entity configuration)
4. **No Tracking**: Use `.AsNoTracking()` since this is read-only query
5. **Caching**: Consider response caching for frequently accessed invitations (future enhancement)

### Expected Performance

- **Query Complexity**: Simple single-table query with two navigation properties
- **Expected Response Time**: < 100ms for typical database
- **Bottlenecks**: None expected for MVP scale

### Database Query Pattern

```csharp
// Optimized query example
var group = await context.Groups
    .AsNoTracking()
    .Include(g => g.Organizer)
    .Include(g => g.GroupParticipants)
    .FirstOrDefaultAsync(g => g.InvitationToken == token);
```

## 9. Implementation Steps

### Step 1: Create Response DTO

**File**: `SantaVibe.Api/Features/Invitations/GetInvitationDetails/GetInvitationDetailsResponse.cs`

```csharp
namespace SantaVibe.Api.Features.Invitations.GetInvitationDetails;

public sealed record GetInvitationDetailsResponse(
    Guid InvitationToken,
    Guid GroupId,
    string GroupName,
    string OrganizerName,
    int ParticipantCount,
    bool DrawCompleted,
    bool IsValid
);
```

### Step 2: Create Service Interface

**File**: `SantaVibe.Api/Features/Invitations/IInvitationService.cs`

```csharp
namespace SantaVibe.Api.Features.Invitations;

public interface IInvitationService
{
    Task<GetInvitationDetailsResponse?> GetInvitationDetailsAsync(Guid token, CancellationToken cancellationToken = default);
}
```

### Step 3: Implement Service

**File**: `SantaVibe.Api/Features/Invitations/InvitationService.cs`

```csharp
namespace SantaVibe.Api.Features.Invitations;

public sealed class InvitationService(ApplicationDbContext context, ILogger<InvitationService> logger) : IInvitationService
{
    public async Task<GetInvitationDetailsResponse?> GetInvitationDetailsAsync(Guid token, CancellationToken cancellationToken = default)
    {
        var group = await context.Groups
            .AsNoTracking()
            .Include(g => g.Organizer)
            .Include(g => g.GroupParticipants)
            .FirstOrDefaultAsync(g => g.InvitationToken == token, cancellationToken);

        if (group is null)
        {
            logger.LogWarning("Invitation token {Token} not found", token);
            return null;
        }

        var organizerName = $"{group.Organizer.FirstName} {group.Organizer.LastName}";
        var participantCount = group.GroupParticipants.Count;
        var drawCompleted = group.DrawCompletedAt.HasValue;

        return new GetInvitationDetailsResponse(
            InvitationToken: group.InvitationToken,
            GroupId: group.Id,
            GroupName: group.Name,
            OrganizerName: organizerName,
            ParticipantCount: participantCount,
            DrawCompleted: drawCompleted,
            IsValid: true
        );
    }
}
```

### Step 4: Register Service in DI Container

**File**: `SantaVibe.Api/Program.cs` (or DI configuration file)

```csharp
builder.Services.AddScoped<IInvitationService, InvitationService>();
```

### Step 5: Create Minimal API Endpoint

**File**: `SantaVibe.Api/Features/Invitations/GetInvitationDetails/GetInvitationDetailsEndpoint.cs`

```csharp
namespace SantaVibe.Api.Features.Invitations.GetInvitationDetails;

public static class GetInvitationDetailsEndpoint
{
    public static IEndpointRouteBuilder MapGetInvitationDetails(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/invitations/{token:guid}", async (
            Guid token,
            IInvitationService invitationService,
            CancellationToken cancellationToken) =>
        {
            var result = await invitationService.GetInvitationDetailsAsync(token, cancellationToken);

            if (result is null)
            {
                return Results.NotFound(new
                {
                    error = "InvalidInvitation",
                    message = "This invitation link is invalid or has expired"
                });
            }

            if (result.DrawCompleted)
            {
                return Results.StatusCode(410, new
                {
                    error = "InvitationExpired",
                    message = "This group has already completed the draw and is no longer accepting participants"
                });
            }

            return Results.Ok(result);
        })
        .WithName("GetInvitationDetails")
        .WithTags("Invitations")
        .Produces<GetInvitationDetailsResponse>(200)
        .Produces(404)
        .Produces(410)
        .AllowAnonymous();

        return app;
    }
}
```

### Step 6: Register Endpoint in Application

**File**: `SantaVibe.Api/Program.cs` (or endpoint configuration file)

```csharp
// In endpoint mapping section
app.MapGetInvitationDetails();
```

### Step 7: Add Unit Tests

**File**: `SantaVibe.Api.Tests/Features/Invitations/GetInvitationDetailsTests.cs`

Test scenarios:
1. Valid invitation token returns 200 with correct data
2. Invalid invitation token returns 404
3. Valid token but draw completed returns 410
4. Verify organizer name is correctly formatted
5. Verify participant count is accurate
6. Verify isValid is always true for found groups

### Step 8: Add Integration Tests

**File**: `SantaVibe.Api.IntegrationTests/Features/Invitations/GetInvitationDetailsIntegrationTests.cs`

Test scenarios:
1. End-to-end request with valid token
2. End-to-end request with invalid token
3. End-to-end request with completed draw
4. Verify no authentication required
5. Performance test with database

### Step 9: Update API Documentation

- Add endpoint to Swagger/OpenAPI documentation
- Verify example responses match specification
- Add endpoint description and tags

### Step 10: Manual Testing Checklist

- [ ] Test with valid invitation token
- [ ] Test with invalid UUID format
- [ ] Test with valid UUID but non-existent token
- [ ] Test with token for group where draw is completed
- [ ] Verify response time is acceptable
- [ ] Verify CORS headers allow frontend access
- [ ] Test rate limiting behavior
- [ ] Verify logging outputs correctly
