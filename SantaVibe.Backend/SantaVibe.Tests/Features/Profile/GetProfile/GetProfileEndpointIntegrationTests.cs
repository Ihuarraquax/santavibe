using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using SantaVibe.Api.Common;
using SantaVibe.Api.Features.Authentication.Login;
using SantaVibe.Api.Features.Authentication.Register;
using SantaVibe.Api.Features.Profile.GetProfile;
using SantaVibe.Tests.Infrastructure;

namespace SantaVibe.Tests.Features.Profile.GetProfile;

/// <summary>
/// Integration tests for the GET /api/profile endpoint
/// </summary>
public class GetProfileEndpointIntegrationTests : IClassFixture<SantaVibeWebApplicationFactory>
{
    private readonly HttpClient client;
    private readonly SantaVibeWebApplicationFactory factory;

    public GetProfileEndpointIntegrationTests(SantaVibeWebApplicationFactory factory)
    {
        this.factory = factory;
        client = factory.CreateClient();
    }

    /// <summary>
    /// Helper method to register and login a test user
    /// </summary>
    private async Task<(string userId, string token)> RegisterAndLoginTestUserAsync(
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
        return (loginResult!.UserId, loginResult.Token);
    }

    [Fact]
    public async Task GET_Profile_WithValidToken_Returns200WithProfile()
    {
        // Arrange
        var email = $"profile.test.{Guid.NewGuid()}@example.com";
        var password = "SecurePass123!";
        var firstName = "Jan";
        var lastName = "Kowalski";

        var (userId, token) = await RegisterAndLoginTestUserAsync(email, password, firstName, lastName);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/profile");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GetProfileResponse>();
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(email, result.Email);
        Assert.Equal(firstName, result.FirstName);
        Assert.Equal(lastName, result.LastName);
        Assert.True(result.CreatedAt <= DateTimeOffset.UtcNow);
        Assert.NotNull(result.LastLoginAt);
        Assert.True(result.LastLoginAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task GET_Profile_WithoutToken_Returns401()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/profile");
        // No Authorization header

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GET_Profile_WithInvalidToken_Returns401()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/profile");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token-123");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GET_Profile_WithExpiredToken_Returns401()
    {
        // Arrange
        // This is a token that has already expired (you can use a real expired token here)
        var expiredToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyLCJleHAiOjE1MTYyMzkwMjJ9.4Adcj0u6gM5FnwFP9rYMIBYq7Q-C5M5M5M5M5M5M5M5";

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/profile");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
