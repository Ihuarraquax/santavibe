# API Endpoint Implementation Plan: Get User's Groups

## 1. Endpoint Overview
This document outlines the implementation plan for the `GET /api/groups` endpoint. Its purpose is to retrieve a list of all Secret Santa groups in which the currently authenticated user is a participant. The endpoint will provide key details about each group, allowing the user to see their group memberships at a glance.

## 2. Request Details
- **HTTP Method**: `GET`
- **URL Structure**: `/api/groups`
- **Parameters**:
  - **Required**: None.
  - **Optional**:
    - `includeCompleted` (boolean): If set to `false`, the response will exclude groups where the draw has already been completed. Defaults to `true`.
- **Request Body**: None.

## 3. Used Types
The following Data Transfer Objects (DTOs) will be created to structure the response:

```csharp
// The main response object
public class GetUserGroupsResponse
{
    public List<GroupDto> Groups { get; set; }
    public int TotalCount { get; set; }
}

// Represents a single group in the list
public class GroupDto
{
    public Guid GroupId { get; set; }
    public required string Name { get; set; }
    public required string OrganizerId { get; set; }
    public required string OrganizerName { get; set; }
    public bool IsOrganizer { get; set; }
    public int ParticipantCount { get; set; }
    public decimal? Budget { get; set; }
    public bool DrawCompleted { get; set; }
    public DateTimeOffset JoinedAt { get; set; }
    public DateTimeOffset? DrawCompletedAt { get; set; }
}
```

## 4. Response Details
- **Success (200 OK)**: Returns a `GetUserGroupsResponse` object with a list of groups the user has joined.
  ```json
  {
    "groups": [
      {
        "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
        "name": "Family Secret Santa 2025",
        "organizerId": "550e8400-e29b-41d4-a716-446655440000",
        "organizerName": "Jan Kowalski",
        "isOrganizer": true,
        "participantCount": 5,
        "budget": 100.00,
        "drawCompleted": false,
        "joinedAt": "2025-10-15T10:00:00Z",
        "drawCompletedAt": null
      }
    ],
    "totalCount": 1
  }
  ```
- **Error (401 Unauthorized)**: Returned if the JWT Bearer token is missing, invalid, or expired.

## 5. Data Flow
1.  A `GET` request is made to `/api/groups`.
2.  The ASP.NET Core authentication middleware validates the JWT token and extracts the user's identity and claims (specifically the user ID).
3.  The request is routed to a minimal API endpoint handler.
4.  The handler builds a MediatR query (`GetUserGroupsQuery`) with the `includeCompleted` parameter.
5.  The `GetUserGroupsQueryHandler` executes the business logic.
6.  The handler queries the database using Entity Framework Core:
    - It starts with the `GroupParticipants` table, filtering for records matching the authenticated user's ID.
    - It joins with the `Groups` table to get group details.
    - It joins with the `ApplicationUser` table (on `Group.OrganizerUserId`) to retrieve the organizer's name.
    - It applies the `includeCompleted` filter by checking if `Group.DrawCompletedAt` is `null`.
    - The results are projected into the `GroupDto` format. This projection will calculate `ParticipantCount` and `IsOrganizer`.
7.  The resulting `GetUserGroupsResponse` object is serialized to JSON and returned to the client with a `200 OK` status code.

## 6. Security Considerations
- **Authentication**: The endpoint must be protected with the `[Authorize]` attribute to ensure only authenticated users can access it.
- **Authorization**: The core query **must** be filtered by the authenticated user's ID. This prevents any user from seeing groups they are not a part of. The query should originate from the `GroupParticipant` entity to ensure data is scoped correctly.
- **Data Projection**: DTOs (`GroupDto`) must be used to ensure only the specified fields are returned, preventing accidental leakage of sensitive entity properties like the group's `InvitationToken`.

## 7. Performance Considerations
- **Read-Only Query**: The database query is for reading data only. `AsNoTracking()` should be used with the EF Core query to improve performance by avoiding change tracking overhead.
- **Efficient Querying**: The data should be fetched in a single, efficient database query. Use LINQ projection (`Select`) to let EF Core generate a query that retrieves only the necessary columns and calculates the participant count on the database side, avoiding N+1 query problems.

## 8. Implementation Steps
1.  **Create Feature Slice**: In `SantaVibe.Api`, create a new feature folder: `/Features/Groups/GetUserGroups/`.
2.  **Define DTOs**: Inside the new folder, create `GetUserGroupsResponse.cs` containing the `GetUserGroupsResponse` and `GroupDto` record definitions.
3.  **Create MediatR Query**: Create `GetUserGroupsQuery.cs` to represent the request.
    ```csharp
    public record GetUserGroupsQuery(bool IncludeCompleted) : IRequest<Result<GetUserGroupsResponse>>;
    ```
4.  **Implement Query Handler**: Create `GetUserGroupsQueryHandler.cs`.
    - Inject `ApplicationDbContext` and an `IUserAccessor` service (a custom service to retrieve the current user's ID from `IHttpContextAccessor`).
    - Implement the `Handle` method.
    - Retrieve the current `userId`.
    - Construct the LINQ query against `_context.GroupParticipants`.
    - Use `Where()` to filter by `userId`.
    - If `IncludeCompleted` is false, add another `Where(p => p.Group.DrawCompletedAt == null)`.
    - Use `Select()` to project the data into `GroupDto`. The projection should include:
        - `ParticipantCount = p.Group.GroupParticipants.Count()`
        - `OrganizerName = p.Group.Organizer.FirstName + " " + p.Group.Organizer.LastName`
        - `IsOrganizer = p.Group.OrganizerUserId == userId`
    - Use `AsNoTracking()` on the query.
    - Execute the query with `ToListAsync()` and populate the `GetUserGroupsResponse`.
5.  **Register Endpoint**: In `Program.cs` (or a relevant `IEndpointRouteBuilder` extension), map the endpoint.
    ```csharp
    app.MapGet("/api/groups", async ([AsParameters] GetUserGroupsRequest request, ISender sender) =>
    {
        var query = new GetUserGroupsQuery(request.IncludeCompleted);
        var result = await sender.Send(query);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    })
    .RequireAuthorization()
    .WithTags("Groups");
    ```
    *(Note: A `GetUserGroupsRequest` record will be needed to bind query parameters)*
6.  **Create User Accessor**: Implement a simple `IUserAccessor` interface and `UserAccessor` class to abstract retrieving the user ID from `HttpContext`. This follows Clean Architecture principles.
7.  **Add Unit Tests**: Create unit tests for the `GetUserGroupsQueryHandler` to verify the logic for filtering and mapping.
8.  **Add Integration Tests**: Create integration tests to verify the endpoint's behavior, including authentication, query parameter handling, and correct response structure.
