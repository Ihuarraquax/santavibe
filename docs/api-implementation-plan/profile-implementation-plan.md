# API Endpoint Implementation Plan: Profile Management

## 1. Endpoint Overview

The Profile Management endpoints allow authenticated users to retrieve and update their own profile information. These endpoints provide a self-service mechanism for users to view and modify their first name and last name, while protecting immutable fields like email address.

**Endpoints:**
- `GET /api/profile` - Retrieve current user's profile
- `PUT /api/profile` - Update current user's profile

**Key Features:**
- JWT-based authentication required for both endpoints
- Users can only access/modify their own profile (enforced via JWT claims)
- Email address is read-only and cannot be changed
- Timestamps (createdAt, lastLoginAt) are system-managed

## 2. Request Details

### GET /api/profile

- **HTTP Method:** GET
- **URL Structure:** `/api/profile`
- **Authentication:** Required (JWT Bearer token)
- **Parameters:**
  - Required: None (user ID extracted from JWT token)
  - Optional: None
- **Request Body:** None

### PUT /api/profile

- **HTTP Method:** PUT
- **URL Structure:** `/api/profile`
- **Authentication:** Required (JWT Bearer token)
- **Parameters:**
  - Required: None (user ID extracted from JWT token)
  - Optional: None
- **Request Body:**
  ```json
  {
    "firstName": "string",
    "lastName": "string"
  }
  ```
  **Validation Rules:**
  - `firstName`: Required, max 100 characters, NOT NULL
  - `lastName`: Required, max 100 characters, NOT NULL

## 3. Used Types

### 3.1 GET /api/profile Types

**GetProfileResponse.cs** (Features/Profile/GetProfile/)
```csharp
public class GetProfileResponse
{
    public required string UserId { get; set; }
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
}
```

**GetProfileQuery.cs** (Features/Profile/GetProfile/)
```csharp
public record GetProfileQuery(Guid UserId);
```

### 3.2 PUT /api/profile Types

**UpdateProfileRequest.cs** (Features/Profile/UpdateProfile/)
```csharp
public class UpdateProfileRequest
{
    [Required(ErrorMessage = "First name is required")]
    [MaxLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
    public required string FirstName { get; set; }

    [Required(ErrorMessage = "Last name is required")]
    [MaxLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
    public required string LastName { get; set; }
}
```

**UpdateProfileCommand.cs** (Features/Profile/UpdateProfile/)
```csharp
public record UpdateProfileCommand(
    Guid UserId,
    string FirstName,
    string LastName
);
```

**UpdateProfileResponse.cs** (Features/Profile/UpdateProfile/)
```csharp
// Same structure as GetProfileResponse
public class UpdateProfileResponse
{
    public required string UserId { get; set; }
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
}
```

### 3.3 Service Interface

**IProfileService.cs** (Features/Profile/)
```csharp
public interface IProfileService
{
    Task<Result<GetProfileResponse>> GetProfileAsync(Guid userId);
    Task<Result<UpdateProfileResponse>> UpdateProfileAsync(UpdateProfileCommand command);
}
```

## 4. Response Details

### GET /api/profile

**Success Response (200 OK):**
```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "email": "user@example.com",
  "firstName": "Jan",
  "lastName": "Kowalski",
  "createdAt": "2025-10-01T10:00:00Z",
  "lastLoginAt": "2025-10-15T08:30:00Z"
}
```

**Error Responses:**
- `401 Unauthorized`: Missing or invalid JWT token (handled by authentication middleware)

### PUT /api/profile

**Success Response (200 OK):**
```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "email": "user@example.com",
  "firstName": "Jan",
  "lastName": "Nowak",
  "createdAt": "2025-10-01T10:00:00Z",
  "lastLoginAt": "2025-10-15T08:30:00Z"
}
```

**Error Responses:**
- `400 Bad Request`: Validation failed
  ```json
  {
    "error": "ValidationError",
    "message": "One or more validation errors occurred",
    "details": {
      "firstName": ["First name is required"],
      "lastName": ["Last name cannot exceed 100 characters"]
    }
  }
  ```
