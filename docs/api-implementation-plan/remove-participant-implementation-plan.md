# API Endpoint Implementation Plan: Remove Participant from Group

## 1. Endpoint Overview

This endpoint allows a group organizer to remove a participant from their Secret Santa group before the draw has been completed. It enforces critical business rules: the organizer cannot remove themselves, and participants cannot be removed after the draw has been executed. This is a destructive operation that may also clean up related data such as exclusion rules involving the removed participant.

**Business Context:**
- Enables organizers to manage group membership during the planning phase
- Maintains group integrity by preventing removal after draw completion
- Protects organizer role by preventing self-removal

## 2. Request Details

- **HTTP Method**: DELETE
- **URL Structure**: `/api/groups/{groupId}/participants/{userId}`
- **Authentication**: Required (JWT Bearer token)
- **Authorization**: Requesting user must be the group organizer

### Parameters

**Path Parameters (Required):**
- `groupId` (Guid): UUID of the group
- `userId` (string): Identifier of the user to remove

**Request Body:** None

**Headers:**
- `Authorization: Bearer {jwt_token}` (Required)

## 3. Used Types

### Command Model
```csharp
public record RemoveParticipantCommand(
    Guid GroupId,
    string UserIdToRemove,
    string RequestingUserId
);
```

### Error Response DTOs
```csharp
public record ErrorResponse(
    string Error,
    string Message
);
```

### Database Entities
- `Group` - from Groups table
- `GroupParticipant` - from GroupParticipants table
- `ExclusionRule` - from ExclusionRules table (for cleanup)
- `ApplicationUser` - from AspNetUsers table

## 4. Response Details

### Success Response
- **Status Code**: 204 No Content
- **Body**: Empty

### Error Responses

**400 Bad Request - Cannot Remove Organizer:**
```json
{
  "error": "CannotRemoveOrganizer",
  "message": "The organizer cannot be removed from the group"
}
```

**400 Bad Request - Draw Already Completed:**
```json
{
  "error": "DrawAlreadyCompleted",
  "message": "Cannot remove participants after draw has been completed"
}
```

**401 Unauthorized:**
```json
{
  "error": "Unauthorized",
  "message": "Missing or invalid token"
}
```

**403 Forbidden:**
```json
{
  "error": "Forbidden",
  "message": "User is not the organizer"
}
```

**404 Not Found:**
```json
{
  "error": "NotFound",
  "message": "Group or participant not found"
}
```

## 5. Data Flow

### High-Level Flow
1. Extract JWT claims to get requesting user ID
2. Validate path parameters (groupId, userId formats)
3. Query database for group entity
4. Verify requesting user is the organizer
5. Verify draw hasn't been completed
6. Verify participant exists in the group
7. Verify user to remove is not the organizer
8. Remove participant from GroupParticipants table
9. Clean up related ExclusionRules (if any)
10. Commit transaction
11. Return 204 No Content

### Database Interactions

**Queries:**
1. `Groups` table: Fetch group by groupId with organizer information
2. `GroupParticipants` table: Verify participant exists
3. `ExclusionRules` table: Find rules involving the user being removed

**Commands:**
1. DELETE from `GroupParticipants` WHERE GroupId = @groupId AND UserId = @userId
2. DELETE from `ExclusionRules` WHERE GroupId = @groupId AND (UserId1 = @userId OR UserId2 = @userId)

### Transaction Management
- Use database transaction to ensure atomic operation
- If participant removal fails, rollback exclusion rule cleanup
- If exclusion rule cleanup fails, rollback participant removal

## 6. Security Considerations

### Authentication
- JWT token required in Authorization header
- Extract user ID from JWT claims (not from request parameters)
- Validate token signature and expiration

### Authorization
- Verify requesting user ID matches Group.OrganizerUserId
- Return 403 if user is not the organizer
- Don't reveal group existence to unauthorized users (return 404)

### Input Validation
- Validate groupId is a valid Guid format
- Validate userId is not null or empty
- Sanitize all inputs to prevent injection attacks

### Data Protection
- Don't expose information about other participants unnecessarily
- Log security events (unauthorized access attempts)

## 7. Error Handling

### Validation Errors (400 Bad Request)

**Scenario 1: Attempting to remove organizer**
- Check: `userIdToRemove == group.OrganizerUserId`
- Response: CannotRemoveOrganizer error

**Scenario 2: Draw already completed**
- Check: `group.DrawCompletedAt != null`
- Response: DrawAlreadyCompleted error

**Scenario 3: Invalid UUID format**
- Check: `Guid.TryParse(groupId, out _)`
- Response: ValidationError

### Authorization Errors

**Scenario 1: Missing/Invalid JWT (401)**
- Handled by authentication middleware
- Return generic Unauthorized error

**Scenario 2: User is not organizer (403)**
- Check: `requestingUserId != group.OrganizerUserId`
- Response: Forbidden error
- Log security event

### Not Found Errors (404)

**Scenario 1: Group doesn't exist**
- Check: `group == null`
- Response: NotFound error

