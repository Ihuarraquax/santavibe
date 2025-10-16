using System.Net;
using System.Net.Http.Json;
using SantaVibe.Api.Common;
using SantaVibe.Api.Features.Authentication.Login;
using SantaVibe.Api.Features.Authentication.Register;
using SantaVibe.Tests.Infrastructure;

namespace SantaVibe.Tests.Features.Authentication.Login;

/// <summary>
/// Integration tests for the login endpoint using TestContainers and WebApplicationFactory
/// </summary>
public class LoginEndpointIntegrationTests : IClassFixture<SantaVibeWebApplicationFactory>
{
    private readonly HttpClient client;
    private readonly SantaVibeWebApplicationFactory factory;

    public LoginEndpointIntegrationTests(SantaVibeWebApplicationFactory factory)
    {
        this.factory = factory;
        client = factory.CreateClient();
    }

    /// <summary>
    /// Helper method to register a test user
    /// </summary>
    private async Task<RegisterResponse> RegisterTestUserAsync(string email, string password)
    {
        var registerRequest = new RegisterRequest
        {
            Email = email,
            Password = password,
            FirstName = "Test",
            LastName = "User",
            GdprConsent = true
        };

        var response = await client.PostAsJsonAsync("/api/auth/register", registerRequest);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<RegisterResponse>())!;
    }

    [Fact]
    public async Task POST_Login_WithValidCredentials_ReturnsOkWithToken()
    {
        // Arrange
        var email = $"login.test.{Guid.NewGuid()}@example.com";
        var password = "SecurePass123!";

        // Register user first
        await RegisterTestUserAsync(email, password);

        var loginRequest = new LoginRequest
        {
            Email = email,
            Password = password
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.UserId);
        Assert.Equal(email, result.Email);
        Assert.Equal("Test", result.FirstName);
        Assert.Equal("User", result.LastName);
        Assert.NotNull(result.Token);
        Assert.NotEmpty(result.Token);
        Assert.True(result.ExpiresAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task POST_Login_WithNonExistentEmail_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = $"nonexistent.{Guid.NewGuid()}@example.com",
            Password = "SecurePass123!"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("InvalidCredentials", error.Error);
        Assert.Equal("Invalid email or password", error.Message);
    }

    [Fact]
    public async Task POST_Login_WithWrongPassword_ReturnsUnauthorized()
    {
        // Arrange
        var email = $"password.test.{Guid.NewGuid()}@example.com";
        var correctPassword = "CorrectPass123!";
        var wrongPassword = "WrongPass123!";

        // Register user first
        await RegisterTestUserAsync(email, correctPassword);

        var loginRequest = new LoginRequest
        {
            Email = email,
            Password = wrongPassword
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("InvalidCredentials", error.Error);
        Assert.Equal("Invalid email or password", error.Message);
    }

    [Fact]
    public async Task POST_Login_WithMissingEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            // Email is missing
            Password = "SecurePass123!"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

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
    public async Task POST_Login_WithMissingPassword_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            Email = "test@example.com"
            // Password is missing
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

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
    public async Task POST_Login_WithInvalidEmailFormat_ReturnsBadRequest()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "not-an-email",
            Password = "SecurePass123!"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("ValidationError", error.Error);
        Assert.NotNull(error.Details);
        Assert.Contains("Email", error.Details.Keys);
    }

    [Fact]
    public async Task POST_Login_WithEmptyRequest_ReturnsBadRequest()
    {
        // Arrange
        var request = new { };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task POST_Login_WithNullBody_ReturnsBadRequest()
    {
        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", (LoginRequest?)null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("ValidationError", error.Error);
        Assert.Contains("Request body is required", error.Message);
    }

    [Fact]
    public async Task POST_Login_GeneratesValidJwtToken()
    {
        // Arrange
        var email = $"jwt.login.{Guid.NewGuid()}@example.com";
        var password = "SecurePass123!";

        // Register user first
        await RegisterTestUserAsync(email, password);

        var loginRequest = new LoginRequest
        {
            Email = email,
            Password = password
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.Token);

        // JWT tokens have 3 parts separated by dots (header.payload.signature)
        var tokenParts = result.Token.Split('.');
        Assert.Equal(3, tokenParts.Length);

        // Each part should be base64 encoded (not empty)
        Assert.All(tokenParts, part => Assert.NotEmpty(part));
    }

    [Fact]
    public async Task POST_Login_MultipleSuccessfulLogins_GeneratesDifferentTokens()
    {
        // Arrange
        var email = $"multiple.login.{Guid.NewGuid()}@example.com";
        var password = "SecurePass123!";

        // Register user first
        await RegisterTestUserAsync(email, password);

        var loginRequest = new LoginRequest
        {
            Email = email,
            Password = password
        };

        // Act - Login twice
        var response1 = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
        await Task.Delay(100); // Small delay to ensure different JTI
        var response2 = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

        var result1 = await response1.Content.ReadFromJsonAsync<LoginResponse>();
        var result2 = await response2.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(result1);
        Assert.NotNull(result2);

        // Tokens should be different (different JTI and timestamp)
        Assert.NotEqual(result1.Token, result2.Token);
    }

    [Fact]
    public async Task POST_Login_CaseSensitivePassword_ReturnsUnauthorized()
    {
        // Arrange
        var email = $"case.test.{Guid.NewGuid()}@example.com";
        var correctPassword = "SecurePass123!";
        var wrongCasePassword = "securepass123!"; // Different case

        // Register user first
        await RegisterTestUserAsync(email, correctPassword);

        var loginRequest = new LoginRequest
        {
            Email = email,
            Password = wrongCasePassword
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("InvalidCredentials", error.Error);
    }

    [Fact]
    public async Task POST_Login_CaseInsensitiveEmail_ReturnsOk()
    {
        // Arrange
        var email = $"CaseSensitive.{Guid.NewGuid()}@Example.COM";
        var password = "SecurePass123!";

        // Register user first
        await RegisterTestUserAsync(email, password);

        // Login with lowercase email
        var loginRequest = new LoginRequest
        {
            Email = email.ToLower(),
            Password = password
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task POST_Login_WithExcessivelyLongEmail_ReturnsBadRequest()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = new string('a', 257) + "@example.com", // Exceeds 256 char limit
            Password = "SecurePass123!"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("ValidationError", error.Error);
    }

    [Fact]
    public async Task POST_Login_ReturnsUserDetails()
    {
        // Arrange
        var email = $"details.test.{Guid.NewGuid()}@example.com";
        var password = "SecurePass123!";

        // Register user first
        var registrationResponse = await RegisterTestUserAsync(email, password);

        var loginRequest = new LoginRequest
        {
            Email = email,
            Password = password
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(result);

        // Verify user details match registration
        Assert.Equal(registrationResponse.UserId, result.UserId);
        Assert.Equal(registrationResponse.Email, result.Email);
        Assert.Equal(registrationResponse.FirstName, result.FirstName);
        Assert.Equal(registrationResponse.LastName, result.LastName);
    }
}
