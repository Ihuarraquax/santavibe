# API Endpoint Implementation Plan: Update My Wishlist for a Group

## 1. Endpoint Overview

This endpoint allows an authenticated user to create or update their wishlist for a specific Secret Santa group they are participating in. **IMPORTANT: This endpoint is only available after the draw has been completed** - users must know the final budget before creating their wishlists. The wishlist can be set, updated, or cleared (by providing null/empty content). Updating the wishlist will trigger a delayed email notification to the user's assigned Santa (the person who will buy them a gift), informing them of the wishlist update. The notification is delayed by 1 hour and includes deduplication logic to prevent spam.

**Business Value:**
- Enables participants to communicate their gift preferences
- Supports dynamic wishlist updates even after the draw
- Notifies Santas of wishlist changes without revealing identities
- Provides flexibility to clear wishlists if needed

## 2. Request Details

### HTTP Method
`PUT`

### URL Structure
`/api/groups/{groupId}/participants/me/wishlist`

### Path Parameters
| Parameter | Type | Required | Description | Validation |
|-----------|------|----------|-------------|------------|
| `groupId` | UUID | Yes | Unique identifier of the group | Must be valid GUID format |

### Request Body
```json
{
  "wishlistContent": "I would love books about cooking, especially Italian cuisine. Size M for clothes. I also enjoy board games."
}
```

### Request Body Schema
| Field | Type | Required | Constraints | Description |
|-------|------|----------|-------------|-------------|
| `wishlistContent` | string | No | None (TEXT type, nullable) | The user's wishlist content. Can be null or empty to clear the wishlist. |

### Authentication
- **Required**: Yes
- **Type**: JWT Bearer token
- **Claims Used**: User ID (extracted via `IUserAccessor`)

### Authorization
- User must be a participant in the specified group
- Draw must be completed (wishlists can only be created/modified post-draw)

## 3. Used Types

### Request DTOs

**UpdateWishlistRequest.cs**
```csharp
namespace SantaVibe.Api.Features.Wishlists.UpdateWishlist;

/// <summary>
/// Request to update user's wishlist for a group
/// </summary>
public class UpdateWishlistRequest
{
    /// <summary>
    /// Wishlist content (nullable to support clearing wishlist)
    /// </summary>
    public string? WishlistContent { get; init; }
}
```

### Response DTOs

**UpdateWishlistResponse.cs**
```csharp
namespace SantaVibe.Api.Features.Wishlists.UpdateWishlist;

/// <summary>
/// Response after updating wishlist
/// </summary>
public class UpdateWishlistResponse
{
    /// <summary>
    /// Group identifier
    /// </summary>
    public required Guid GroupId { get; init; }

    /// <summary>
    /// Updated wishlist content (null if cleared)
    /// </summary>
    public string? WishlistContent { get; init; }

    /// <summary>
    /// Timestamp of last modification
    /// </summary>
    public required DateTimeOffset LastModified { get; init; }
}
```

### Command Models

**UpdateWishlistCommand.cs**
```csharp
using MediatR;
using SantaVibe.Api.Common;

namespace SantaVibe.Api.Features.Wishlists.UpdateWishlist;

/// <summary>
/// Command to update authenticated user's wishlist for a group
/// </summary>
/// <param name="GroupId">The group identifier</param>
/// <param name="WishlistContent">The new wishlist content (nullable)</param>
public record UpdateWishlistCommand(
    Guid GroupId,
    string? WishlistContent) : IRequest<Result<UpdateWishlistResponse>>;
```

## 4. Response Details

### Success Response (200 OK)

**Status Code**: `200 OK`

**Response Body**:
```json
{
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "wishlistContent": "I would love books about cooking, especially Italian cuisine. Size M for clothes. I also enjoy board games.",
  "lastModified": "2025-10-15T14:45:00Z"
}
```

**Response Schema**:
| Field | Type | Description |
|-------|------|-------------|
| `groupId` | UUID | The group identifier |
| `wishlistContent` | string (nullable) | The updated wishlist content, or null if cleared |
| `lastModified` | ISO 8601 DateTime | UTC timestamp of when wishlist was last modified |

### Error Responses

#### 401 Unauthorized
**Scenario**: Missing or invalid JWT token

**Response**: Handled automatically by ASP.NET Core authentication middleware

---

#### 403 Forbidden - Not a Participant
**Scenario**: User is not a participant in the group

**Response Body**:
```json
{
  "error": "NotParticipant",
  "message": "You are not a participant in this group"
}
```

---

#### 403 Forbidden - Draw Not Completed
**Scenario**: Draw has not been completed for the group