- `401 Unauthorized`: Missing or invalid JWT token
- `404 Not Found`: User not found (edge case)
  ```json
  {
    "error": "UserNotFound",
    "message": "User profile not found"
  }
  ```
- `500 Internal Server Error`: Database update failure

## 5. Data Flow

### GET /api/profile Flow

1. **Authentication Middleware** validates JWT token and populates HttpContext.User
2. **Endpoint Handler** (GetProfileEndpoint.cs):
   - Uses IUserAccessor to extract user ID from JWT claims
   - Creates GetProfileQuery with user ID
   - Calls IProfileService.GetProfileAsync()
3. **ProfileService**:
   - Uses UserManager<ApplicationUser> to find user by ID
   - Checks if user exists and is not soft-deleted
   - Maps ApplicationUser entity to GetProfileResponse DTO
   - Returns Result<GetProfileResponse>
4. **Endpoint Handler**:
   - If success: Returns 200 OK with response
   - If failure: Returns appropriate error status code

### PUT /api/profile Flow

1. **Authentication Middleware** validates JWT token
2. **Endpoint Handler** (UpdateProfileEndpoint.cs):
   - Validates request body is not null
   - Validates UpdateProfileRequest using DataAnnotations
   - Uses IUserAccessor to extract user ID from JWT claims
   - Creates UpdateProfileCommand with user ID and updated fields
   - Calls IProfileService.UpdateProfileAsync()
3. **ProfileService**:
   - Uses UserManager<ApplicationUser> to find user by ID
   - Checks if user exists and is not soft-deleted
   - Updates user.FirstName and user.LastName
   - Calls UserManager.UpdateAsync() to persist changes
   - Maps updated ApplicationUser to UpdateProfileResponse
   - Returns Result<UpdateProfileResponse>
4. **Endpoint Handler**:
   - If success: Returns 200 OK with updated profile
   - If validation failure: Returns 400 Bad Request
   - If user not found: Returns 404 Not Found
   - If database error: Returns 500 Internal Server Error

**Database Interaction:**
- Uses ASP.NET Core Identity's UserManager<ApplicationUser>
- UserManager handles all database operations via EF Core
- No direct DbContext access needed

## 6. Security Considerations

### Authentication
- Both endpoints require JWT Bearer token authentication
- Authentication is enforced via `.RequireAuthorization()` on endpoint registration
- UserAccessor extracts user ID from `ClaimTypes.NameIdentifier` claim in JWT
- If user ID claim is missing or invalid, UserAccessor throws InvalidOperationException

### Authorization
- Users can only access their own profile (no admin override in MVP)
- User ID is always extracted from JWT token, never from request body or query parameters
- This prevents users from accessing or modifying other users' profiles

### Data Validation
- All input validated server-side using DataAnnotations
- MaxLength constraints match database schema (100 characters)
- Email cannot be changed (not included in UpdateProfileRequest)
- Soft-deleted users cannot access their profile

### Immutable Fields
- Email address is read-only (cannot be changed via this endpoint)
- CreatedAt timestamp is set once and never modified
- LastLoginAt is only updated during login, not during profile updates
- UserId is immutable (identity field)

## 7. Error Handling

### Error Scenarios and Status Codes

| Scenario | Status Code | Error Code | When It Occurs |
|----------|-------------|------------|----------------|
| Missing/invalid JWT token | 401 Unauthorized | (Middleware handles) | Token expired, malformed, or missing |
| Null request body (PUT) | 400 Bad Request | ValidationError | Request body is null or malformed JSON |
| Validation failure (PUT) | 400 Bad Request | ValidationError | firstName/lastName empty or > 100 chars |
| User not found | 404 Not Found | UserNotFound | JWT contains valid user ID but user deleted from DB |
| Soft-deleted user | 404 Not Found | UserNotFound | User has IsDeleted = true |
| Database update failure | 500 Internal Server Error | InternalError | EF Core update fails |

### Error Response Format

All errors follow the standard `ErrorResponse` model:
```json
{
  "error": "ErrorCode",
  "message": "User-friendly error message",
  "details": {
    "fieldName": ["Error message 1", "Error message 2"]
  }
}
```

### Logging Strategy

