# API Endpoint Implementation Plan: Login

## 1. Endpoint Overview

The login endpoint authenticates users with email and password credentials, returning a JWT token for subsequent authenticated requests. This is a public endpoint that doesn't require authentication but is protected by rate limiting to prevent brute force attacks.

**Purpose**: Authenticate existing users and provide JWT access token
**HTTP Method**: POST
**URL**: `/api/auth/login`
**Authentication Required**: No (public endpoint)

## 2. Request Details

### HTTP Method
POST

### URL Structure
```
POST /api/auth/login
```

### Headers
- `Content-Type: application/json`

### Request Body

```json
{
  "email": "user@example.com",
  "password": "SecureP@ssw0rd"
}
```

### Parameters

**Required:**
- `email` (string): User's email address
  - Validation: Required, valid email format, max 256 characters
  - Example: `"jan.kowalski@example.com"`

- `password` (string): User's password
  - Validation: Required
  - Note: No complexity validation needed for login (only verification)

**Optional:**
- None

## 3. Used Types

### Request Types

**LoginRequest.cs** (new file: `Features/Authentication/Login/LoginRequest.cs`)
```csharp
using System.ComponentModel.DataAnnotations;

namespace SantaVibe.Api.Features.Authentication.Login;

/// <summary>
/// Request model for user login
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// User's email address (used for login)
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [MaxLength(256, ErrorMessage = "Email cannot exceed 256 characters")]
    public required string Email { get; set; }

    /// <summary>
    /// User's password
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    public required string Password { get; set; }
}
```

### Response Types

**LoginResponse.cs** (new file: `Features/Authentication/Login/LoginResponse.cs`)
```csharp
namespace SantaVibe.Api.Features.Authentication.Login;

/// <summary>
/// Response model for successful user login
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// Unique identifier for the user
    /// </summary>
    public required string UserId { get; set; }

    /// <summary>
    /// User's email address
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// User's first name
    /// </summary>
    public required string FirstName { get; set; }

    /// <summary>
    /// User's last name
    /// </summary>
    public required string LastName { get; set; }

    /// <summary>
    /// JWT access token for authentication
    /// </summary>
    public required string Token { get; set; }

    /// <summary>
    /// Token expiration timestamp
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }
}
```

### Service Types

**ILoginService** and **LoginService** (new file: `Features/Authentication/Login/LoginService.cs`)
```csharp
using SantaVibe.Api.Common;

namespace SantaVibe.Api.Features.Authentication.Login;

public interface ILoginService
{
    Task<Result<LoginResponse>> LoginUserAsync(LoginRequest request);
}
```

### Reused Types

From existing codebase:
- `Result<T>` ([Common/Result.cs](SantaVibe.Api/Common/Result.cs)) - Service layer result wrapper
- `ErrorResponse` ([Common/ErrorResponse.cs](SantaVibe.Api/Common/ErrorResponse.cs)) - Standard error response
- `ApplicationUser` ([Data/Entities/ApplicationUser.cs](SantaVibe.Api/Data/Entities/ApplicationUser.cs:9)) - User entity

## 4. Response Details

### Success Response (200 OK)

```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "email": "user@example.com",
  "firstName": "Jan",
  "lastName": "Kowalski",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2025-10-23T14:30:00Z"
}
```

