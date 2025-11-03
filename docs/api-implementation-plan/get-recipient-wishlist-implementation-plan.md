# API Endpoint Implementation Plan: Get Recipient's Wishlist

## 1. Endpoint Overview

This endpoint allows an authenticated user (the "Santa") to retrieve the wishlist of their assigned gift recipient after a Secret Santa draw has been completed. The endpoint enforces strict authorization to ensure:
- Only participants can access group data
- Wishlists are only accessible after draw completion
- Users can only see their assigned recipient's wishlist (not other participants' wishlists)

**Business Context**: After a draw is completed, participants need to view their assigned recipient's wishlist to choose an appropriate gift within the established budget.

## 2. Request Details

- **HTTP Method**: GET
- **URL Structure**: `/api/groups/{groupId}/my-assignment/wishlist`
- **Route Constraints**: `{groupId:guid}` (enforces GUID format)
- **Authentication**: Required - JWT Bearer token
- **Authorization**: User must be a participant in the group AND draw must be completed

### Parameters

**Required Path Parameters**:
- `groupId` (Guid): The unique identifier of the Secret Santa group

**No Query Parameters**

**No Request Body**

## 3. Used Types

### Query Model
```csharp
// File: GetRecipientWishlistQuery.cs
public record GetRecipientWishlistQuery(Guid GroupId) : IRequest<Result<GetRecipientWishlistResponse>>;
```

### Response DTO
```csharp
// File: GetRecipientWishlistResponse.cs
public class GetRecipientWishlistResponse
{
    public required Guid GroupId { get; init; }
    public required string RecipientId { get; init; }
    public required string RecipientFirstName { get; init; }
    public required string RecipientLastName { get; init; }
    public string? WishlistContent { get; init; }
    public DateTimeOffset? LastModified { get; init; }
}
```

### Handler Dependencies
- `ApplicationDbContext`: Database access
- `IUserAccessor`: Extract current user ID from JWT claims

## 4. Response Details

### Success Response (200 OK)

**With Wishlist Content**:
```json
{
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "recipientId": "770g0622-g41d-63g6-c938-668877662222",
  "recipientFirstName": "Maria",
  "recipientLastName": "Wiśniewska",
  "wishlistContent": "I love mystery novels, especially Agatha Christie. I also enjoy gardening tools and flower seeds.",
  "lastModified": "2025-10-20T16:00:00Z"
}
```

**Empty Wishlist**:
```json
{
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "recipientId": "770g0622-g41d-63g6-c938-668877662222",
  "recipientFirstName": "Maria",
  "recipientLastName": "Wiśniewska",
  "wishlistContent": null,
  "lastModified": null
}
```

### Error Responses

