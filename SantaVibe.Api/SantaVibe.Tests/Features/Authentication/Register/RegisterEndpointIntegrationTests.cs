using System.Net;
using System.Net.Http.Json;
using SantaVibe.Api.DTOs.Common;
using SantaVibe.Api.Features.Authentication.Register;
using SantaVibe.Tests.Infrastructure;

namespace SantaVibe.Tests.Features.Authentication.Register;

/// <summary>
/// Integration tests for the registration endpoint using TestContainers and WebApplicationFactory
/// </summary>
public class RegisterEndpointIntegrationTests : IClassFixture<SantaVibeWebApplicationFactory>
{
    private readonly HttpClient client;
    private readonly SantaVibeWebApplicationFactory factory;

    public RegisterEndpointIntegrationTests(SantaVibeWebApplicationFactory factory)
    {
        this.factory = factory;
        client = factory.CreateClient();
    }

    [Fact]
    public async Task POST_Register_WithValidRequest_ReturnsCreatedWithToken()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = $"john.doe.{Guid.NewGuid()}@example.com", // Unique email
            Password = "SecurePass123!",
            FirstName = "John",
            LastName = "Doe",
            GdprConsent = true
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.UserId);
        Assert.Equal(request.Email, result.Email);
        Assert.Equal(request.FirstName, result.FirstName);
        Assert.Equal(request.LastName, result.LastName);
        Assert.NotNull(result.Token);
        Assert.NotEmpty(result.Token);
        Assert.True(result.ExpiresAt > DateTimeOffset.UtcNow);

        // Verify Location header
        Assert.NotNull(response.Headers.Location);
        Assert.Contains($"/api/users/{result.UserId}", response.Headers.Location.ToString());
    }

    [Fact]
    public async Task POST_Register_WithDuplicateEmail_ReturnsConflict()
    {
        // Arrange
        var email = $"duplicate.{Guid.NewGuid()}@example.com";
        var firstRequest = new RegisterRequest
        {
            Email = email,
            Password = "SecurePass123!",
            FirstName = "First",
            LastName = "User",
            GdprConsent = true
        };

        // First registration (should succeed)
        var firstResponse = await client.PostAsJsonAsync("/api/auth/register", firstRequest);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        // Second registration with same email
        var secondRequest = new RegisterRequest
        {
            Email = email,
            Password = "DifferentPass123!",
            FirstName = "Second",
            LastName = "User",
            GdprConsent = true
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", secondRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("EmailAlreadyExists", error.Error);
        Assert.Equal("An account with this email address already exists", error.Message);
    }

    [Fact]
    public async Task POST_Register_WithMissingEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            // Email is missing
            Password = "SecurePass123!",
            FirstName = "John",
            LastName = "Doe",
            GdprConsent = true
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        // Model binding fails when required properties are missing, returns 400
        // This happens before our endpoint handler is called
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        // If there's a response body, verify it's the right error
        var contentString = await response.Content.ReadAsStringAsync();
        if (!string.IsNullOrWhiteSpace(contentString))
        {
            var error = System.Text.Json.JsonSerializer.Deserialize<ErrorResponse>(contentString,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (error != null)
            {
                Assert.Equal("ValidationError", error.Error);
            }
        }
    }

    [Fact]
    public async Task POST_Register_WithInvalidEmailFormat_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "not-an-email",
            Password = "SecurePass123!",
            FirstName = "John",
            LastName = "Doe",
            GdprConsent = true
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("ValidationError", error.Error);
        Assert.NotNull(error.Details);
        Assert.Contains("Email", error.Details.Keys);
    }

    [Fact]
    public async Task POST_Register_WithWeakPassword_NoSpecialChar_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = $"test.{Guid.NewGuid()}@example.com",
            Password = "WeakPass123", // Missing special character
            FirstName = "John",
            LastName = "Doe",
            GdprConsent = true
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("ValidationError", error.Error);
        Assert.NotNull(error.Details);
        Assert.Contains("Password", error.Details.Keys);
    }

    [Fact]
    public async Task POST_Register_WithWeakPassword_NoUppercase_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = $"test.{Guid.NewGuid()}@example.com",
            Password = "weakpass123!", // Missing uppercase
            FirstName = "John",
            LastName = "Doe",
            GdprConsent = true
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("ValidationError", error.Error);
        Assert.NotNull(error.Details);
        Assert.Contains("Password", error.Details.Keys);
    }

    [Fact]
    public async Task POST_Register_WithShortPassword_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = $"test.{Guid.NewGuid()}@example.com",
            Password = "Pass1!", // Only 6 characters
            FirstName = "John",
            LastName = "Doe",
            GdprConsent = true
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("ValidationError", error.Error);
        Assert.NotNull(error.Details);
        Assert.Contains("Password", error.Details.Keys);
    }

    [Fact]
    public async Task POST_Register_WithoutGdprConsent_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = $"test.{Guid.NewGuid()}@example.com",
            Password = "SecurePass123!",
            FirstName = "John",
            LastName = "Doe",
            GdprConsent = false // GDPR consent not given
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("ValidationError", error.Error);
        Assert.NotNull(error.Details);
        Assert.Contains("GdprConsent", error.Details.Keys);
    }

    [Fact]
    public async Task POST_Register_WithLongFirstName_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = $"test.{Guid.NewGuid()}@example.com",
            Password = "SecurePass123!",
            FirstName = new string('A', 101), // 101 characters (max is 100)
            LastName = "Doe",
            GdprConsent = true
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("ValidationError", error.Error);
        Assert.NotNull(error.Details);
        Assert.Contains("FirstName", error.Details.Keys);
    }

    [Fact]
    public async Task POST_Register_GeneratesValidJwtToken()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = $"jwt.test.{Guid.NewGuid()}@example.com",
            Password = "SecurePass123!",
            FirstName = "JWT",
            LastName = "Test",
            GdprConsent = true
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.Token);

        // JWT tokens have 3 parts separated by dots (header.payload.signature)
        var tokenParts = result.Token.Split('.');
        Assert.Equal(3, tokenParts.Length);

        // Each part should be base64 encoded (not empty)
        Assert.All(tokenParts, part => Assert.NotEmpty(part));
    }

    [Fact]
    public async Task POST_Register_StoresUserInDatabase()
    {
        // Arrange
        var email = $"db.test.{Guid.NewGuid()}@example.com";
        var request = new RegisterRequest
        {
            Email = email,
            Password = "SecurePass123!",
            FirstName = "Database",
            LastName = "Test",
            GdprConsent = true
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Try to register again with the same email - should fail (proves it's in DB)
        var duplicateResponse = await client.PostAsJsonAsync("/api/auth/register", request);
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task POST_Register_WithMultipleValidationErrors_ReturnsAllErrors()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "invalid-email", // Invalid format
            Password = "weak", // Too short, no uppercase, no digit, no special char
            FirstName = "", // Empty
            LastName = "", // Empty
            GdprConsent = false // Not accepted
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("ValidationError", error.Error);
        Assert.NotNull(error.Details);

        // Should have errors for multiple fields
        Assert.True(error.Details.Count >= 4); // Email, Password, FirstName, LastName, GdprConsent
    }
}