**GET /api/profile:**
- Log info: Successful profile retrieval
- Log warning: User not found (edge case)
- Log error: Database query failure

**PUT /api/profile:**
- Log info: Successful profile update with user ID
- Log warning: Validation errors with details
- Log warning: User not found or soft-deleted
- Log error: Database update failure with exception details

## 8. Performance Considerations

### Potential Bottlenecks
- UserManager.FindByIdAsync() performs database query on each request
- No caching implemented in MVP (acceptable for profile endpoints)

### Optimization Strategies (Future Enhancements)
- Implement distributed caching (Redis) for user profiles
- Cache invalidation on profile update
- Use AsNoTracking() for GET requests (read-only query)

### Current Implementation
- Simple, synchronous database queries via UserManager
- EF Core handles connection pooling and query optimization
- Minimal API ensures low overhead per request

## 9. Implementation Steps

### Step 1: Create Directory Structure
```
Features/
└── Profile/
    ├── IProfileService.cs
    ├── ProfileService.cs
    ├── GetProfile/
    │   ├── GetProfileEndpoint.cs
    │   ├── GetProfileQuery.cs
    │   └── GetProfileResponse.cs
    └── UpdateProfile/
        ├── UpdateProfileEndpoint.cs
        ├── UpdateProfileRequest.cs
        ├── UpdateProfileCommand.cs
        └── UpdateProfileResponse.cs
```

### Step 2: Implement GetProfile Feature

**2.1. Create GetProfileResponse.cs**
- Define DTO with all required fields: userId, email, firstName, lastName, createdAt, lastLoginAt
- Use `required` modifier for non-nullable properties
- Use `DateTimeOffset?` for nullable lastLoginAt

**2.2. Create GetProfileQuery.cs**
- Define record with single parameter: `Guid UserId`

**2.3. Implement ProfileService.GetProfileAsync()**
- Inject UserManager<ApplicationUser> and ILogger<ProfileService>
- Use UserManager.FindByIdAsync(userId.ToString()) to fetch user
- Check if user is null or IsDeleted = true
- Return Result.Failure("UserNotFound", "User profile not found") if not found
- Map ApplicationUser to GetProfileResponse
- Return Result.Success(response)

**2.4. Create GetProfileEndpoint.cs**
- Create static MapGetProfileEndpoint(IEndpointRouteBuilder endpoints) method
- Map GET /api/profile with .RequireAuthorization()
- Configure OpenAPI metadata (summary, description, tags)
- Define Produces<> for 200 OK and error responses
- Implement HandleGetProfile(IUserAccessor userAccessor, IProfileService profileService, ILogger logger)
- Extract user ID using userAccessor.GetCurrentUserId()
- Call profileService.GetProfileAsync(userId)
- Handle Result<GetProfileResponse> and return appropriate IResult

### Step 3: Implement UpdateProfile Feature

**3.1. Create UpdateProfileRequest.cs**
- Define class with firstName and lastName properties
- Add [Required] and [MaxLength(100)] validation attributes
- Use `required` modifier on both properties

**3.2. Create UpdateProfileCommand.cs**
- Define record with parameters: Guid UserId, string FirstName, string LastName

**3.3. Create UpdateProfileResponse.cs**
- Same structure as GetProfileResponse (or reuse the same class)

**3.4. Implement ProfileService.UpdateProfileAsync()**
- Use UserManager.FindByIdAsync() to fetch user
- Check if user exists and is not soft-deleted
- Update user.FirstName and user.LastName with command values
- Call UserManager.UpdateAsync(user)
- Check if update succeeded (IdentityResult.Succeeded)
- If failed, log errors and return Result.Failure("InternalError", "Failed to update profile")
- Map updated ApplicationUser to UpdateProfileResponse
- Return Result.Success(response)

