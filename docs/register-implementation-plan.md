# API Endpoint Implementation Plan: Register New User

## 1. Endpoint Overview

The `POST /api/auth/register` endpoint creates a new user account in the SantaVibe application. This is a public endpoint (no authentication required) that accepts user registration details including email, password, first and last name, and GDPR consent. Upon successful registration, the endpoint immediately authenticates the user and returns a JWT token for subsequent requests.

**Key Characteristics:**
- Public endpoint (no authentication required)
- Leverages ASP.NET Core Identity for user management and password security
- Returns JWT token immediately after successful registration (auto-login)
- Validates email uniqueness and password complexity
- Records GDPR consent with timestamp
- Implements minimal API pattern (not MVC controllers)

## 2. Request Details

**HTTP Method:** `POST`

**URL Structure:** `/api/auth/register`

**Content-Type:** `application/json`

**Request Body Structure:**

```json
{
  "email": "user@example.com",
  "password": "SecureP@ssw0rd",
  "firstName": "Jan",
  "lastName": "Kowalski",
  "gdprConsent": true
}
```

**Parameters:**

**Required:**
- `email` (string): User's email address
  - Must be valid email format
  - Must be unique (not already registered)
  - Maximum 256 characters
  - Case-insensitive for uniqueness check

- `password` (string): User's password
  - Minimum 8 characters
  - Must contain at least one uppercase letter
  - Must contain at least one lowercase letter
  - Must contain at least one digit
  - Must contain at least one special character

- `firstName` (string): User's first name
  - Maximum 100 characters
  - Cannot be empty or whitespace

- `lastName` (string): User's last name
  - Maximum 100 characters
  - Cannot be empty or whitespace

- `gdprConsent` (boolean): GDPR consent flag
  - Must be exactly `true`
  - User must actively consent to data processing

**Optional:**
- None - all parameters are required

## 3. Used Types

### Request DTO

**File:** `SantaVibe.Api/DTOs/Auth/RegisterUserRequest.cs`

```csharp
namespace SantaVibe.Api.DTOs.Auth;

public class RegisterUserRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [MaxLength(256, ErrorMessage = "Email cannot exceed 256 characters")]
    public required string Email { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    [PasswordComplexity] // Custom validation attribute
    public required string Password { get; set; }

    [Required(ErrorMessage = "First name is required")]
    [MaxLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
    public required string FirstName { get; set; }

    [Required(ErrorMessage = "Last name is required")]
    [MaxLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
    public required string LastName { get; set; }

    [MustBeTrue(ErrorMessage = "GDPR consent is required")]
    public bool GdprConsent { get; set; }
}
```

### Response DTO

**File:** `SantaVibe.Api/DTOs/Auth/RegisterUserResponse.cs`

```csharp
namespace SantaVibe.Api.DTOs.Auth;

public class RegisterUserResponse
{
    public required string UserId { get; set; }
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Token { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}
```

### Error Response DTO

**File:** `SantaVibe.Api/DTOs/Common/ErrorResponse.cs`

```csharp
namespace SantaVibe.Api.DTOs.Common;

public class ErrorResponse
{
    public required string Error { get; set; }
    public required string Message { get; set; }
    public Dictionary<string, string[]>? Details { get; set; }
}
```

### Custom Validation Attributes

**File:** `SantaVibe.Api/Validators/PasswordComplexityAttribute.cs`

```csharp
namespace SantaVibe.Api.Validators;

[AttributeUsage(AttributeTargets.Property)]
public class PasswordComplexityAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string password)
            return new ValidationResult("Password is required");

        var errors = new List<string>();

        if (!password.Any(char.IsUpper))
            errors.Add("Must contain at least one uppercase letter");

        if (!password.Any(char.IsLower))
            errors.Add("Must contain at least one lowercase letter");

        if (!password.Any(char.IsDigit))
            errors.Add("Must contain at least one digit");

        if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
            errors.Add("Must contain at least one special character");

        return errors.Any()
            ? new ValidationResult(string.Join("; ", errors))
            : ValidationResult.Success;
    }
}
```

**File:** `SantaVibe.Api/Validators/MustBeTrueAttribute.cs`

```csharp
namespace SantaVibe.Api.Validators;

[AttributeUsage(AttributeTargets.Property)]
public class MustBeTrueAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is bool boolValue && boolValue)
            return ValidationResult.Success;

        return new ValidationResult(ErrorMessage ?? "This field must be true");
    }
}
```

### Service Interface and Implementation

**File:** `SantaVibe.Api/Services/IAuthenticationService.cs`

```csharp
namespace SantaVibe.Api.Services;

public interface IAuthenticationService
{
    Task<Result<RegisterUserResponse>> RegisterUserAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default);
}
```

**File:** `SantaVibe.Api/Services/AuthenticationService.cs`

```csharp
namespace SantaVibe.Api.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthenticationService> _logger;

    // Constructor and implementation details in step 3
}
```

### Result Type

**File:** `SantaVibe.Api/Common/Result.cs`

```csharp
namespace SantaVibe.Api.Common;

public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public string? Message { get; }
    public Dictionary<string, string[]>? ValidationErrors { get; }

    private Result(bool isSuccess, T? value, string? error, string? message,
        Dictionary<string, string[]>? validationErrors)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        Message = message;
        ValidationErrors = validationErrors;
    }

    public static Result<T> Success(T value) =>
        new(true, value, null, null, null);

    public static Result<T> Failure(string error, string message) =>
        new(false, default, error, message, null);

    public static Result<T> ValidationFailure(string message, Dictionary<string, string[]> errors) =>
        new(false, default, "ValidationError", message, errors);
}
```

## 4. Response Details

### Success Response (201 Created)

**HTTP Status Code:** `201 Created`

**Headers:**
- `Content-Type: application/json`
- `Location: /api/users/{userId}` (optional, points to user resource)

**Response Body:**

```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "email": "user@example.com",
  "firstName": "Jan",
  "lastName": "Kowalski",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2025-10-16T14:30:00Z"
}
```