**Response Body**:
```json
{
  "error": "DrawNotCompleted",
  "message": "Wishlist can only be created/modified after the draw has been completed"
}
```

---

#### 404 Not Found
**Scenario**: Group does not exist

**Response Body**:
```json
{
  "error": "GroupNotFound",
  "message": "Group not found"
}
```

---

#### 500 Internal Server Error
**Scenario**: Database error or unexpected exception

**Response Body**:
```json
{
  "error": "InternalServerError",
  "message": "An unexpected error occurred while updating wishlist"
}
```

## 5. Data Flow

### High-Level Flow
```
1. Client sends PUT request with groupId and wishlistContent
2. Endpoint extracts userId from JWT claims
3. Endpoint creates UpdateWishlistCommand
4. MediatR sends command to UpdateWishlistCommandHandler
5. Handler validates group existence and user participation
6. Handler validates draw is completed (Group.DrawCompletedAt != null) - returns 403 if not
7. Handler updates GroupParticipant.WishlistContent and WishlistLastModified
8. Handler finds user's Santa:
   a. Query Assignment to find user's Santa (RecipientUserId = current user)
   b. Check for existing pending WishlistUpdated notification within 1-hour window
   c. If no pending notification, create EmailNotification with 1-hour delay
9. Handler commits transaction
10. Handler returns UpdateWishlistResponse
11. Endpoint returns 200 OK with response body
```

### Database Queries

**Query 1: Fetch Group with Participant Check**
```csharp
var group = await _context.Groups
    .Include(g => g.GroupParticipants.Where(gp => gp.UserId == userId))
    .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);
```

**Query 2: Find User's Santa (if draw completed)**
```csharp
var assignment = await _context.Assignments
    .Where(a => a.GroupId == groupId && a.RecipientUserId == userId)
    .Select(a => new { a.SantaUserId })
    .FirstOrDefaultAsync(cancellationToken);
```

**Query 3: Check for Existing Pending Notification**
```csharp
var oneHourFromNow = DateTimeOffset.UtcNow.AddHours(1);
var existingNotification = await _context.EmailNotifications
    .Where(n =>
        n.Type == EmailNotificationType.WishlistUpdated &&
        n.GroupId == groupId &&
        n.RecipientUserId == santaUserId &&
        n.SentAt == null &&
        n.ScheduledAt <= oneHourFromNow)
    .AnyAsync(cancellationToken);
```

### Database Writes

**Update 1: Update GroupParticipant Wishlist**
```csharp
participant.WishlistContent = command.WishlistContent;
participant.WishlistLastModified = DateTimeOffset.UtcNow;
```

**Insert 1: Create Email Notification (if conditions met)**
```csharp
var notification = new EmailNotification
{
    Id = Guid.NewGuid(),
    Type = EmailNotificationType.WishlistUpdated,
    RecipientUserId = santaUserId,
    GroupId = groupId,
    ScheduledAt = DateTimeOffset.UtcNow.AddHours(1),
    SentAt = null,
    AttemptCount = 0
};
_context.EmailNotifications.Add(notification);
```

## 6. Security Considerations

### Authentication
- Endpoint requires valid JWT Bearer token (enforced via `.RequireAuthorization()`)
- User ID extracted from token claims using `IUserAccessor` service
- No user input accepted for user identification (prevents impersonation)

### Authorization
- User must be a participant in the group (verified by checking GroupParticipants table)
- Returns 403 Forbidden if user is not a participant
- Prevents unauthorized access to other groups' data

### Input Validation
- `groupId` validated as UUID by route model binding
- `wishlistContent` has no length limit in MVP (TEXT type)
- No sanitization needed as content is stored as-is and displayed as plain text (XSS protection on frontend)

### Data Privacy
- User can only update their own wishlist (userId from token, not request body)
- Email notification sent only to user's assigned Santa (not visible to others)
- No exposure of Santa identity to the recipient

## 7. Error Handling

### Error Scenarios and Handling Strategy

| Error Scenario | Detection Method | Status Code | Error Code | Handler Action |
|----------------|------------------|-------------|------------|----------------|
| Invalid JWT token | ASP.NET Core middleware | 401 | - | Automatic rejection before endpoint |
| Missing JWT token | ASP.NET Core middleware | 401 | - | Automatic rejection before endpoint |
| Group not found | `group == null` after query | 404 | GroupNotFound | Return Result.Failure |
| User not participant | `group.GroupParticipants.Count == 0` | 403 | NotParticipant | Return Result.Failure |
| Draw not completed | `!group.DrawCompletedAt.HasValue` | 403 | DrawNotCompleted | Return Result.Failure |
| Database connection error | Exception during SaveChanges | 500 | InternalServerError | Log error, rollback, return Failure |
| Transaction failure | Exception in transaction block | 500 | InternalServerError | Automatic rollback, log error |
| General exception | Any unhandled exception | 500 | InternalServerError | Catch-all handler, log error |

