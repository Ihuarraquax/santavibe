using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using SantaVibe.Api.Common;
using SantaVibe.Api.Features.Authentication.Login;
using SantaVibe.Api.Features.Authentication.Register;
using SantaVibe.Api.Features.Profile.UpdateProfile;
using SantaVibe.Tests.Infrastructure;

namespace SantaVibe.Tests.Features.Profile.UpdateProfile;

/// <summary>
/// Integration tests for the PUT /api/profile endpoint
/// </summary>
public class UpdateProfileEndpointIntegrationTests : IClassFixture<SantaVibeWebApplicationFactory>
{
    private readonly HttpClient client;
    private readonly SantaVibeWebApplicationFactory factory;

    public UpdateProfileEndpointIntegrationTests(SantaVibeWebApplicationFactory factory)
    {
        this.factory = factory;
        client = factory.CreateClient();
    }

    /// <summary>
    /// Helper method to register and login a test user
    /// </summary>
    private async Task<(string userId, string token, string email)> RegisterAndLoginTestUserAsync(
        string email,
        string password,
        string firstName = "Test",
        string lastName = "User")
    {
        // Register
        var registerRequest = new RegisterRequest
        {
            Email = email,
            Password = password,
            FirstName = firstName,
            LastName = lastName,
            GdprConsent = true
        };

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", registerRequest);
        registerResponse.EnsureSuccessStatusCode();

        // Login
        var loginRequest = new LoginRequest
        {
            Email = email,
            Password = password
        };

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
        loginResponse.EnsureSuccessStatusCode();

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        return (loginResult!.UserId, loginResult.Token, email);
    }

    [Fact]
    public async Task PUT_Profile_WithValidData_Returns200WithUpdatedProfile()
    {
        // Arrange
        var email = $"profile.update.{Guid.NewGuid()}@example.com";
        var password = "SecurePass123!";
        var originalFirstName = "Jan";
        var originalLastName = "Kowalski";

        var (userId, token, _) = await RegisterAndLoginTestUserAsync(email, password, originalFirstName, originalLastName);

        var updateRequest = new UpdateProfileRequest
        {
            FirstName = "Adam",
            LastName = "Nowak"
        };

        var request = new HttpRequestMessage(HttpMethod.Put, "/api/profile")
        {
            Content = JsonContent.Create(updateRequest)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<UpdateProfileResponse>();
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(email, result.Email); // Email should NOT change
        Assert.Equal("Adam", result.FirstName);
        Assert.Equal("Nowak", result.LastName);
        Assert.True(result.CreatedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task PUT_Profile_WithoutToken_Returns401()
    {
        // Arrange
        var updateRequest = new UpdateProfileRequest
        {
            FirstName = "Test",
            LastName = "User"
        };

        var request = new HttpRequestMessage(HttpMethod.Put, "/api/profile")
        {
            Content = JsonContent.Create(updateRequest)
        };
        // No Authorization header

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PUT_Profile_WithEmptyFirstName_Returns400()
    {
        // Arrange
        var email = $"profile.update.{Guid.NewGuid()}@example.com";
        var password = "SecurePass123!";

        var (_, token, _) = await RegisterAndLoginTestUserAsync(email, password);

        var updateRequest = new UpdateProfileRequest
        {
            FirstName = "", // Empty first name
            LastName = "Nowak"
        };

        var request = new HttpRequestMessage(HttpMethod.Put, "/api/profile")
        {
            Content = JsonContent.Create(updateRequest)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("ValidationError", error.Error);
        Assert.NotNull(error.Details);
        Assert.True(error.Details.ContainsKey("FirstName"));
    }

    [Fact]
    public async Task PUT_Profile_WithEmptyLastName_Returns400()
    {
        // Arrange
        var email = $"profile.update.{Guid.NewGuid()}@example.com";
        var password = "SecurePass123!";

        var (_, token, _) = await RegisterAndLoginTestUserAsync(email, password);

        var updateRequest = new UpdateProfileRequest
        {
            FirstName = "Adam",
            LastName = "" // Empty last name
        };

        var request = new HttpRequestMessage(HttpMethod.Put, "/api/profile")
        {
            Content = JsonContent.Create(updateRequest)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("ValidationError", error.Error);
        Assert.NotNull(error.Details);
        Assert.True(error.Details.ContainsKey("LastName"));
    }

    [Fact]
    public async Task PUT_Profile_WithTooLongFirstName_Returns400()
    {
        // Arrange
        var email = $"profile.update.{Guid.NewGuid()}@example.com";
        var password = "SecurePass123!";

        var (_, token, _) = await RegisterAndLoginTestUserAsync(email, password);

        var updateRequest = new UpdateProfileRequest
        {
            FirstName = new string('A', 101), // 101 characters (exceeds 100 limit)
            LastName = "Nowak"
        };

        var request = new HttpRequestMessage(HttpMethod.Put, "/api/profile")
        {
            Content = JsonContent.Create(updateRequest)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("ValidationError", error.Error);
        Assert.NotNull(error.Details);
        Assert.True(error.Details.ContainsKey("FirstName"));
    }

    [Fact]
    public async Task PUT_Profile_WithTooLongLastName_Returns400()
    {
        // Arrange
        var email = $"profile.update.{Guid.NewGuid()}@example.com";
        var password = "SecurePass123!";

        var (_, token, _) = await RegisterAndLoginTestUserAsync(email, password);

        var updateRequest = new UpdateProfileRequest
        {
            FirstName = "Adam",
            LastName = new string('B', 101) // 101 characters (exceeds 100 limit)
        };

        var request = new HttpRequestMessage(HttpMethod.Put, "/api/profile")
        {
            Content = JsonContent.Create(updateRequest)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("ValidationError", error.Error);
        Assert.NotNull(error.Details);
        Assert.True(error.Details.ContainsKey("LastName"));
    }

    [Fact]
    public async Task PUT_Profile_WithNullRequestBody_Returns400()
    {
        // Arrange
        var email = $"profile.update.{Guid.NewGuid()}@example.com";
        var password = "SecurePass123!";

        var (_, token, _) = await RegisterAndLoginTestUserAsync(email, password);

        var request = new HttpRequestMessage(HttpMethod.Put, "/api/profile")
        {
            Content = new StringContent("null", System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("ValidationError", error.Error);
    }

    [Fact]
    public async Task PUT_Profile_EmailRemainsUnchanged_AfterUpdate()
    {
        // Arrange
        var email = $"profile.update.{Guid.NewGuid()}@example.com";
        var password = "SecurePass123!";

        var (userId, token, originalEmail) = await RegisterAndLoginTestUserAsync(email, password);

        var updateRequest = new UpdateProfileRequest
        {
            FirstName = "NewFirstName",
            LastName = "NewLastName"
        };

        var request = new HttpRequestMessage(HttpMethod.Put, "/api/profile")
        {
            Content = JsonContent.Create(updateRequest)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<UpdateProfileResponse>();
        Assert.NotNull(result);
        Assert.Equal(originalEmail, result.Email); // Email must remain unchanged
    }
}