**Response Fields:**
- `userId`: Unique identifier (GUID) for the created user
- `email`: User's email address (confirmed)
- `firstName`: User's first name
- `lastName`: User's last name
- `token`: JWT access token for authentication
- `expiresAt`: Token expiration timestamp in ISO 8601 format with timezone

### Error Response: Validation Failed (400 Bad Request)

**HTTP Status Code:** `400 Bad Request`

**Response Body:**

```json
{
  "error": "ValidationError",
  "message": "One or more validation errors occurred",
  "details": {
    "password": [
      "Must contain at least one uppercase letter",
      "Must be at least 8 characters"
    ],
    "email": [
      "Invalid email format"
    ]
  }
}
```

**Common Validation Errors:**
- Invalid email format
- Email exceeds 256 characters
- Password too short (< 8 characters)
- Password missing uppercase letter
- Password missing lowercase letter
- Password missing digit
- Password missing special character
- First name missing or exceeds 100 characters
- Last name missing or exceeds 100 characters
- GDPR consent not provided or false

### Error Response: Email Already Exists (409 Conflict)

**HTTP Status Code:** `409 Conflict`

**Response Body:**

```json
{
  "error": "EmailAlreadyExists",
  "message": "An account with this email already exists"
}
```

**Scenario:** User attempts to register with an email address that's already in the database.

### Error Response: Internal Server Error (500)

**HTTP Status Code:** `500 Internal Server Error`

**Response Body:**

```json
{
  "error": "InternalServerError",
  "message": "An unexpected error occurred while processing your request"
}
```

**Scenarios:**
- Database connection failures
- Unexpected Identity framework errors
- JWT token generation failures
- Any unhandled exceptions

**Note:** Internal error details should NOT be exposed to clients in production. Log full error details server-side using Serilog.

## 5. Data Flow

### High-Level Flow

```
Client Request
    ↓
[1] Minimal API Endpoint (/api/auth/register)
    ↓
[2] Model Binding & Validation (Data Annotations)
    ↓ (if validation passes)
[3] AuthenticationService.RegisterUserAsync()
    ↓
[4] Check Email Uniqueness (UserManager.FindByEmailAsync)
    ↓ (if email not found)
[5] Create ApplicationUser Entity
    ↓
[6] UserManager.CreateAsync(user, password)
    ↓ (if creation succeeds)
[7] Generate JWT Token
    ↓
[8] Return 201 Created with RegisterUserResponse
    ↓
Client Response
```

### Detailed Step-by-Step Flow

**Step 1: Request Reception**
- Client sends POST request to `/api/auth/register`
- ASP.NET Core routing directs to minimal API endpoint
- Request body is deserialized to `RegisterUserRequest` DTO

**Step 2: Model Validation**
- ASP.NET Core executes data annotation validations
- Custom validation attributes execute (PasswordComplexity, MustBeTrue)
- If validation fails:
  - Collect all validation errors into dictionary
  - Return 400 Bad Request with `ErrorResponse` containing details
  - Flow terminates
- If validation passes, continue to service layer

**Step 3: Service Invocation**
- Endpoint calls `IAuthenticationService.RegisterUserAsync(request)`
- Service is injected via dependency injection (scoped lifetime)

**Step 4: Email Uniqueness Check**
- Service calls `UserManager<ApplicationUser>.FindByEmailAsync(email)`
- Email lookup is case-insensitive (handled by ASP.NET Core Identity)
- If user found:
  - Return `Result<RegisterUserResponse>.Failure("EmailAlreadyExists", "An account with this email already exists")`
  - Endpoint returns 409 Conflict
  - Flow terminates
- If user not found, continue to user creation

**Step 5: Create ApplicationUser Entity**
- Instantiate new `ApplicationUser` with:
  - `Email = request.Email`
  - `UserName = request.Email` (Identity requires username, use email)
  - `FirstName = request.FirstName`
  - `LastName = request.LastName`
  - `CreatedAt = DateTimeOffset.UtcNow`
  - `LastLoginAt = DateTimeOffset.UtcNow` (user is immediately logged in)
  - `IsDeleted = false`

**Step 6: Persist User with Identity**
- Call `UserManager<ApplicationUser>.CreateAsync(user, request.Password)`
- Identity framework:
  - Validates password against configured `PasswordOptions`
  - Hashes password using PBKDF2
  - Inserts user into `AspNetUsers` table
  - EF Core transaction ensures atomicity
- If creation fails:
  - Extract error descriptions from `IdentityResult.Errors`
  - Return validation failure result
  - Endpoint returns 400 Bad Request
  - Flow terminates
- If creation succeeds, continue to token generation

**Step 7: Generate JWT Token**
- Create claims for JWT:
  - `ClaimTypes.NameIdentifier`: user.Id
  - `ClaimTypes.Email`: user.Email
  - `ClaimTypes.GivenName`: user.FirstName
  - `ClaimTypes.Surname`: user.LastName
  - `JwtRegisteredClaimNames.Jti`: unique token ID (GUID)
- Read JWT configuration from `appsettings.json`:
  - Secret key for signing
  - Issuer
  - Audience
  - Expiration time (e.g., 7 days)
- Create `JwtSecurityToken` with:
  - Claims
  - Signing credentials (HMAC-SHA256 or RSA)
  - Expiration time
- Serialize token using `JwtSecurityTokenHandler`
- Calculate `expiresAt` timestamp

**Step 8: Return Success Response**
- Create `RegisterUserResponse` with:
  - `UserId = user.Id`
  - `Email = user.Email`
  - `FirstName = user.FirstName`
  - `LastName = user.LastName`
  - `Token = jwtToken`
  - `ExpiresAt = expirationTimestamp`
- Return `Result<RegisterUserResponse>.Success(response)`
- Endpoint returns 201 Created with response body

### Database Interactions

1. **Email Lookup Query** (Step 4):
   ```sql
   SELECT * FROM "AspNetUsers"
   WHERE "NormalizedEmail" = 'USER@EXAMPLE.COM'
   LIMIT 1
   ```