### Logging Strategy

**Information Level:**
- Successful wishlist update: `"User {UserId} updated wishlist for group {GroupId}"`
- Email notification created: `"Scheduled wishlist update notification for Santa {SantaUserId} in group {GroupId}"`
- Email notification skipped: `"Skipped duplicate notification for Santa {SantaUserId} in group {GroupId}"`

**Warning Level:**
- User not participant: `"User {UserId} attempted to update wishlist for group {GroupId} without being a participant"`
- Group not found: `"User {UserId} attempted to update wishlist for non-existent group {GroupId}"`

**Error Level:**
- Database errors: `"Error updating wishlist for user {UserId} in group {GroupId}: {ErrorMessage}"`
- Transaction failures: `"Transaction failed while updating wishlist for user {UserId} in group {GroupId}"`

## 8. Performance Considerations

### Potential Bottlenecks
1. **Multiple Database Queries**: Separate queries for group, assignment, and notification check
   - **Mitigation**: Use Include() to fetch related entities in single query where possible

2. **Transaction Overhead**: Using ExecutionStrategy with explicit transactions
   - **Mitigation**: Keep transaction scope minimal, only around writes

3. **Notification Deduplication Query**: Query to check existing pending notifications
   - **Mitigation**: Index on EmailNotifications (Type, GroupId, RecipientUserId, SentAt, ScheduledAt)

### Optimization Strategies
- Use `.AsNoTracking()` for read-only queries (assignment and notification check)
- Use projection (Select) to fetch only needed fields
- Index recommendations:
  - `GroupParticipants (GroupId, UserId)` - composite primary key (already indexed)
  - `Assignments (GroupId, RecipientUserId)` - for Santa lookup
  - `EmailNotifications (Type, GroupId, RecipientUserId, SentAt, ScheduledAt)` - for deduplication

### Expected Performance
- Single user wishlist update: < 100ms
- With notification creation: < 150ms
- Database round trips: 2-4 queries depending on draw status

## 9. Implementation Steps

### Step 1: Create Feature Folder Structure
Create the following folder structure under `SantaVibe.Api/Features/`:
```
Wishlists/
  UpdateWishlist/
    UpdateWishlistEndpoint.cs
    UpdateWishlistRequest.cs
    UpdateWishlistResponse.cs
    UpdateWishlistCommand.cs
    UpdateWishlistCommandHandler.cs
```

### Step 2: Implement Request DTO
**File**: `UpdateWishlistRequest.cs`

Create the request DTO with:
- `WishlistContent` property (string?, nullable)
- XML documentation comments

### Step 3: Implement Response DTO
**File**: `UpdateWishlistResponse.cs`

Create the response DTO with:
- `GroupId` property (Guid, required)
- `WishlistContent` property (string?, nullable)
- `LastModified` property (DateTimeOffset, required)
- XML documentation comments

### Step 4: Implement Command Model
**File**: `UpdateWishlistCommand.cs`

Create the command record with:
- Constructor parameters: `GroupId` (Guid), `WishlistContent` (string?)
- Implements `IRequest<Result<UpdateWishlistResponse>>`
- XML documentation comments

### Step 5: Implement Command Handler
**File**: `UpdateWishlistCommandHandler.cs`

Implement `IRequestHandler<UpdateWishlistCommand, Result<UpdateWishlistResponse>>` with:

**Dependencies (constructor injection)**:
- `ApplicationDbContext _context`
- `IUserAccessor _userAccessor`
- `ILogger<UpdateWishlistCommandHandler> _logger`

**Handler Logic** (in Handle method):

1. **Extract User ID**:
   ```csharp
   var userId = _userAccessor.GetCurrentUserId();
   ```

2. **Validate Group and Participation**:
   ```csharp
   var group = await _context.Groups
       .Include(g => g.GroupParticipants.Where(gp => gp.UserId == userId))
       .FirstOrDefaultAsync(g => g.Id == command.GroupId, cancellationToken);

   if (group == null)
   {
       _logger.LogWarning("Group {GroupId} not found", command.GroupId);
       return Result<UpdateWishlistResponse>.Failure(
           "GroupNotFound",
           "Group not found");
   }

   var participant = group.GroupParticipants.FirstOrDefault();
   if (participant == null)
   {
       _logger.LogWarning(
           "User {UserId} is not a participant in group {GroupId}",
           userId,
           command.GroupId);
       return Result<UpdateWishlistResponse>.Failure(
           "NotParticipant",
           "You are not a participant in this group");
   }

   // Validate draw is completed (wishlists can only be created/modified after draw)
   if (!group.DrawCompletedAt.HasValue)
   {
       _logger.LogWarning(
           "User {UserId} attempted to update wishlist for group {GroupId} before draw completion",
           userId,
           command.GroupId);
       return Result<UpdateWishlistResponse>.Failure(
           "DrawNotCompleted",
           "Wishlist can only be created/modified after the draw has been completed");
   }
   ```