**3.5. Create UpdateProfileEndpoint.cs**
- Create static MapUpdateProfileEndpoint(IEndpointRouteBuilder endpoints) method
- Map PUT /api/profile with .RequireAuthorization()
- Configure OpenAPI metadata
- Define Produces<> for 200 OK, 400 Bad Request, 401, 404, 500 responses
- Implement HandleUpdateProfile([FromBody] request, IUserAccessor, IProfileService, ILogger)
- Validate request body is not null
- Manually validate request using Validator.TryValidateObject()
- If validation fails, return 400 Bad Request with ErrorResponse
- Extract user ID using userAccessor.GetCurrentUserId()
- Create UpdateProfileCommand(userId, request.FirstName, request.LastName)
- Call profileService.UpdateProfileAsync(command)
- Map Result<UpdateProfileResponse> to appropriate HTTP response

### Step 4: Implement IProfileService Interface and Service

**4.1. Create IProfileService.cs**
```csharp
public interface IProfileService
{
    Task<Result<GetProfileResponse>> GetProfileAsync(Guid userId);
    Task<Result<UpdateProfileResponse>> UpdateProfileAsync(UpdateProfileCommand command);
}
```

**4.2. Implement ProfileService.cs**
- Use primary constructor with dependencies: UserManager<ApplicationUser>, ILogger<ProfileService>
- Implement both interface methods as described in Steps 2.3 and 3.4

### Step 5: Register Services in Program.cs

Add to the service registration section:
```csharp
builder.Services.AddScoped<IProfileService, ProfileService>();
```

### Step 6: Register Endpoints in Program.cs

Add to the endpoint mapping section (after authentication endpoints):
```csharp
app.MapGetProfileEndpoint();
app.MapUpdateProfileEndpoint();
```

### Step 7: Testing

**7.1. Unit Tests**
- Test ProfileService.GetProfileAsync() with valid user ID
- Test ProfileService.GetProfileAsync() with non-existent user ID
- Test ProfileService.GetProfileAsync() with soft-deleted user
- Test ProfileService.UpdateProfileAsync() with valid data
- Test ProfileService.UpdateProfileAsync() with validation errors
- Test ProfileService.UpdateProfileAsync() when UserManager.UpdateAsync() fails

**7.2. Integration Tests**
- Test GET /api/profile with valid JWT token (200 OK)
- Test GET /api/profile without JWT token (401 Unauthorized)
- Test PUT /api/profile with valid data (200 OK)
- Test PUT /api/profile with invalid data (400 Bad Request)
- Test PUT /api/profile without JWT token (401 Unauthorized)
- Verify email cannot be changed in response

**7.3. Manual Testing with Swagger**
- Use Swagger UI to test both endpoints
- Verify authentication requirement
- Test validation error scenarios
- Verify response format matches specification

### Step 8: Documentation

- Update Swagger/OpenAPI documentation with endpoint descriptions
- Add XML comments to all public types and methods
- Ensure response examples match API specification

## 10. Dependencies

### Required NuGet Packages (Already Installed)
- Microsoft.AspNetCore.Identity.EntityFrameworkCore (for UserManager)
- Microsoft.EntityFrameworkCore (for database operations)
- System.ComponentModel.DataAnnotations (for validation)

### Required Services (Already Registered)
- IUserAccessor (to extract user ID from JWT)
- UserManager<ApplicationUser> (ASP.NET Core Identity)
- IHttpContextAccessor (for authentication context)

### New Services to Register
- IProfileService and ProfileService

## 11. Compliance with Implementation Rules

### Clean Architecture
- Domain logic isolated in ProfileService
- Endpoints only handle HTTP concerns (validation, status codes)
- DTOs separate from entity models (ApplicationUser vs GetProfileResponse)

### Vertical Slice Architecture
- Features/Profile/GetProfile - complete feature slice
- Features/Profile/UpdateProfile - complete feature slice
- Each slice contains endpoint, request/response, and query/command

### ASP.NET Core Best Practices
- Use minimal APIs (not controllers)
- Use primary constructors for services
- Proper dependency injection lifetimes (Scoped for ProfileService)
- Manual validation in endpoints (minimal APIs don't auto-validate)

### Entity Framework Best Practices
- Use UserManager abstraction (not direct DbContext access)
- UserManager handles change tracking and persistence
- Appropriate async/await patterns throughout

### DDD Principles
- ApplicationUser is the domain entity
- ProfileService contains business logic
- Rich validation on input DTOs
- Proper error handling with Result pattern