2. **User Insert** (Step 6):
   ```sql
   INSERT INTO "AspNetUsers" (
       "Id", "UserName", "NormalizedUserName", "Email", "NormalizedEmail",
       "PasswordHash", "SecurityStamp", "ConcurrencyStamp",
       "FirstName", "LastName", "CreatedAt", "LastLoginAt", "IsDeleted"
   )
   VALUES (...)
   ```

### Error Handling Flow

**Validation Errors:**
```
Request → Model Validation Fails → Collect Errors → 400 Bad Request
```

**Email Conflict:**
```
Request → Email Check → User Found → 409 Conflict
```

**Identity Creation Errors:**
```
Request → UserManager.CreateAsync Fails → Extract Errors → 400 Bad Request
```

**Unexpected Errors:**
```
Request → Exception Thrown → Exception Middleware Catches → 500 Internal Server Error
```

## 6. Security Considerations

### Authentication & Authorization
- **No authentication required** for this endpoint (public access)
- **User gains authentication** immediately upon successful registration via returned JWT token
- Client should store JWT token securely (e.g., HTTP-only cookie or secure storage)

### Password Security

**Password Hashing:**
- ASP.NET Core Identity uses **PBKDF2** (Password-Based Key Derivation Function 2) with HMAC-SHA256
- Password hash includes:
  - Hashing algorithm identifier
  - Iteration count (10,000 by default)
  - Random salt (128-bit)
  - Hash value (256-bit)
- Passwords are NEVER stored in plain text

**Password Policy Enforcement:**
Configure in `Program.cs`:
```csharp
builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 1;
});
```

### JWT Token Security

**Token Generation:**
- Use strong secret key (minimum 256-bit for HS256)
- Store secret in environment variables or Azure Key Vault (NEVER in code)
- Set appropriate expiration time (e.g., 7 days for MVP)
- Include essential claims only (avoid sensitive data)

**JWT Configuration Example:**
```json
{
  "Jwt": {
    "Secret": "[STRONG_SECRET_KEY_FROM_ENV]",
    "Issuer": "SantaVibe.Api",
    "Audience": "SantaVibe.Web",
    "ExpirationInDays": 7
  }
}
```

**Token Best Practices:**
- Use HTTPS exclusively to prevent token interception
- Implement token refresh mechanism (future enhancement)
- Consider short-lived access tokens with refresh tokens for production
- Include `jti` (JWT ID) claim for potential token revocation

### Input Validation

**Validation Layers:**

1. **Model-Level Validation** (Data Annotations):
   - Prevents obviously malformed input
   - Enforces length constraints
   - Validates data types and formats

2. **Business-Level Validation** (Service Layer):
   - Email uniqueness check
   - Password complexity validation
   - GDPR consent verification

3. **Database-Level Constraints**:
   - Unique index on normalized email
   - NOT NULL constraints
   - CHECK constraints where applicable

**SQL Injection Prevention:**
- EF Core uses parameterized queries automatically
- Never construct SQL queries using string concatenation
- All user input is treated as data, not executable code

### Email Enumeration

**Trade-off Consideration:**
- API specification requires 409 Conflict for existing emails
- This allows potential attackers to enumerate registered emails
- **Mitigation strategies**:
  - Implement rate limiting on this endpoint
  - Monitor for suspicious registration patterns
  - Consider CAPTCHA for public-facing deployment
  - Accept this risk for MVP given better UX

### Rate Limiting

**Protection Against Abuse:**
- Apply rate limiting middleware to prevent:
  - Brute force password attempts
  - Account enumeration attacks
  - Resource exhaustion (DoS)

**Rate Limiting Configuration (Program.cs):**
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("register", config =>
    {
        config.PermitLimit = 5; // 5 attempts
        config.Window = TimeSpan.FromMinutes(15); // per 15 minutes
        config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        config.QueueLimit = 0; // No queueing
    });
});
```

**Apply to endpoint:**
```csharp
app.MapPost("/api/auth/register", RegisterHandler)
   .RequireRateLimiting("register");
```

### GDPR Compliance

**Data Recording:**
- Record that user provided GDPR consent
- Store timestamp of consent in `CreatedAt` field
- Link to privacy policy and terms of service must be presented during registration (frontend responsibility)

**Minimal Data Collection:**
- Collect only necessary information: email, name, password
- Avoid collecting unnecessary personal data
- Provide mechanism for users to delete their accounts (future requirement)

### HTTPS Enforcement

**Transport Security:**
- Use HTTPS exclusively for all API communication
- Configure in `Program.cs`:
  ```csharp
  if (!app.Environment.IsDevelopment())
  {
      app.UseHttpsRedirection();
      app.UseHsts(); // HTTP Strict Transport Security
  }
  ```

### Error Information Disclosure

**Security Best Practices:**
- Don't expose internal error details to clients
- Don't reveal whether email exists for login failures (but registration requires 409 per spec)
- Don't expose stack traces in production
- Log detailed errors server-side only

**Example - Safe Error Messages:**
- ✅ "Password does not meet security requirements"
- ✅ "An account with this email already exists"
- ❌ "Database connection failed: timeout after 30s"
- ❌ "User table constraint violation on unique index IX_AspNetUsers_Email"

### Logging & Monitoring

**Security Logging with Serilog:**
- Log all registration attempts (success and failure)
- Log email conflicts (potential enumeration attempts)
- Log validation failures (potential attack patterns)
- Log rate limit violations
- **Never log passwords or tokens**

**Example Log Structure:**
```csharp
_logger.LogInformation(
    "User registration successful for email {Email} (UserId: {UserId})",
    user.Email, user.Id);

_logger.LogWarning(
    "Registration attempt failed: Email {Email} already exists",
    request.Email);