3. **Update Wishlist** (within transaction):
   ```csharp
   var strategy = _context.Database.CreateExecutionStrategy();

   return await strategy.ExecuteAsync(async () =>
   {
       using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

       try
       {
           var lastModified = DateTimeOffset.UtcNow;

           participant.WishlistContent = command.WishlistContent;
           participant.WishlistLastModified = lastModified;

           // Handle notification (draw is always completed at this point due to earlier validation)
           await HandleWishlistUpdateNotificationAsync(
               command.GroupId,
               userId,
               cancellationToken);

           await _context.SaveChangesAsync(cancellationToken);
           await transaction.CommitAsync(cancellationToken);

           _logger.LogInformation(
               "User {UserId} updated wishlist for group {GroupId}",
               userId,
               command.GroupId);

           return Result<UpdateWishlistResponse>.Success(
               new UpdateWishlistResponse
               {
                   GroupId = command.GroupId,
                   WishlistContent = command.WishlistContent,
                   LastModified = lastModified
               });
       }
       catch
       {
           await transaction.RollbackAsync(cancellationToken);
           throw;
       }
   });
   ```

4. **Handle Email Notification** (private method):
   ```csharp
   private async Task HandleWishlistUpdateNotificationAsync(
       Guid groupId,
       string userId,
       CancellationToken cancellationToken)
   {
       // Find the user's Santa
       var assignment = await _context.Assignments
           .AsNoTracking()
           .Where(a => a.GroupId == groupId && a.RecipientUserId == userId)
           .Select(a => new { a.SantaUserId })
           .FirstOrDefaultAsync(cancellationToken);

       if (assignment == null)
       {
           // No assignment found (shouldn't happen if draw completed)
           _logger.LogWarning(
               "No assignment found for user {UserId} in group {GroupId}",
               userId,
               groupId);
           return;
       }

       // Check for existing pending notification within 1-hour window
       var oneHourFromNow = DateTimeOffset.UtcNow.AddHours(1);
       var hasPendingNotification = await _context.EmailNotifications
           .AsNoTracking()
           .Where(n =>
               n.Type == EmailNotificationType.WishlistUpdated &&
               n.GroupId == groupId &&
               n.RecipientUserId == assignment.SantaUserId &&
               n.SentAt == null &&
               n.ScheduledAt <= oneHourFromNow)
           .AnyAsync(cancellationToken);

       if (hasPendingNotification)
       {
           _logger.LogInformation(
               "Skipped duplicate wishlist notification for Santa {SantaUserId} in group {GroupId}",
               assignment.SantaUserId,
               groupId);
           return;
       }

       // Create new email notification
       var notification = new EmailNotification
       {
           Id = Guid.NewGuid(),
           Type = EmailNotificationType.WishlistUpdated,
           RecipientUserId = assignment.SantaUserId,
           GroupId = groupId,
           ScheduledAt = DateTimeOffset.UtcNow.AddHours(1),
           SentAt = null,
           AttemptCount = 0
       };

       _context.EmailNotifications.Add(notification);

       _logger.LogInformation(
           "Scheduled wishlist update notification for Santa {SantaUserId} in group {GroupId}",
           assignment.SantaUserId,
           groupId);
   }
   ```

5. **Exception Handling** (catch block around entire handler):
   ```csharp
   catch (Exception ex)
   {
       _logger.LogError(
           ex,
           "Error updating wishlist for user {UserId} in group {GroupId}: {ErrorMessage}",
           userId,
           command.GroupId,
           ex.Message);

       return Result<UpdateWishlistResponse>.Failure(
           "InternalServerError",
           "An unexpected error occurred while updating wishlist");
   }
   ```

### Step 6: Implement Minimal API Endpoint
**File**: `UpdateWishlistEndpoint.cs`

Create static class with extension method:

