using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SantaVibe.Api.Data;
using SantaVibe.Api.Data.Entities;
using SantaVibe.Api.Features.Authentication.Login;
using SantaVibe.Api.Features.Groups.Create;
using SantaVibe.Tests.Infrastructure;
using Xunit;

namespace SantaVibe.Tests.Features.Groups.Create;

/// <summary>
/// Integration tests for the Create Group endpoint (POST /api/groups)
/// </summary>
public class CreateGroupIntegrationTests : IClassFixture<SantaVibeWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly SantaVibeWebApplicationFactory _factory;

    public CreateGroupIntegrationTests(SantaVibeWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(ApplicationUser user, string token)> CreateAndAuthenticateUser(
        string firstName = "Test",
        string lastName = "User")
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var email = $"user.{Guid.NewGuid()}@example.com";
        var user = new ApplicationUser
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            UserName = email
        };

        var result = await userManager.CreateAsync(user, "Password123!");
        if (!result.Succeeded)
        {
            throw new Exception($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        var loginRequest = new LoginRequest { Email = user.Email, Password = "Password123!" };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        loginResponse.EnsureSuccessStatusCode();
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        return (user, loginResult!.Token);
    }

    #region Happy Path Tests

    [Fact]
    public async Task POST_Groups_WithValidRequest_Returns201WithCorrectData()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser("John", "Doe");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateGroupRequest
        {
            Name = "Family Secret Santa 2025"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/groups", request);

        // Diagnostic: Print response content if not successful
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Expected 201 Created but got {response.StatusCode}. Response: {errorContent}");
        }

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<CreateGroupResponse>();
        Assert.NotNull(result);

        // Verify response fields
        Assert.NotEqual(Guid.Empty, result.GroupId);
        Assert.Equal("Family Secret Santa 2025", result.Name);
        Assert.Equal(user.Id, result.OrganizerId);
        Assert.Equal("John Doe", result.OrganizerName);
        Assert.NotEqual(Guid.Empty, result.InvitationToken);
        Assert.Contains(result.InvitationToken.ToString(), result.InvitationLink);
        Assert.Equal(1, result.ParticipantCount);
        Assert.Null(result.Budget);
        Assert.False(result.DrawCompleted);
        Assert.True(result.CreatedAt <= DateTimeOffset.UtcNow);
        Assert.True(result.CreatedAt >= DateTimeOffset.UtcNow.AddMinutes(-1));

        // Verify Location header
        Assert.NotNull(response.Headers.Location);
        Assert.Equal($"/api/groups/{result.GroupId}", response.Headers.Location.ToString());
    }

    [Fact]
    public async Task POST_Groups_WithValidRequest_CreatesGroupInDatabase()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser("Jane", "Smith");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateGroupRequest
        {
            Name = "Office Secret Santa"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/groups", request);
        var result = await response.Content.ReadFromJsonAsync<CreateGroupResponse>();

        // Assert - Verify group exists in database
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var group = await context.Groups.FindAsync(result!.GroupId);
        Assert.NotNull(group);
        Assert.Equal("Office Secret Santa", group.Name);
        Assert.Equal(user.Id, group.OrganizerUserId);
        Assert.Null(group.Budget);
        Assert.Null(group.DrawCompletedAt);
    }

    [Fact]
    public async Task POST_Groups_WithValidRequest_AddsOrganizerAsParticipant()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser("Bob", "Johnson");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateGroupRequest
        {
            Name = "Friends Secret Santa"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/groups", request);
        var result = await response.Content.ReadFromJsonAsync<CreateGroupResponse>();

        // Assert - Verify participant exists in database
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var participant = context.GroupParticipants
            .FirstOrDefault(gp => gp.GroupId == result!.GroupId && gp.UserId == user.Id);

        Assert.NotNull(participant);
        Assert.Null(participant.BudgetSuggestion);
        Assert.Null(participant.WishlistContent);
        Assert.True(participant.JoinedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task POST_Groups_WithTrimmedName_StoresNameCorrectly()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateGroupRequest
        {
            Name = "  Test Group With Spaces  "
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/groups", request);
        var result = await response.Content.ReadFromJsonAsync<CreateGroupResponse>();

        // Assert
        Assert.Equal("Test Group With Spaces", result!.Name);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var group = await context.Groups.FindAsync(result.GroupId);

        Assert.Equal("Test Group With Spaces", group!.Name);
    }

    #endregion

    #region Validation Error Tests

    [Fact]
    public async Task POST_Groups_WithMissingName_Returns400()
    {
        // Arrange
        var (_, token) = await CreateAndAuthenticateUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new { }; // Missing name field

        // Act
        var response = await _client.PostAsJsonAsync("/api/groups", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task POST_Groups_WithEmptyName_Returns400()
    {
        // Arrange
        var (_, token) = await CreateAndAuthenticateUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateGroupRequest
        {
            Name = ""
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/groups", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problemDetails = await response.Content.ReadAsStringAsync();
        Assert.Contains("Group name", problemDetails);
    }

    [Fact]
    public async Task POST_Groups_WithWhitespaceName_Returns400()
    {
        // Arrange
        var (_, token) = await CreateAndAuthenticateUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateGroupRequest
        {
            Name = "   "
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/groups", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problemDetails = await response.Content.ReadAsStringAsync();
        // Check that error is about the name field (whitespace or required validation)
        Assert.Contains("name", problemDetails.ToLower());
    }

    [Fact]
    public async Task POST_Groups_WithTooShortName_Returns400()
    {
        // Arrange
        var (_, token) = await CreateAndAuthenticateUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateGroupRequest
        {
            Name = "AB" // Only 2 characters
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/groups", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problemDetails = await response.Content.ReadAsStringAsync();
        Assert.Contains("at least 3 characters", problemDetails);
    }

    [Fact]
    public async Task POST_Groups_WithTooLongName_Returns400()
    {
        // Arrange
        var (_, token) = await CreateAndAuthenticateUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateGroupRequest
        {
            Name = new string('A', 201) // 201 characters
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/groups", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problemDetails = await response.Content.ReadAsStringAsync();
        Assert.Contains("cannot exceed 200 characters", problemDetails);
    }

    [Fact]
    public async Task POST_Groups_WithExactly3Characters_Returns201()
    {
        // Arrange
        var (_, token) = await CreateAndAuthenticateUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateGroupRequest
        {
            Name = "ABC" // Exactly 3 characters
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/groups", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task POST_Groups_WithExactly200Characters_Returns201()
    {
        // Arrange
        var (_, token) = await CreateAndAuthenticateUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateGroupRequest
        {
            Name = new string('A', 200) // Exactly 200 characters
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/groups", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    #endregion

    #region Authentication Error Tests

    [Fact]
    public async Task POST_Groups_WithoutAuthentication_Returns401()
    {
        // Arrange
        var request = new CreateGroupRequest
        {
            Name = "Test Group"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/groups", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task POST_Groups_WithInvalidToken_Returns401()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

        var request = new CreateGroupRequest
        {
            Name = "Test Group"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/groups", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Business Logic Tests

    [Fact]
    public async Task POST_Groups_GeneratesUniqueInvitationTokens()
    {
        // Arrange
        var (_, token) = await CreateAndAuthenticateUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - Create multiple groups
        var response1 = await _client.PostAsJsonAsync("/api/groups", new CreateGroupRequest { Name = "Group 1" });
        var response2 = await _client.PostAsJsonAsync("/api/groups", new CreateGroupRequest { Name = "Group 2" });
        var response3 = await _client.PostAsJsonAsync("/api/groups", new CreateGroupRequest { Name = "Group 3" });

        var result1 = await response1.Content.ReadFromJsonAsync<CreateGroupResponse>();
        var result2 = await response2.Content.ReadFromJsonAsync<CreateGroupResponse>();
        var result3 = await response3.Content.ReadFromJsonAsync<CreateGroupResponse>();

        // Assert - All invitation tokens are unique
        Assert.NotEqual(result1!.InvitationToken, result2!.InvitationToken);
        Assert.NotEqual(result1.InvitationToken, result3!.InvitationToken);
        Assert.NotEqual(result2.InvitationToken, result3.InvitationToken);
    }

    [Fact]
    public async Task POST_Groups_GeneratesUniqueGroupIds()
    {
        // Arrange
        var (_, token) = await CreateAndAuthenticateUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - Create multiple groups
        var response1 = await _client.PostAsJsonAsync("/api/groups", new CreateGroupRequest { Name = "Group 1" });
        var response2 = await _client.PostAsJsonAsync("/api/groups", new CreateGroupRequest { Name = "Group 2" });

        var result1 = await response1.Content.ReadFromJsonAsync<CreateGroupResponse>();
        var result2 = await response2.Content.ReadFromJsonAsync<CreateGroupResponse>();

        // Assert - All group IDs are unique
        Assert.NotEqual(result1!.GroupId, result2!.GroupId);
    }

    [Fact]
    public async Task POST_Groups_AllowsMultipleGroupsForSameUser()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser("Alice", "Wonder");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - Create 3 groups with the same user
        var response1 = await _client.PostAsJsonAsync("/api/groups", new CreateGroupRequest { Name = "Group 1" });
        var response2 = await _client.PostAsJsonAsync("/api/groups", new CreateGroupRequest { Name = "Group 2" });
        var response3 = await _client.PostAsJsonAsync("/api/groups", new CreateGroupRequest { Name = "Group 3" });

        // Assert - All succeed
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);
        Assert.Equal(HttpStatusCode.Created, response2.StatusCode);
        Assert.Equal(HttpStatusCode.Created, response3.StatusCode);

        // Verify all groups exist in database
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var userGroups = context.Groups
            .Where(g => g.OrganizerUserId == user.Id)
            .ToList();

        Assert.Equal(3, userGroups.Count);
    }

    #endregion
}