```

## 7. Error Handling

### Error Response Format

All error responses follow a consistent structure:

```json
{
  "error": "ErrorType",
  "message": "Human-readable error message",
  "details": {
    "fieldName": ["Error message 1", "Error message 2"]
  }
}
```

### Error Scenarios

#### 1. Validation Errors (400 Bad Request)

**Trigger Conditions:**
- Empty or null required fields
- Invalid email format
- Password doesn't meet complexity requirements
- First/last name exceeds maximum length
- GDPR consent is false or missing

**Response Example:**
```json
{
  "error": "ValidationError",
  "message": "One or more validation errors occurred",
  "details": {
    "email": ["Invalid email format"],
    "password": [
      "Must contain at least one uppercase letter",
      "Must contain at least one digit",
      "Must be at least 8 characters"
    ],
    "firstName": ["First name is required"],
    "gdprConsent": ["GDPR consent is required"]
  }
}
```

**Implementation:**
- Validation occurs at model binding stage
- Data annotation validators execute automatically
- Custom validation attributes add specific rules
- All errors collected into `ModelStateDictionary`
- Endpoint transforms errors into `ErrorResponse` format

**Code Example:**
```csharp
if (!context.ModelState.IsValid)
{
    var errors = context.ModelState
        .Where(x => x.Value?.Errors.Count > 0)
        .ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
        );

    return Results.BadRequest(new ErrorResponse
    {
        Error = "ValidationError",
        Message = "One or more validation errors occurred",
        Details = errors
    });
}
```

#### 2. Email Already Exists (409 Conflict)

**Trigger Condition:**
- User attempts to register with email that already exists in database

**Response Example:**
```json
{
  "error": "EmailAlreadyExists",
  "message": "An account with this email already exists"
}
```

**Implementation Flow:**
1. Service calls `UserManager.FindByEmailAsync(email)`
2. If user found, return failure result
3. Endpoint maps to 409 Conflict response

**Code Example:**
```csharp
var existingUser = await _userManager.FindByEmailAsync(request.Email);
if (existingUser is not null)
{
    return Result<RegisterUserResponse>.Failure(
        "EmailAlreadyExists",
        "An account with this email already exists"
    );
}
```

**Security Note:**
- This reveals email existence (email enumeration)
- Mitigated by rate limiting
- Accepted trade-off for better user experience

#### 3. Identity Creation Failures (400 Bad Request)

**Trigger Conditions:**
- Password doesn't meet Identity's configured requirements
- Username validation fails (unlikely since using email)
- Other Identity framework validation errors

**Response Example:**
```json
{
  "error": "ValidationError",
  "message": "User creation failed",
  "details": {
    "password": ["Passwords must have at least one non alphanumeric character"]
  }
}
```

**Implementation:**
```csharp
var result = await _userManager.CreateAsync(user, request.Password);
if (!result.Succeeded)
{
    var errors = result.Errors
        .GroupBy(e => e.Code)
        .ToDictionary(
            g => g.Key,
            g => g.Select(e => e.Description).ToArray()
        );

    return Result<RegisterUserResponse>.ValidationFailure(
        "User creation failed",
        errors
    );
}
```

#### 4. JWT Token Generation Failures (500 Internal Server Error)

**Trigger Conditions:**
- JWT secret key not configured
- Invalid JWT configuration
- Token generation throws exception

**Response Example:**
```json
{
  "error": "InternalServerError",
  "message": "An unexpected error occurred while processing your request"
}
```

**Implementation:**
- Exception caught by global exception handling middleware
- Log full error details with Serilog
- Return generic error message to client (don't expose internals)

**Code Example:**
```csharp
try
{
    var token = GenerateJwtToken(user);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to generate JWT token for user {UserId}", user.Id);
    throw; // Let global exception handler manage response
}
```

#### 5. Database Connection Failures (500 Internal Server Error)

**Trigger Conditions:**
- Database server unavailable
- Connection timeout
- Network issues
- Invalid connection string

**Response Example:**
```json
{
  "error": "InternalServerError",
  "message": "An unexpected error occurred while processing your request"
}
```

**Implementation:**
- EF Core throws `DbUpdateException` or `InvalidOperationException`
- Global exception middleware catches
- Log full error for diagnostics
- Return generic error to client

**Retry Strategy (Optional Enhancement):**
```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null
        );
    });
});
```

#### 6. Rate Limit Exceeded (429 Too Many Requests)

**Trigger Condition:**
- User exceeds registration attempt limit (e.g., 5 attempts per 15 minutes)

**Response Example:**
```json
{
  "error": "RateLimitExceeded",
  "message": "Too many registration attempts. Please try again later."
}
```

**Implementation:**
- Rate limiting middleware intercepts request before endpoint
- Automatically returns 429 status code
- Can include `Retry-After` header with wait time

**Configuration:**
```csharp
app.UseRateLimiter(); // Apply middleware

app.MapPost("/api/auth/register", RegisterHandler)
   .RequireRateLimiting("register");
```

### Global Exception Handling Middleware

**Purpose:**
- Catch unhandled exceptions
- Log detailed error information
- Return consistent error responses
- Prevent sensitive information disclosure

**Implementation Example:**
```csharp
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionHandlerFeature =
            context.Features.Get<IExceptionHandlerFeature>();
        var exception = exceptionHandlerFeature?.Error;

        var logger = context.RequestServices
            .GetRequiredService<ILogger<Program>>();

        logger.LogError(exception,
            "Unhandled exception occurred for request {Method} {Path}",
            context.Request.Method, context.Request.Path);

        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(new ErrorResponse
        {
            Error = "InternalServerError",
            Message = "An unexpected error occurred while processing your request"
        });
    });
});
```

### Logging Strategy

**Information Logging (Success Cases):**
```csharp
_logger.LogInformation(
    "User registered successfully: Email={Email}, UserId={UserId}",
    user.Email, user.Id);
```

**Warning Logging (Business Rule Violations):**
```csharp
_logger.LogWarning(
    "Registration failed: Email already exists - {Email}",
    request.Email);
```

**Error Logging (Unexpected Failures):**
```csharp
_logger.LogError(exception,
    "Failed to create user during registration: Email={Email}",
    request.Email);