**Status Code**: 200 OK (not 201 Created, since login doesn't create a resource)

### Error Responses

#### 400 Bad Request - Validation Error
```json
{
  "error": "ValidationError",
  "message": "One or more validation errors occurred",
  "details": {
    "Email": ["Email is required"],
    "Password": ["Password is required"]
  }
}
```

#### 401 Unauthorized - Invalid Credentials
```json
{
  "error": "InvalidCredentials",
  "message": "Invalid email or password"
}
```

**Note**: Use generic error message to prevent user enumeration. Don't reveal whether email exists or password is wrong.

#### 401 Unauthorized - Soft Deleted User
```json
{
  "error": "InvalidCredentials",
  "message": "Invalid email or password"
}
```

**Note**: Treat soft-deleted users the same as invalid credentials to prevent enumeration.

#### 429 Too Many Requests - Rate Limit Exceeded
```json
{
  "error": "RateLimitExceeded",
  "message": "Too many login attempts. Please try again later."
}
```

**Note**: Handled automatically by rate limiting middleware.

#### 500 Internal Server Error - Server Error
```json
{
  "error": "InternalError",
  "message": "An unexpected error occurred"
}
```

## 5. Data Flow

### Request Flow

1. **HTTP Request** → Minimal API endpoint handler
2. **Null Check** → Validate request body exists
3. **Model Validation** → Validate using DataAnnotations
4. **Service Call** → `ILoginService.LoginUserAsync(request)`
5. **HTTP Response** → Map service result to HTTP response

### Service Layer Flow

1. **Find User** → Query `UserManager.FindByEmailAsync(email)`
2. **Null Check** → Return failure if user not found
3. **Soft Delete Check** → Verify `user.IsDeleted == false`
4. **Password Verification** → `SignInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: false)`
5. **Update Last Login** → Set `user.LastLoginAt = DateTimeOffset.UtcNow`
6. **Save Changes** → `UserManager.UpdateAsync(user)`
7. **Generate JWT** → Create token with user claims
8. **Return Success** → Return `Result<LoginResponse>` with user data and token

### Database Interactions

**Read Operations:**
- Query `AspNetUsers` table by email (via `UserManager.FindByEmailAsync`)
- Includes standard Identity fields plus custom fields (FirstName, LastName, IsDeleted, LastLoginAt)

**Write Operations:**
- Update `LastLoginAt` timestamp on successful login (via `UserManager.UpdateAsync`)

### External Service Interactions

**ASP.NET Core Identity:**
- `UserManager<ApplicationUser>`: User retrieval and updates
- `SignInManager<ApplicationUser>`: Password verification

**JWT Token Generation:**
- Uses configuration from `appsettings.json` (`Jwt:Secret`, `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpirationInDays`)
- Generates token with claims: sub, email, given_name, family_name, jti, iat
- Token lifetime: 7 days (configurable via `Jwt:ExpirationInDays`)

## 6. Security Considerations

### Authentication & Authorization

- **No authentication required** - Public endpoint
- **Rate limiting required** - Prevent brute force attacks

### Rate Limiting Policy

Create a new rate limiting policy named `"login"`:
- **Window**: 15 minutes (fixed window)
- **Permit Limit**: 5 attempts per window
- **Partition Key**: Remote IP address
- **Queue**: Disabled (reject immediately when limit exceeded)
- **Status Code**: 429 Too Many Requests

**Configuration** (in [Program.cs](SantaVibe.Api/Program.cs:99)):
```csharp
options.AddPolicy("login", context =>
    System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(15),
            QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        }));
```

### Input Validation

**Request Body Validation:**
- Check for null request body (malformed JSON)
- Validate email format and length
- Validate password presence (not empty)
- Use DataAnnotations for declarative validation

**Business Logic Validation:**
- Verify user exists in database
- Check soft delete flag (`IsDeleted == false`)
- Verify password using `SignInManager` (constant-time comparison)

### Security Threats & Mitigations

| Threat | Mitigation |
|--------|------------|
| Brute force attacks | Rate limiting (5 attempts per 15 minutes) |
| User enumeration | Generic error messages (don't reveal if email exists) |
| Timing attacks | Use `SignInManager.CheckPasswordSignInAsync` (constant-time) |
| Credential stuffing | Rate limiting by IP address |
| Soft-deleted user access | Check `IsDeleted` flag before authentication |
| Password exposure in logs | Never log passwords (use structured logging) |
| Token theft | Use HTTPS only, short token expiration (7 days) |
| Account lockout bypass | Consider enabling Identity lockout (future enhancement) |

### Soft Delete Handling

**Critical**: Check `IsDeleted` flag **before** password verification to prevent soft-deleted users from logging in while maintaining generic error messages.

### Password Security

- **Storage**: Passwords hashed using PBKDF2 (ASP.NET Core Identity default)
- **Verification**: Use `SignInManager.CheckPasswordSignInAsync` for constant-time comparison
- **Logging**: Never log passwords or password hashes
- **Transmission**: Only accept over HTTPS

### JWT Token Security

- **Signing Algorithm**: HS256 (HMAC-SHA256)
- **Secret Key**: Stored in configuration (minimum 256 bits recommended)
- **Claims**: Include user ID, email, first name, last name, jti, iat
- **Expiration**: 7 days (configurable)
- **Validation**: Validate issuer, audience, lifetime, and signature
- **Clock Skew**: Zero tolerance (`ClockSkew = TimeSpan.Zero`)

### Logging for Security

Log the following events for security monitoring:
- Failed login attempts (with email, without password)
- Successful login attempts (with user ID and email)
- Soft-deleted user login attempts
- Rate limit violations (handled by middleware)
- Unexpected errors during authentication

**Use Serilog structured logging** - already configured in [Program.cs](SantaVibe.Api/Program.cs:14-20)

## 7. Error Handling

### Error Scenarios

| Scenario | Error Code | Status Code | Message | Logging Level |
|----------|-----------|-------------|---------|---------------|
| Null request body | ValidationError | 400 | "Request body is required" | Warning |
| Invalid email format | ValidationError | 400 | "Invalid email format" | Warning |
| Missing required fields | ValidationError | 400 | "One or more validation errors occurred" | Warning |
| User not found | InvalidCredentials | 401 | "Invalid email or password" | Warning |
| Wrong password | InvalidCredentials | 401 | "Invalid email or password" | Warning |
| Soft-deleted user | InvalidCredentials | 401 | "Invalid email or password" | Warning |
| Rate limit exceeded | RateLimitExceeded | 429 | "Too many login attempts. Please try again later." | Warning |
| Database error | InternalError | 500 | "An unexpected error occurred" | Error |
| JWT generation error | InternalError | 500 | "An unexpected error occurred" | Error |

### Error Response Format

All errors follow the standard `ErrorResponse` format:
```json
{
  "error": "ErrorCode",
  "message": "Human-readable message",
  "details": {
    "FieldName": ["Error message 1", "Error message 2"]
  }
}
```

The `details` field is optional and only included for validation errors (400 Bad Request).

### Service Layer Error Handling

The `LoginService` returns `Result<LoginResponse>` with:
- `IsSuccess = false`
- `Error`: Error code (e.g., "InvalidCredentials")
- `Message`: Error message
- `ValidationErrors`: Dictionary of field-specific errors (if applicable)

### Endpoint Layer Error Mapping

The endpoint handler maps service errors to HTTP status codes:

```csharp
var statusCode = result.Error switch
{
    "InvalidCredentials" => StatusCodes.Status401Unauthorized,
    "ValidationError" => StatusCodes.Status400BadRequest,
    "InternalError" => StatusCodes.Status500InternalServerError,
    _ => StatusCodes.Status500InternalServerError
};
```

### Global Exception Handling

Use existing global exception handler middleware ([Middleware/GlobalExceptionHandler.cs](SantaVibe.Api/Middleware/GlobalExceptionHandler.cs)) to catch unhandled exceptions and return 500 Internal Server Error.

## 8. Performance Considerations

### Potential Bottlenecks

1. **Database Query** - User lookup by email
2. **Password Verification** - Intentionally slow (PBKDF2 hashing)
3. **Database Update** - LastLoginAt timestamp update
4. **JWT Generation** - Token signing operation

### Optimization Strategies

1. **Database Query Optimization**
   - Email field is indexed (part of ASP.NET Core Identity schema)
   - Use `AsNoTracking()` for read query if not updating in same context
   - **However**: We need to update `LastLoginAt`, so tracking is required

2. **Password Verification**
   - Intentionally slow to prevent brute force (PBKDF2 work factor)
   - Cannot optimize without compromising security
   - Mitigate with rate limiting

3. **Database Update**
   - Single update operation for `LastLoginAt`
   - Consider async/await pattern (already used)
   - **Optional optimization**: Update in background (fire-and-forget) to reduce response time
     - Trade-off: Risk of missing updates if process crashes
     - Recommendation: Keep synchronous for MVP, optimize later if needed

4. **JWT Generation**
   - Relatively fast operation (milliseconds)
   - Could cache configuration values (Secret, Issuer, Audience) as class fields
   - Recommendation: Extract JWT generation to a shared service or helper class to avoid code duplication with RegisterService

5. **Caching**
   - **Don't cache user credentials or authentication results** (security risk)
   - Could cache JWT configuration values (minor optimization)

### Expected Performance

- **Target Response Time**: < 500ms (excluding network latency)
- **Bottleneck**: Password verification (~200-300ms due to PBKDF2)
- **Concurrent Users**: Limited by rate limiting (5 login attempts per 15 minutes per IP)

### Database Connection Pooling

Already configured in [Program.cs](SantaVibe.Api/Program.cs:32-41):
- Uses Npgsql connection pooling (default behavior)
- Retry on failure enabled (3 retries, 5-second delay)

## 9. Implementation Steps

### Step 1: Create Service Layer

**File**: `SantaVibe.Api/Features/Authentication/Login/LoginService.cs`

1. Create `ILoginService` interface with method:
   ```csharp
   Task<Result<LoginResponse>> LoginUserAsync(LoginRequest request)
   ```

2. Implement `LoginService` class with primary constructor:
   ```csharp
   public class LoginService(
       UserManager<ApplicationUser> userManager,
       SignInManager<ApplicationUser> signInManager,
       IConfiguration configuration,
       ILogger<LoginService> logger)
       : ILoginService
   ```

3. Implement `LoginUserAsync` method:
   - Find user by email using `UserManager.FindByEmailAsync`
   - Return failure if user not found (generic error message)
   - Check `IsDeleted` flag, return failure if true (generic error message)
   - Verify password using `SignInManager.CheckPasswordSignInAsync`
   - Return failure if password incorrect (generic error message)
   - Update `LastLoginAt` timestamp using `UserManager.UpdateAsync`
   - Generate JWT token using private helper method
   - Return success with `LoginResponse`

4. Create private `GenerateJwtToken` method:
   - Read JWT configuration (Secret, Issuer, Audience, ExpirationInDays)
   - Create claims array (sub, email, given_name, family_name, jti, iat)
   - Create `JwtSecurityToken` with expiration
   - Return token string using `JwtSecurityTokenHandler`
   - **Note**: Can reuse/adapt from [RegisterService.cs:98-128](SantaVibe.Api/Features/Authentication/Register/RegisterService.cs:98-128)

5. Add structured logging:
   - Log login attempts (include email, exclude password)
   - Log successful logins (include user ID)
   - Log failures (user not found, wrong password, soft deleted)
   - Use `ILogger<LoginService>` with structured logging

### Step 2: Create Request/Response DTOs

**File**: `SantaVibe.Api/Features/Authentication/Login/LoginRequest.cs`

1. Create `LoginRequest` class with properties:
   - `Email` (required, email format, max 256 chars)
   - `Password` (required)

2. Add DataAnnotations validation attributes
3. Add XML documentation comments

**File**: `SantaVibe.Api/Features/Authentication/Login/LoginResponse.cs`

1. Create `LoginResponse` class with properties:
   - `UserId` (string)
   - `Email` (string)
   - `FirstName` (string)
   - `LastName` (string)
   - `Token` (string)
   - `ExpiresAt` (DateTimeOffset)

2. Mark all properties as `required`
3. Add XML documentation comments

### Step 3: Create Minimal API Endpoint

**File**: `SantaVibe.Api/Features/Authentication/Login/LoginEndpoint.cs`

1. Create static class `LoginEndpoint`

2. Create extension method `MapLoginEndpoint(IEndpointRouteBuilder endpoints)`:
   - Map POST endpoint at `/api/auth/login`
   - Set endpoint name: `"Login"`
   - Set tags: `"Authentication"`
   - Configure OpenAPI metadata (summary, description)
   - Apply rate limiting policy: `"login"`
   - Document response types (200, 400, 401, 429, 500)
   - Reference existing pattern in [RegisterEndpoint.cs:15-34](SantaVibe.Api/Features/Authentication/Register/RegisterEndpoint.cs:15-34)

3. Create private handler method `HandleLogin`:
   - Parameters: `LoginRequest`, `ILoginService`, `ILogger`, `HttpContext`
   - Check for null request body → return 400 with error response
   - Validate request using `Validator.TryValidateObject` → return 400 with validation errors
   - Call `loginService.LoginUserAsync(request)`
   - Map service result to HTTP response:
     - Success → return 200 OK with `LoginResponse`
     - InvalidCredentials → return 401 with error response
     - ValidationError → return 400 with error response
     - InternalError → return 500 with error response
   - Reference existing pattern in [RegisterEndpoint.cs:39-121](SantaVibe.Api/Features/Authentication/Register/RegisterEndpoint.cs:39-121)

### Step 4: Register Services in DI Container

**File**: `SantaVibe.Api/Program.cs`

1. Add `SignInManager<ApplicationUser>` to DI container:
   - **Note**: Already included via `.AddIdentity<ApplicationUser, IdentityRole>()` at [line 44](SantaVibe.Api/Program.cs:44)
   - No additional registration needed

2. Register `ILoginService` and `LoginService` (around [line 112](SantaVibe.Api/Program.cs:112)):
   ```csharp
   builder.Services.AddScoped<ILoginService, LoginService>();
   ```

### Step 5: Configure Rate Limiting Policy

**File**: `SantaVibe.Api/Program.cs`

1. Add `"login"` rate limiting policy in `AddRateLimiter` configuration (around [line 99](SantaVibe.Api/Program.cs:99)):
   ```csharp
   options.AddPolicy("login", context =>
       System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
           partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
           factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
           {
               PermitLimit = 5,
               Window = TimeSpan.FromMinutes(15),
               QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
               QueueLimit = 0
           }));
   ```

### Step 6: Map Endpoint in Application

**File**: `SantaVibe.Api/Program.cs`

1. Add endpoint mapping after [line 180](SantaVibe.Api/Program.cs:180):
   ```csharp
   app.MapLoginEndpoint();
   ```

### Step 7: Write Unit Tests

**File**: `SantaVibe.Tests/Features/Authentication/Login/LoginServiceUnitTests.cs`

Test scenarios:
1. **Successful login** - Returns success with token and user data
2. **User not found** - Returns InvalidCredentials error
3. **Wrong password** - Returns InvalidCredentials error
4. **Soft deleted user** - Returns InvalidCredentials error
5. **LastLoginAt updated** - Verifies timestamp is set
6. **JWT token contains correct claims** - Verifies token payload

Use NSubstitute for mocking `UserManager`, `SignInManager`, `IConfiguration`, `ILogger`.

### Step 8: Write Integration Tests

**File**: `SantaVibe.Tests/Features/Authentication/Login/LoginEndpointIntegrationTests.cs`

Test scenarios:
1. **Successful login** - 200 OK with correct response structure
2. **Invalid email format** - 400 Bad Request with validation error
3. **Missing required fields** - 400 Bad Request with validation errors
4. **Invalid credentials** - 401 Unauthorized
5. **Soft deleted user** - 401 Unauthorized
6. **Rate limiting** - 429 Too Many Requests after 5 attempts
7. **Null request body** - 400 Bad Request

Use TestContainers for database, WebApplicationFactory for integration testing.

Reference existing test structure in [RegisterEndpointIntegrationTests.cs](SantaVibe.Tests/Features/Authentication/Register/RegisterEndpointIntegrationTests.cs).

### Step 9: Test Manually

1. Start application: `dotnet run --project SantaVibe.Api`
2. Open Swagger UI: `https://localhost:5001/swagger`
3. Test login endpoint with various scenarios:
   - Valid credentials
   - Invalid credentials
   - Validation errors
   - Rate limiting (make 6+ requests rapidly)
4. Verify JWT token structure using jwt.io
5. Test token authentication with protected endpoints

### Step 10: Update Documentation

1. Verify Swagger documentation is auto-generated correctly
2. Ensure OpenAPI spec includes all response types
3. Add endpoint to API documentation if separate docs exist
4. Update CLAUDE.md if new patterns are introduced

### Step 11: Optional Enhancements (Future)

Consider for future iterations:
1. **Account lockout** - Enable Identity's lockout feature after X failed attempts
2. **Two-factor authentication** - Add 2FA support
3. **Remember me** - Extend token expiration for trusted devices
4. **Refresh tokens** - Implement token refresh mechanism
5. **Background LastLoginAt update** - Update timestamp asynchronously for faster response
6. **Login history** - Track login attempts in separate table
7. **Geolocation tracking** - Log IP address and location for security

## Summary

This implementation plan provides a comprehensive guide for implementing the login endpoint following the existing architecture patterns in the SantaVibe application. The endpoint:

- Uses **minimal APIs** (not controllers)
- Follows **vertical slice architecture** (all login-related code in `Features/Authentication/Login/`)
- Uses **ASP.NET Core Identity** for authentication
- Implements **JWT token generation** consistent with registration
- Applies **rate limiting** to prevent brute force attacks
- Uses **generic error messages** to prevent user enumeration
- Checks **soft delete flag** to prevent deleted users from logging in
- Updates **LastLoginAt timestamp** for security auditing
- Follows **clean architecture** principles with service layer abstraction
- Uses **Result<T> pattern** for service layer responses
- Implements comprehensive **input validation** and **error handling**
- Includes **structured logging** with Serilog for security monitoring
- Follows **C# 13 conventions** (primary constructors, required properties)
- Maintains consistency with existing [RegisterEndpoint](SantaVibe.Api/Features/Authentication/Register/RegisterEndpoint.cs) implementation

The implementation can be completed incrementally, testing each layer independently before integration.