| Status Code | Error Code | Message | Scenario |
|-------------|------------|---------|----------|
| 401 | Unauthorized | Missing or invalid token | No JWT or invalid JWT |
| 403 | NotAParticipant | You are not a participant in this group | User not in group |
| 403 | DrawNotCompleted | Draw has not been completed yet. You cannot view recipient wishlist. | Draw not executed |
| 404 | GroupNotFound | Group does not exist | Invalid groupId |
| 404 | AssignmentNotFound | No assignment found for this user | User has no assignment (shouldn't happen if draw completed) |

## 5. Data Flow

### High-Level Flow
```
1. User Request → Endpoint
2. Extract groupId from route
3. Get currentUserId from JWT (IUserAccessor)
4. Query Database:
   a. Load Group with GroupParticipants
   b. Verify group exists
   c. Verify user is participant
   d. Verify draw is completed
5. Query Assignment for current user as Santa
6. Verify assignment exists
7. Load recipient's wishlist from GroupParticipant
8. Build response DTO
9. Return 200 OK with response
```

### Database Queries

**Query 1: Load Group with Participants**
```csharp
var group = await context.Groups
    .AsNoTracking()
    .Include(g => g.GroupParticipants)
    .FirstOrDefaultAsync(g => g.Id == request.GroupId, cancellationToken);
```

**Query 2: Load User's Assignment with Recipient Details**
```csharp
var assignment = await context.Assignments
    .AsNoTracking()
    .Include(a => a.Recipient)
    .FirstOrDefaultAsync(a =>
        a.GroupId == request.GroupId &&
        a.SantaUserId == currentUserId.ToString(),
        cancellationToken);
```

**Query 3: Load Recipient's Wishlist**
```csharp
var recipientWishlist = await context.GroupParticipants
    .AsNoTracking()
    .FirstOrDefaultAsync(gp =>
        gp.GroupId == request.GroupId &&
        gp.UserId == assignment.RecipientUserId,
        cancellationToken);
```

### Entity Relationships Used
- `Group` → `GroupParticipants` (1:many)
- `Assignment` → `Recipient` (ApplicationUser) (many:1)
- `GroupParticipant` → stores wishlist data

## 6. Security Considerations

### Authentication
- Endpoint requires `[RequireAuthorization]` attribute
- JWT Bearer token must be present and valid
- Token expiration enforced by ASP.NET Core Identity

### Authorization Checks (in order)
1. **Group Existence**: Verify group exists (404 if not)
2. **Participant Verification**: User must be in GroupParticipants list (403 if not)
3. **Draw Completion**: Group.DrawCompletedAt must not be null (403 if null)
4. **Assignment Existence**: Assignment must exist for user as Santa (404 if not)

### Privacy Rules
- Users can **only** view their own recipient's wishlist
- Organizers have **no special access** (same restrictions)
- No endpoint to view all wishlists
- No reverse lookup (can't find who is Santa for a specific recipient)

### Input Validation
- `groupId` format validated by route constraint `:guid`
- No user-controlled input beyond groupId
- CurrentUserId extracted from authenticated JWT claims (trusted source)

## 7. Error Handling

### Error Code Mapping (via ProblemDetailsExtensions)
The existing `ToProblem()` extension handles error mapping:
- `GroupNotFound` → 404 Not Found
- `NotAParticipant` → 403 Forbidden
- `DrawNotCompleted` → 403 Forbidden
- `AssignmentNotFound` → 404 Not Found

### Validation Sequence
```
1. Check group exists → GroupNotFound (404)
2. Check user is participant → NotAParticipant (403)
3. Check draw completed → DrawNotCompleted (403)
4. Check assignment exists → AssignmentNotFound (404)
5. Load wishlist (may be null - valid state)
6. Return success response
```

### Edge Cases
- **Empty Wishlist**: Valid state, return `wishlistContent: null`, `lastModified: null`
- **Missing Assignment**: Shouldn't happen if draw completed, but return 404 for safety
- **User Not Participant But Draw Completed**: Return 403 Forbidden before checking assignment

## 8. Performance Considerations

### Database Query Optimization
- Use `AsNoTracking()` for read-only queries (better performance)
- Use selective `Include()` for navigation properties
- Avoid N+1 queries by including related entities upfront

### Potential Bottlenecks
- Multiple sequential database queries (3 queries)
- Can be optimized with a single complex query if needed in the future

### Optimization Opportunities (Future)
- Combine queries into a single LINQ query with multiple joins
- Add database indexes:
  - `IX_Assignments_GroupId_SantaUserId` (composite index)
  - `IX_GroupParticipants_GroupId_UserId` (composite index)

### Expected Performance
- Query time: < 50ms for typical group size (< 30 participants)
- No pagination needed (single wishlist response)

## 9. Implementation Steps

### Step 1: Create Query Model
**File**: `SantaVibe.Api/Features/Groups/GetRecipientWishlist/GetRecipientWishlistQuery.cs`

```csharp
using MediatR;
using SantaVibe.Api.Common;

namespace SantaVibe.Api.Features.Groups.GetRecipientWishlist;

/// <summary>
/// Query to retrieve the wishlist of the authenticated user's assigned gift recipient
/// Only available after draw completion
/// </summary>
public record GetRecipientWishlistQuery(Guid GroupId)
    : IRequest<Result<GetRecipientWishlistResponse>>;
```

### Step 2: Create Response DTO
**File**: `SantaVibe.Api/Features/Groups/GetRecipientWishlist/GetRecipientWishlistResponse.cs`

```csharp
namespace SantaVibe.Api.Features.Groups.GetRecipientWishlist;

/// <summary>
/// Response containing the recipient's wishlist information
/// </summary>
public class GetRecipientWishlistResponse
{
    /// <summary>
    /// Group identifier
    /// </summary>
    public required Guid GroupId { get; init; }

    /// <summary>
    /// Recipient user ID
    /// </summary>
    public required string RecipientId { get; init; }

    /// <summary>
    /// Recipient's first name
    /// </summary>
    public required string RecipientFirstName { get; init; }

    /// <summary>
    /// Recipient's last name
    /// </summary>
    public required string RecipientLastName { get; init; }

    /// <summary>
    /// Wishlist content (null if recipient hasn't created a wishlist)
    /// </summary>
    public string? WishlistContent { get; init; }

    /// <summary>
    /// Last modification timestamp (null if wishlist empty)
    /// </summary>
    public DateTimeOffset? LastModified { get; init; }
}
```

### Step 3: Create Query Handler
**File**: `SantaVibe.Api/Features/Groups/GetRecipientWishlist/GetRecipientWishlistQueryHandler.cs`

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using SantaVibe.Api.Common;
using SantaVibe.Api.Data;
using SantaVibe.Api.Services;

namespace SantaVibe.Api.Features.Groups.GetRecipientWishlist;

/// <summary>
/// Handler for GetRecipientWishlistQuery
/// Retrieves the assigned recipient's wishlist with authorization checks
/// </summary>
public class GetRecipientWishlistQueryHandler(
    ApplicationDbContext context,
    IUserAccessor userAccessor)
    : IRequestHandler<GetRecipientWishlistQuery, Result<GetRecipientWishlistResponse>>
{
    public async Task<Result<GetRecipientWishlistResponse>> Handle(
        GetRecipientWishlistQuery request,
        CancellationToken cancellationToken)
    {
        // Get current user ID from JWT token
        var currentUserId = userAccessor.GetCurrentUserId();

        // Query group with participants to verify access
        var group = await context.Groups
            .AsNoTracking()
            .Include(g => g.GroupParticipants)
            .FirstOrDefaultAsync(g => g.Id == request.GroupId, cancellationToken);

        // Check if group exists
        if (group == null)
        {
            return Result<GetRecipientWishlistResponse>.Failure(
                "GroupNotFound",
                "Group does not exist"
            );
        }

        // Check if user is a participant
        var isParticipant = group.GroupParticipants
            .Any(gp => gp.UserId == currentUserId.ToString());

        if (!isParticipant)
        {
            return Result<GetRecipientWishlistResponse>.Failure(
                "NotAParticipant",
                "You are not a participant in this group"
            );
        }

        // Check if draw has been completed
        if (group.DrawCompletedAt == null)
        {
            return Result<GetRecipientWishlistResponse>.Failure(
                "DrawNotCompleted",
                "Draw has not been completed yet. You cannot view recipient wishlist."
            );
        }

        // Query user's assignment (where they are the Santa)
        var assignment = await context.Assignments
            .AsNoTracking()
            .Include(a => a.Recipient)
            .FirstOrDefaultAsync(a =>
                a.GroupId == request.GroupId &&
                a.SantaUserId == currentUserId.ToString(),
                cancellationToken);

        // Check if assignment exists
        if (assignment == null)
        {
            return Result<GetRecipientWishlistResponse>.Failure(
                "AssignmentNotFound",
                "No assignment found for this user"
            );
        }

        // Query recipient's wishlist from GroupParticipant
        var recipientParticipant = await context.GroupParticipants
            .AsNoTracking()
            .FirstOrDefaultAsync(gp =>
                gp.GroupId == request.GroupId &&
                gp.UserId == assignment.RecipientUserId,
                cancellationToken);

        // Build response (wishlist may be null if recipient hasn't created one)
        var response = new GetRecipientWishlistResponse
        {
            GroupId = request.GroupId,
            RecipientId = assignment.RecipientUserId,
            RecipientFirstName = assignment.Recipient.FirstName,
            RecipientLastName = assignment.Recipient.LastName,
            WishlistContent = recipientParticipant?.WishlistContent,
            LastModified = recipientParticipant?.WishlistLastModified
        };

        return Result<GetRecipientWishlistResponse>.Success(response);
    }
}
```

### Step 4: Create Endpoint Registration
**File**: `SantaVibe.Api/Features/Groups/GetRecipientWishlist/GetRecipientWishlistEndpoint.cs`

```csharp
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SantaVibe.Api.Common;

namespace SantaVibe.Api.Features.Groups.GetRecipientWishlist;

/// <summary>
/// Endpoint registration for GET /api/groups/{groupId}/my-assignment/wishlist
/// Retrieves the wishlist of the authenticated user's assigned gift recipient
/// </summary>
public static class GetRecipientWishlistEndpoint
{
    public static void MapGetRecipientWishlistEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/groups/{groupId:guid}/my-assignment/wishlist", async (
                Guid groupId,
                ISender sender) =>
            {
                var query = new GetRecipientWishlistQuery(groupId);
                var result = await sender.Send(query);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
            })
            .RequireAuthorization()
            .WithTags("Groups")
            .WithName("GetRecipientWishlist")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Get recipient's wishlist";
                operation.Description =
                    "Retrieves the wishlist of the authenticated user's assigned gift recipient. " +
                    "Only available after draw completion. " +
                    "Returns recipient's name, wishlist content, and last modification date.";
                return operation;
            })
            .Produces<GetRecipientWishlistResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}