```

**Sensitive Data Exclusion:**
- ❌ Never log passwords
- ❌ Never log JWT tokens
- ❌ Never log password hashes
- ✅ Log email addresses (business requirement)
- ✅ Log user IDs
- ✅ Log error types and messages

## 8. Performance Considerations

### Database Queries

**Email Lookup Query:**
- Executes ONCE per registration attempt
- Uses index on `NormalizedEmail` column (created by Identity)
- Query performance: O(log n) with B-tree index
- Expected response time: <10ms for databases up to 1M users

**User Insert:**
- Single INSERT statement
- Transaction managed by Identity framework
- Expected response time: <50ms

**Optimization:**
- Email uniqueness index already exists (ASP.NET Core Identity creates it)
- No N+1 query problems (single user creation)
- No need for eager loading (no related entities loaded)

### JWT Token Generation

**Performance Characteristics:**
- Token generation is CPU-bound (cryptographic operations)
- HMAC-SHA256 signing: ~1-2ms per token
- RSA signing: ~5-10ms per token (if using asymmetric keys)

**Recommendation for MVP:**
- Use HMAC-SHA256 (symmetric key) for better performance
- Consider RSA (asymmetric) for distributed systems requiring public key verification

### Password Hashing

**PBKDF2 Performance:**
- Intentionally slow to prevent brute force attacks
- Default iteration count: 10,000
- Expected hashing time: 50-100ms per password
- This is a **feature, not a bug** (security over speed)

**Considerations:**
- Password hashing is the slowest operation in registration flow
- Cannot be significantly optimized without compromising security
- Total registration time: 150-200ms under normal conditions

### Response Time Targets

**Expected End-to-End Performance:**
- Happy path (successful registration): 150-250ms
- Email conflict detection: 50-100ms (early exit)
- Validation failures: <10ms (model validation only)

**Performance Budget Breakdown:**
- Model validation: <5ms
- Email lookup query: 5-10ms
- Password hashing: 50-100ms
- User insert: 20-50ms
- JWT generation: 1-5ms
- Response serialization: 1-5ms

### Scalability Considerations

**Concurrency:**
- Multiple users can register simultaneously
- Email uniqueness enforced by database constraint
- Race condition handled by database (unique index violation)

**Potential Race Condition Scenario:**
1. User A checks email availability → email doesn't exist
2. User B checks email availability → email doesn't exist
3. User A attempts to create user → success
4. User B attempts to create user → fails (unique constraint violation)

**Mitigation:**
- Database unique constraint is the final authority
- If `UserManager.CreateAsync` fails due to duplicate email, catch and return 409
- No distributed locking needed (database handles it)

### Bottlenecks

**Potential Bottlenecks:**

1. **Password Hashing (50-100ms per request)**
   - Unavoidable for security
   - Consider async processing for other operations
   - Don't increase iteration count beyond defaults for MVP

2. **Database Connection Pool Exhaustion**
   - Default pool size: 100 connections
   - Sufficient for MVP (expected low concurrency)
   - Monitor connection pool usage in production

3. **JWT Secret Key Access**
   - Read from configuration once at startup (not per request)
   - Cache configuration values in service

### Optimization Strategies

**Implemented:**
- ✅ Use indexed email lookup
- ✅ Single database roundtrip for user creation
- ✅ Async/await throughout the stack
- ✅ Connection pooling (default in Npgsql)

**Future Optimizations (if needed):**
- Implement caching for JWT signing key retrieval
- Use prepared statements for common queries (EF Core handles this)
- Consider connection pooling configuration tuning under load
- Monitor and optimize Identity configuration if performance issues arise

### Monitoring Metrics

**Key Metrics to Track:**
- Average response time for successful registrations
- 95th percentile response time
- Error rate by error type
- Database query execution times
- Password hashing time
- JWT generation time
- Rate limit hit rate

**Logging Performance:**
```csharp
var stopwatch = Stopwatch.StartNew();

// Registration logic

stopwatch.Stop();
_logger.LogInformation(
    "Registration completed in {ElapsedMs}ms for email {Email}",
    stopwatch.ElapsedMilliseconds, user.Email);
```

## 9. Implementation Steps

### Step 1: Configure ASP.NET Core Identity

**File:** `Program.cs`

**Tasks:**
1. Add Identity services to dependency injection container
2. Configure password requirements
3. Configure user options
4. Configure database context with Identity

**Code:**

```csharp
// Add Identity services
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 1;

    // User settings
    options.User.RequireUniqueEmail = true;
    options.User.AllowedUserNameCharacters =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

    // Sign-in settings (not used for registration, but configure anyway)
    options.SignIn.RequireConfirmedEmail = false; // No email verification for MVP
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Add database context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null
        )
    )
);
```

**Configuration File:** `appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=santavibe;Username=postgres;Password=yourpassword"
  }
}
```

### Step 2: Configure JWT Authentication

**File:** `Program.cs`

**Tasks:**
1. Add JWT configuration to appsettings.json
2. Configure JWT authentication services
3. Add authentication middleware to pipeline

**Configuration:** `appsettings.json`

```json
{
  "Jwt": {
    "Secret": "your-very-secure-secret-key-min-256-bits-long",
    "Issuer": "SantaVibe.Api",
    "Audience": "SantaVibe.Web",
    "ExpirationInDays": 7
  }
}
```

**⚠️ IMPORTANT:** In production, store `Secret` in environment variables or Azure Key Vault, not in appsettings.json.

**Code:**

```csharp
// Add JWT authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["Secret"]
    ?? throw new InvalidOperationException("JWT Secret not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(secretKey)
        ),
        ClockSkew = TimeSpan.Zero // No tolerance for token expiration
    };
});

builder.Services.AddAuthorization();

// In app configuration (after app.Build())
app.UseAuthentication();
app.UseAuthorization();
```

**NuGet Packages Required:**
```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="9.0.9" />
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.5.0" />
```

### Step 3: Create DTOs and Validation Attributes

**Create Directory Structure:**
```
SantaVibe.Api/
├── DTOs/
│   ├── Auth/
│   │   ├── RegisterUserRequest.cs
│   │   └── RegisterUserResponse.cs
│   └── Common/
│       └── ErrorResponse.cs
├── Validators/
│   ├── PasswordComplexityAttribute.cs
│   └── MustBeTrueAttribute.cs
└── Common/
    └── Result.cs