**Scenario 2: Participant not in group**
- Check: `participant == null`
- Response: NotFound error

### Server Errors (500)

**Scenario 1: Database connection failure**
- Catch: DbException
- Log: Full error details with Serilog
- Response: Generic internal server error

**Scenario 2: Transaction failure**
- Catch: DbUpdateException
- Log: Full error details
- Response: Generic internal server error

### Error Logging Strategy
- 400 errors: Log as Information with business context
- 403 errors: Log as Warning for security monitoring
- 404 errors: Log as Information
- 500 errors: Log as Error with full stack trace

## 8. Performance Considerations

### Database Optimization
- Use eager loading for group with organizer: `Include(g => g.Organizer)`
- Index on GroupParticipants composite key (GroupId, UserId)
- Index on ExclusionRules for cleanup query

### Query Efficiency
- Single query to fetch group with organizer
- Single query to check participant existence
- Batch delete for exclusion rules if multiple exist

### Potential Bottlenecks
- Multiple exclusion rules for popular participant
- Large groups with many participants (low risk in MVP)

### Optimization Strategies
- Use compiled queries for frequently executed operations
- Consider soft delete pattern if undo functionality needed later
- Use AsNoTracking() for read-only verification queries

## 9. Implementation Steps

### Step 1: Create Command Handler
1. Create `RemoveParticipantCommand` record in `Features/Groups/RemoveParticipant/`
2. Create `RemoveParticipantHandler` implementing `IRequestHandler<RemoveParticipantCommand>`
3. Inject `ApplicationDbContext` and `ILogger<RemoveParticipantHandler>`

### Step 2: Implement Handler Logic
1. Query group by groupId with eager loading:
   ```csharp
   var group = await context.Groups
       .Include(g => g.Participants)
       .FirstOrDefaultAsync(g => g.Id == command.GroupId);
   ```
2. Return 404 if group is null
3. Verify requesting user is organizer, return 403 if not
4. Verify draw not completed, return 400 DrawAlreadyCompleted if completed
5. Verify userId to remove is not organizer, return 400 CannotRemoveOrganizer if is organizer
6. Find participant in group, return 404 if not found
7. Begin transaction
8. Remove participant from context
9. Remove related exclusion rules
10. Save changes and commit transaction
11. Return success result

### Step 3: Create Minimal API Endpoint
1. In `Features/Groups/RemoveParticipant/`, create endpoint mapping
2. Register endpoint in `GroupsEndpoints.cs` or `Program.cs`
3. Map route: `DELETE /api/groups/{groupId}/participants/{userId}`
4. Apply authentication/authorization filters
5. Extract user ID from JWT claims: `ClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)`
6. Create and send command to handler
7. Map handler results to HTTP responses

### Step 4: Implement Error Response Mapping
1. Create endpoint filter for standardized error responses
2. Map domain exceptions to HTTP status codes
3. Use ErrorResponse DTO for consistent error format
4. Include appropriate error codes and messages per API spec

### Step 5: Add Input Validation
1. Validate groupId is valid Guid using `Guid.TryParse()`
2. Validate userId is not null or empty
3. Return 400 with ValidationError if validation fails
4. Consider using FluentValidation if not already in project

### Step 6: Add Logging
1. Log endpoint entry with groupId and userId
2. Log validation failures as Information
3. Log authorization failures as Warning
4. Log business rule violations as Information
5. Log unexpected errors as Error with full context
6. Log successful removal as Information

### Step 7: Add Unit Tests
1. Test successful participant removal
2. Test 404 when group not found
3. Test 404 when participant not found
4. Test 403 when user is not organizer
5. Test 400 when trying to remove organizer
6. Test 400 when draw already completed
7. Test transaction rollback on error
8. Test exclusion rule cleanup

### Step 8: Add Integration Tests
1. Create test with TestContainers for PostgreSQL
2. Test full request/response cycle with authentication
3. Test database state after successful removal
4. Test exclusion rules are properly cleaned up
5. Test concurrent removal attempts
6. Verify 204 No Content response

### Step 9: Update API Documentation
1. Update Swagger/OpenAPI annotations
2. Add XML documentation comments
3. Include example requests/responses
4. Document all error scenarios

### Step 10: Manual Testing Checklist
1. Test with valid organizer removing valid participant
2. Test with non-organizer attempting removal (should fail 403)
3. Test removing organizer themselves (should fail 400)
4. Test removal after draw completion (should fail 400)
5. Test with invalid group ID (should fail 404)
6. Test with invalid user ID (should fail 404)
7. Test without authentication token (should fail 401)
8. Verify exclusion rules are cleaned up in database

---

**Implementation Priority:** High - Core group management feature

**Estimated Effort:** 4-6 hours (including tests and documentation)

**Dependencies:**
- ApplicationDbContext with Groups, GroupParticipants, ExclusionRules entities
- JWT authentication middleware
- MediatR (if using CQRS pattern)

**Follow-up Tasks:**
- Consider adding undo/restore functionality with soft deletes
- Consider adding organizer notification when participant removed
- Consider adding audit trail for participant removals