```

### Step 5: Register Endpoint in Program.cs
**File**: `SantaVibe.Api/Program.cs`

Add the endpoint registration with other group endpoints:

```csharp
// In the endpoint registration section
app.MapGetRecipientWishlistEndpoint();
```

### Step 6: Add Error Code Mapping (if not already present)
**File**: `SantaVibe.Api/Common/ProblemDetailsExtensions.cs`

Verify these error codes are mapped (should already exist based on existing code):
- `GroupNotFound` → 404
- `NotAParticipant` → 403
- `DrawNotCompleted` → 403
- `AssignmentNotFound` → 404

### Step 7: Write Unit Tests
**File**: `SantaVibe.Api.Tests/Features/Groups/GetRecipientWishlistTests.cs`

Test cases to implement:
1. ✅ **Success**: Returns wishlist for valid Santa-Recipient assignment
2. ✅ **Success**: Returns null wishlist when recipient hasn't created one
3. ❌ **404**: Group doesn't exist
4. ❌ **403**: User is not a participant
5. ❌ **403**: Draw has not been completed
6. ❌ **404**: User has no assignment (edge case)

### Step 8: Integration Testing
**Test Scenarios**:
1. Authenticate as a user
2. Create a group and add participants
3. Execute draw
4. Call endpoint as Santa → Should return recipient's wishlist
5. Call endpoint as non-participant → Should return 403
6. Call endpoint before draw → Should return 403

### Step 9: Documentation Update
- Verify Swagger/OpenAPI documentation is generated correctly
- Ensure response examples appear in Swagger UI
- Validate all error responses are documented

## 10. Checklist

- [ ] Create GetRecipientWishlistQuery.cs
- [ ] Create GetRecipientWishlistResponse.cs
- [ ] Create GetRecipientWishlistQueryHandler.cs
- [ ] Create GetRecipientWishlistEndpoint.cs
- [ ] Register endpoint in Program.cs
- [ ] Verify error code mappings in ProblemDetailsExtensions.cs
- [ ] Write unit tests for handler
- [ ] Write integration tests
- [ ] Test with Swagger UI
- [ ] Verify authorization checks work correctly
- [ ] Test empty wishlist scenario
- [ ] Verify performance with database query profiling