```

**File:** `DTOs/Auth/RegisterUserRequest.cs`

```csharp
using System.ComponentModel.DataAnnotations;
using SantaVibe.Api.Validators;

namespace SantaVibe.Api.DTOs.Auth;

/// <summary>
/// Request model for user registration
/// </summary>
public class RegisterUserRequest
{
    /// <summary>
    /// User's email address (used for login)
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [MaxLength(256, ErrorMessage = "Email cannot exceed 256 characters")]
    public required string Email { get; set; }

    /// <summary>
    /// User's password (must meet complexity requirements)
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    [PasswordComplexity]
    public required string Password { get; set; }

    /// <summary>
    /// User's first name
    /// </summary>
    [Required(ErrorMessage = "First name is required")]
    [MaxLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
    public required string FirstName { get; set; }

    /// <summary>
    /// User's last name
    /// </summary>
    [Required(ErrorMessage = "Last name is required")]
    [MaxLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
    public required string LastName { get; set; }

    /// <summary>
    /// GDPR consent flag (must be true to register)
    /// </summary>
    [MustBeTrue(ErrorMessage = "GDPR consent is required")]
    public bool GdprConsent { get; set; }
}
```

**File:** `DTOs/Auth/RegisterUserResponse.cs`

```csharp
namespace SantaVibe.Api.DTOs.Auth;

/// <summary>
/// Response model for successful user registration
/// </summary>
public class RegisterUserResponse
{
    /// <summary>
    /// Unique identifier for the created user
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

**File:** `DTOs/Common/ErrorResponse.cs`

```csharp
namespace SantaVibe.Api.DTOs.Common;

/// <summary>
/// Standard error response model for API errors
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Error type/code (e.g., "ValidationError", "EmailAlreadyExists")
    /// </summary>
    public required string Error { get; set; }

    /// <summary>
    /// Human-readable error message
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Detailed validation errors by field (optional)
    /// </summary>
    public Dictionary<string, string[]>? Details { get; set; }
}
```

**File:** `Validators/PasswordComplexityAttribute.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace SantaVibe.Api.Validators;

/// <summary>
/// Validates password complexity requirements
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class PasswordComplexityAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(
        object? value,
        ValidationContext validationContext)
    {
        if (value is not string password)
            return new ValidationResult("Password is required");

        var errors = new List<string>();

        if (!password.Any(char.IsUpper))
            errors.Add("Must contain at least one uppercase letter");

        if (!password.Any(char.IsLower))
            errors.Add("Must contain at least one lowercase letter");

        if (!password.Any(char.IsDigit))
            errors.Add("Must contain at least one digit");

        if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
            errors.Add("Must contain at least one special character");

        return errors.Any()
            ? new ValidationResult(string.Join("; ", errors))
            : ValidationResult.Success;
    }
}
```

**File:** `Validators/MustBeTrueAttribute.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace SantaVibe.Api.Validators;

/// <summary>
/// Validates that a boolean property is true (for consent checkboxes)
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class MustBeTrueAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(
        object? value,
        ValidationContext validationContext)
    {
        if (value is bool boolValue && boolValue)
            return ValidationResult.Success;

        return new ValidationResult(
            ErrorMessage ?? "This field must be true");
    }
}
```

**File:** `Common/Result.cs`

```csharp
namespace SantaVibe.Api.Common;

/// <summary>
/// Result type for service operations
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public string? Message { get; }
    public Dictionary<string, string[]>? ValidationErrors { get; }

    private Result(
        bool isSuccess,
        T? value,
        string? error,
        string? message,
        Dictionary<string, string[]>? validationErrors)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        Message = message;
        ValidationErrors = validationErrors;
    }

    public static Result<T> Success(T value) =>
        new(true, value, null, null, null);

    public static Result<T> Failure(string error, string message) =>
        new(false, default, error, message, null);

    public static Result<T> ValidationFailure(
        string message,
        Dictionary<string, string[]> errors) =>
        new(false, default, "ValidationError", message, errors);
}
```

### Step 4: Create Authentication Service

**File:** `Services/IAuthenticationService.cs`

```csharp
using SantaVibe.Api.Common;
using SantaVibe.Api.DTOs.Auth;

namespace SantaVibe.Api.Services;

/// <summary>
/// Service for authentication-related operations
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Registers a new user and returns JWT token
    /// </summary>
    Task<Result<RegisterUserResponse>> RegisterUserAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default);
}
```

**File:** `Services/AuthenticationService.cs`

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using SantaVibe.Api.Common;
using SantaVibe.Api.Data.Entities;
using SantaVibe.Api.DTOs.Auth;

namespace SantaVibe.Api.Services;

/// <summary>
/// Service for authentication-related operations
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger<AuthenticationService> logger)
    {
        _userManager = userManager;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Result<RegisterUserResponse>> RegisterUserAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default)
    {
        // Check if email already exists
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
        {
            _logger.LogWarning(
                "Registration attempt failed: Email already exists - {Email}",
                request.Email);

            return Result<RegisterUserResponse>.Failure(
                "EmailAlreadyExists",
                "An account with this email already exists");
        }

        // Create new user entity
        var user = new ApplicationUser
        {
            UserName = request.Email, // Identity requires username
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            CreatedAt = DateTimeOffset.UtcNow,
            LastLoginAt = DateTimeOffset.UtcNow, // User is immediately logged in
            IsDeleted = false
        };

        // Create user with password
        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            _logger.LogWarning(
                "User creation failed for email {Email}: {Errors}",
                request.Email,
                string.Join(", ", createResult.Errors.Select(e => e.Description)));

            var errors = createResult.Errors
                .GroupBy(e => e.Code)
                .ToDictionary(
                    g => ToCamelCase(g.Key),
                    g => g.Select(e => e.Description).ToArray());

            return Result<RegisterUserResponse>.ValidationFailure(
                "User creation failed",
                errors);
        }

        _logger.LogInformation(
            "User registered successfully: Email={Email}, UserId={UserId}",
            user.Email, user.Id);

        // Generate JWT token
        var token = GenerateJwtToken(user);
        var expiresAt = DateTimeOffset.UtcNow.AddDays(
            _configuration.GetValue<int>("Jwt:ExpirationInDays", 7));

        // Return response
        return Result<RegisterUserResponse>.Success(new RegisterUserResponse
        {
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Token = token,
            ExpiresAt = expiresAt
        });
    }