```csharp
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SantaVibe.Api.Common;

namespace SantaVibe.Api.Features.Wishlists.UpdateWishlist;

/// <summary>
/// Minimal API endpoint for updating user's wishlist in a group
/// </summary>
public static class UpdateWishlistEndpoint
{
    public static void MapUpdateWishlistEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPut("/api/groups/{groupId}/participants/me/wishlist", async (
                [FromRoute] Guid groupId,
                [FromBody] UpdateWishlistRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateWishlistCommand(
                    groupId,
                    request.WishlistContent);

                var result = await sender.Send(command, cancellationToken);

                if (result.IsSuccess)
                {
                    return Results.Ok(result.Value);
                }

                return result.ToProblem();
            })
            .RequireAuthorization()
            .WithName("UpdateWishlist")
            .WithTags("Wishlists")
            .Produces<UpdateWishlistResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}
```

**Notes**:
- No ValidationFilter needed (no validation rules in MVP)
- Uses `.RequireAuthorization()` for JWT authentication
- Maps result to appropriate HTTP responses using `result.ToProblem()`

### Step 7: Register Endpoint in Program.cs
In `Program.cs`, add endpoint registration:

```csharp
// Register wishlist endpoints
app.MapUpdateWishlistEndpoint();
```

Group with other wishlist-related endpoints if they exist.

### Step 8: Add Database Indexes (if not already present)
In the `ApplicationDbContext` configuration:

```csharp
// EmailNotifications index for efficient notification deduplication
modelBuilder.Entity<EmailNotification>()
    .HasIndex(e => new { e.Type, e.GroupId, e.RecipientUserId, e.SentAt, e.ScheduledAt })
    .HasDatabaseName("IX_EmailNotifications_Deduplication");

// Assignments index for efficient Santa lookup
modelBuilder.Entity<Assignment>()
    .HasIndex(e => new { e.GroupId, e.RecipientUserId })
    .HasDatabaseName("IX_Assignments_GroupRecipient");
```

Create and apply migration:
```bash
dotnet ef migrations add AddWishlistUpdateIndexes
dotnet ef database update
```

### Step 9: Write Unit Tests
**File**: `UpdateWishlistCommandHandlerTests.cs`

Test cases to implement:
1. ✅ **Success**: Update wishlist before draw (no notification)
2. ✅ **Success**: Update wishlist after draw (with notification)
3. ✅ **Success**: Clear wishlist (set to null)
4. ✅ **Success**: Skip duplicate notification (deduplication)
5. ❌ **Failure**: Group not found (404)
6. ❌ **Failure**: User not participant (403)
7. ❌ **Exception**: Database error (500)

Use:
- xUnit for test framework
- NSubstitute for mocking dependencies
- Verify.Xunit for snapshot testing responses
- TestContainers for integration tests with real database

### Step 10: Write Integration Tests
**File**: `UpdateWishlistEndpointTests.cs`

Test the full endpoint flow:
1. Setup test database with group, participants, assignments
2. Authenticate test user with JWT
3. Call endpoint with valid/invalid data
4. Verify response status codes and body
5. Verify database state changes
6. Verify email notification creation/deduplication

### Step 11: Test Manually
1. Start the application
2. Use Swagger UI or Postman to test endpoint
3. Test scenarios:
   - Update wishlist before draw
   - Update wishlist after draw
   - Verify notification created
   - Update again quickly (verify deduplication)
   - Clear wishlist (null content)
   - Test error cases (invalid group, non-participant)
4. Verify logs for proper information/warning/error messages

### Step 12: Update API Documentation
Update Swagger documentation with:
- Endpoint description
- Request/response examples
- Error response examples
- Authentication requirements

This is typically auto-generated from XML comments and attributes.

## 10. Additional Notes

### Dependencies Required
- `MediatR` - for CQRS pattern
- `Microsoft.EntityFrameworkCore` - for database access
- `Microsoft.AspNetCore.Authentication.JwtBearer` - for JWT authentication

### Related Endpoints
This endpoint is part of wishlist management feature. Related endpoints:
- `GET /api/groups/{groupId}/participants/me/wishlist` - Get own wishlist
- `GET /api/groups/{groupId}/my-assignment/wishlist` - Get recipient's wishlist (after draw)

### Future Enhancements
- Add wishlist character limit with validation
- Add rich text formatting support
- Add wishlist versioning/history
- Add real-time notification via SignalR
- Add notification preferences (immediate vs batched)
- Support image attachments in wishlist

### Business Rules Summary
1. User must be authenticated and a participant to update wishlist
2. Wishlist can be updated at any time (before or after draw)
3. If draw completed, Santa is notified of wishlist updates
4. Notifications are delayed 1 hour to avoid spam
5. Duplicate notifications within 1-hour window are prevented
6. Wishlist can be cleared by providing null/empty content