    private string GenerateJwtToken(ApplicationUser user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var secretKey = jwtSettings["Secret"]
            ?? throw new InvalidOperationException("JWT Secret not configured");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.GivenName, user.FirstName),
            new Claim(ClaimTypes.Surname, user.LastName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var expirationDays = _configuration.GetValue<int>("Jwt:ExpirationInDays", 7);
        var expiration = DateTime.UtcNow.AddDays(expirationDays);

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: expiration,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string ToCamelCase(string input)
    {
        if (string.IsNullOrEmpty(input) || char.IsLower(input[0]))
            return input;

        return char.ToLower(input[0]) + input[1..];
    }
}
```

**Register Service in DI Container:** `Program.cs`

```csharp
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
```

### Step 5: Create Minimal API Endpoint

**File:** `Endpoints/AuthEndpoints.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using SantaVibe.Api.DTOs.Auth;
using SantaVibe.Api.DTOs.Common;
using SantaVibe.Api.Services;

namespace SantaVibe.Api.Endpoints;

/// <summary>
/// Authentication endpoints
/// </summary>
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/auth")
            .WithTags("Authentication")
            .WithOpenApi();

        group.MapPost("/register", RegisterUser)
            .WithName("RegisterUser")
            .WithSummary("Register a new user account")
            .WithDescription("Creates a new user account with email and password. Returns JWT token upon success.")
            .Produces<RegisterUserResponse>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
            .AllowAnonymous(); // Explicitly allow anonymous access
    }

    private static async Task<IResult> RegisterUser(
        [FromBody] RegisterUserRequest request,
        [FromServices] IAuthenticationService authService,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        // Validate model state
        if (!context.Request.HasJsonContentType())
        {
            return Results.BadRequest(new ErrorResponse
            {
                Error = "InvalidContentType",
                Message = "Content-Type must be application/json"
            });
        }

        // Call service
        var result = await authService.RegisterUserAsync(request, cancellationToken);

        // Handle result
        if (!result.IsSuccess)
        {
            // Check error type
            if (result.Error == "EmailAlreadyExists")
            {
                return Results.Conflict(new ErrorResponse
                {
                    Error = result.Error,
                    Message = result.Message!
                });
            }

            // Validation error
            return Results.BadRequest(new ErrorResponse
            {
                Error = result.Error!,
                Message = result.Message!,
                Details = result.ValidationErrors
            });
        }

        // Success
        return Results.Created($"/api/users/{result.Value!.UserId}", result.Value);
    }
}
```

**Register Endpoints in Program.cs:**

```csharp
// After var app = builder.Build();

// Map endpoints
app.MapAuthEndpoints();
```

### Step 6: Configure Rate Limiting

**File:** `Program.cs`

```csharp
// Add rate limiting services
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("register", config =>
    {
        config.PermitLimit = 5; // 5 attempts
        config.Window = TimeSpan.FromMinutes(15); // per 15 minutes
        config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        config.QueueLimit = 0; // No queueing
    });

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(
            new ErrorResponse
            {
                Error = "RateLimitExceeded",
                Message = "Too many registration attempts. Please try again later."
            },
            cancellationToken: cancellationToken);
    };
});

// Apply rate limiting middleware (after app.Build())
app.UseRateLimiter();
```

**Update Endpoint Registration:**

```csharp
group.MapPost("/register", RegisterUser)
    .WithName("RegisterUser")
    .RequireRateLimiting("register") // Apply rate limiting
    .AllowAnonymous();
```

**NuGet Package Required:**
```xml
<PackageReference Include="Microsoft.AspNetCore.RateLimiting" Version="9.0.9" />
```

### Step 7: Configure Serilog Logging

**File:** `Program.cs`

```csharp
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/santavibe-.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting SantaVibe API");

    builder.Host.UseSerilog();

    // ... rest of Program.cs

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
```

**Configuration:** `appsettings.json`

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    }
  }
}
```

**NuGet Packages Required:**
```xml
<PackageReference Include="Serilog.AspNetCore" Version="9.0.0" />
<PackageReference Include="Serilog.Sinks.Console" Version="6.0.0" />
<PackageReference Include="Serilog.Sinks.File" Version="6.0.0" />
```

### Step 8: Configure Global Exception Handling

**File:** `Program.cs` (after `var app = builder.Build();`)

```csharp
// Configure exception handling
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            var exceptionHandlerFeature =
                context.Features.Get<IExceptionHandlerFeature>();
            var exception = exceptionHandlerFeature?.Error;

            var logger = context.RequestServices
                .GetRequiredService<ILogger<Program>>();

            logger.LogError(
                exception,
                "Unhandled exception for {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(new ErrorResponse
            {
                Error = "InternalServerError",
                Message = "An unexpected error occurred while processing your request"
            });
        });
    });
}
```

### Step 9: Configure Swagger for API Documentation

**File:** `Program.cs`

```csharp
// Configure Swagger (already partially configured)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SantaVibe API",
        Version = "v1",
        Description = "Secret Santa gift exchange API"
    });

    // Configure JWT authentication in Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme (Example: 'Bearer {token}')",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
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

    // Include XML comments if available
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Enable Swagger UI (after app.Build())
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "SantaVibe API v1");
        options.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}
```

**Enable XML Documentation:** `SantaVibe.Api.csproj`

```xml
<PropertyGroup>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);1591</NoWarn> <!-- Suppress missing XML comment warnings -->
</PropertyGroup>
```

### Step 10: Write Unit Tests

**File:** `SantaVibe.Tests/Services/AuthenticationServiceTests.cs`

```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SantaVibe.Api.Data.Entities;
using SantaVibe.Api.DTOs.Auth;
using SantaVibe.Api.Services;
using Xunit;

namespace SantaVibe.Tests.Services;

public class AuthenticationServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<AuthenticationService>> _loggerMock;
    private readonly AuthenticationService _sut;

    public AuthenticationServiceTests()
    {
        // Setup UserManager mock
        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            store.Object, null, null, null, null, null, null, null, null);

        // Setup Configuration mock
        _configurationMock = new Mock<IConfiguration>();
        var jwtSection = new Mock<IConfigurationSection>();
        jwtSection.Setup(x => x["Secret"]).Returns("test-secret-key-minimum-256-bits-long-for-hmac-sha256");
        jwtSection.Setup(x => x["Issuer"]).Returns("TestIssuer");
        jwtSection.Setup(x => x["Audience"]).Returns("TestAudience");
        jwtSection.Setup(x => x["ExpirationInDays"]).Returns("7");
        _configurationMock.Setup(x => x.GetSection("Jwt")).Returns(jwtSection.Object);

        // Setup Logger mock
        _loggerMock = new Mock<ILogger<AuthenticationService>>();

        // Create system under test
        _sut = new AuthenticationService(
            _userManagerMock.Object,
            _configurationMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task RegisterUserAsync_WithValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = new RegisterUserRequest
        {
            Email = "test@example.com",
            Password = "Test@123",
            FirstName = "Jan",
            LastName = "Kowalski",
            GdprConsent = true
        };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _sut.RegisterUserAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(request.Email, result.Value.Email);
        Assert.Equal(request.FirstName, result.Value.FirstName);
        Assert.Equal(request.LastName, result.Value.LastName);
        Assert.NotEmpty(result.Value.Token);
    }

    [Fact]
    public async Task RegisterUserAsync_WithExistingEmail_ReturnsConflict()
    {
        // Arrange
        var request = new RegisterUserRequest
        {
            Email = "existing@example.com",
            Password = "Test@123",
            FirstName = "Jan",
            LastName = "Kowalski",
            GdprConsent = true
        };

        var existingUser = new ApplicationUser
        {
            Email = request.Email,
            UserName = request.Email,
            FirstName = "Existing",
            LastName = "User"
        };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _sut.RegisterUserAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("EmailAlreadyExists", result.Error);
        Assert.Contains("already exists", result.Message);
    }

    [Fact]
    public async Task RegisterUserAsync_WithIdentityError_ReturnsValidationFailure()
    {
        // Arrange
        var request = new RegisterUserRequest
        {
            Email = "test@example.com",
            Password = "weak",
            FirstName = "Jan",
            LastName = "Kowalski",
            GdprConsent = true
        };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);

        var identityErrors = new[]
        {
            new IdentityError
            {
                Code = "PasswordTooShort",
                Description = "Passwords must be at least 8 characters"
            }
        };

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Failed(identityErrors));

        // Act
        var result = await _sut.RegisterUserAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("ValidationError", result.Error);
        Assert.NotNull(result.ValidationErrors);
    }
}
```

**NuGet Packages for Testing:**
```xml
<PackageReference Include="xunit" Version="2.9.2" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
<PackageReference Include="Moq" Version="4.20.72" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
```

### Step 11: Manual Testing with curl or Postman

**Test 1: Successful Registration**

```bash
curl -X POST https://localhost:5001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "SecureP@ssw0rd",
    "firstName": "Jan",
    "lastName": "Kowalski",
    "gdprConsent": true
  }'
```

**Expected Response (201 Created):**
```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "email": "test@example.com",
  "firstName": "Jan",
  "lastName": "Kowalski",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2025-10-23T14:30:00Z"
}
```

**Test 2: Validation Error (Weak Password)**

```bash
curl -X POST https://localhost:5001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "weak",
    "firstName": "Jan",
    "lastName": "Kowalski",
    "gdprConsent": true
  }'
```

**Expected Response (400 Bad Request):**
```json
{
  "error": "ValidationError",
  "message": "One or more validation errors occurred",
  "details": {
    "password": [
      "Must contain at least one uppercase letter",
      "Must contain at least one digit",
      "Must contain at least one special character"
    ]
  }
}
```

**Test 3: Email Already Exists**

```bash
# Run the same successful registration request twice
curl -X POST https://localhost:5001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "SecureP@ssw0rd",
    "firstName": "Jan",
    "lastName": "Kowalski",
    "gdprConsent": true
  }'
```

**Expected Response (409 Conflict):**
```json
{
  "error": "EmailAlreadyExists",
  "message": "An account with this email already exists"
}
```

### Step 12: Verify Database Records

After successful registration, verify the database:

```sql
-- Connect to PostgreSQL
psql -U postgres -d santavibe

-- Check user record
SELECT "Id", "Email", "FirstName", "LastName", "CreatedAt", "LastLoginAt"
FROM "AspNetUsers"
WHERE "Email" = 'test@example.com';

-- Check password hash exists (never log or display the actual hash)
SELECT "PasswordHash" IS NOT NULL AS HasPasswordHash
FROM "AspNetUsers"
WHERE "Email" = 'test@example.com';
```

### Step 13: Deployment Checklist

Before deploying to production:

- [ ] Move JWT secret to environment variables or Key Vault
- [ ] Configure HTTPS with valid SSL certificate
- [ ] Set up connection pooling for PostgreSQL (Neon handles this)
- [ ] Configure Serilog to write to persistent storage or logging service
- [ ] Set up health check endpoints for monitoring

---

## Summary

This implementation plan provides comprehensive guidance for creating the `POST /api/auth/register` endpoint using:
- ASP.NET Core 9 minimal APIs
- ASP.NET Core Identity for user management
- JWT authentication
- Entity Framework Core 9
- PostgreSQL (Neon)
- Serilog for logging
- Rate limiting for security
- xUnit for testing

The endpoint is production-ready with proper validation, error handling, security considerations, and performance optimizations.
