using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SantaVibe.Api.Data;
using SantaVibe.Api.Data.Entities;
using SantaVibe.Api.Features.Authentication.Login;
using SantaVibe.Api.Features.Groups.GetMyBudgetSuggestion;
using SantaVibe.Tests.Infrastructure;
using Xunit;

namespace SantaVibe.Tests.Features.Groups.GetMyBudgetSuggestion;

/// <summary>
/// Integration tests for the Get My Budget Suggestion endpoint (GET /api/groups/{groupId}/participants/me/budget-suggestion)
/// </summary>
public class GetMyBudgetSuggestionIntegrationTests : IClassFixture<SantaVibeWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly SantaVibeWebApplicationFactory _factory;

    public GetMyBudgetSuggestionIntegrationTests(SantaVibeWebApplicationFactory factory)
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
    public async Task GET_MyBudgetSuggestion_WithBudgetSet_Returns200WithCorrectData()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser("John", "Doe");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = new Group { Name = "Test Group", OrganizerUserId = user.Id };
        var joinedAt = DateTimeOffset.UtcNow.AddDays(-2);
        var participant = new GroupParticipant
        {
            Group = group,
            UserId = user.Id,
            BudgetSuggestion = 120.00m,
            JoinedAt = joinedAt
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.Add(participant);
            await context.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}/participants/me/budget-suggestion");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GetMyBudgetSuggestionResponse>();
        Assert.NotNull(result);
        Assert.Equal(group.Id, result.GroupId);
        Assert.Equal(120.00m, result.BudgetSuggestion);
        Assert.NotNull(result.SubmittedAt);
        // Check timestamp is within a reasonable range (database precision may vary)
        Assert.True(Math.Abs((result.SubmittedAt.Value - joinedAt).TotalMilliseconds) < 1,
            $"Expected SubmittedAt to be {joinedAt}, but was {result.SubmittedAt}");
    }

    [Fact]
    public async Task GET_MyBudgetSuggestion_WithoutBudgetSet_Returns200WithNullValues()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser("Jane", "Smith");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = new Group { Name = "Test Group", OrganizerUserId = user.Id };
        var participant = new GroupParticipant
        {
            Group = group,
            UserId = user.Id,
            BudgetSuggestion = null
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.Add(participant);
            await context.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}/participants/me/budget-suggestion");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GetMyBudgetSuggestionResponse>();
        Assert.NotNull(result);
        Assert.Equal(group.Id, result.GroupId);
        Assert.Null(result.BudgetSuggestion);
        Assert.Null(result.SubmittedAt);
    }

    [Fact]
    public async Task GET_MyBudgetSuggestion_AfterDrawCompleted_Returns200()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser("Alice", "Wonder");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = new Group
        {
            Name = "Test Group",
            OrganizerUserId = user.Id,
            DrawCompletedAt = DateTimeOffset.UtcNow.AddHours(-1)
        };
        var participant = new GroupParticipant
        {
            Group = group,
            UserId = user.Id,
            BudgetSuggestion = 250.50m
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.Add(participant);
            await context.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}/participants/me/budget-suggestion");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GetMyBudgetSuggestionResponse>();
        Assert.NotNull(result);
        Assert.Equal(250.50m, result.BudgetSuggestion);
    }

    [Fact]
    public async Task GET_MyBudgetSuggestion_BeforeDrawCompleted_Returns200()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser("Bob", "Builder");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = new Group
        {
            Name = "Test Group",
            OrganizerUserId = user.Id,
            DrawCompletedAt = null
        };
        var participant = new GroupParticipant
        {
            Group = group,
            UserId = user.Id,
            BudgetSuggestion = 80.00m
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.Add(participant);
            await context.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}/participants/me/budget-suggestion");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GetMyBudgetSuggestionResponse>();
        Assert.NotNull(result);
        Assert.Equal(80.00m, result.BudgetSuggestion);
    }

    #endregion

    #region Authentication and Authorization Tests

    [Fact]
    public async Task GET_MyBudgetSuggestion_WithoutAuthentication_Returns401()
    {
        // Arrange
        var groupId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/groups/{groupId}/participants/me/budget-suggestion");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GET_MyBudgetSuggestion_WithInvalidToken_Returns401()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
        var groupId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/groups/{groupId}/participants/me/budget-suggestion");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GET_MyBudgetSuggestion_AsNonParticipant_Returns403()
    {
        // Arrange
        var (organizer, _) = await CreateAndAuthenticateUser("Organizer", "User");
        var (nonParticipant, nonParticipantToken) = await CreateAndAuthenticateUser("NonParticipant", "User");

        var group = new Group { Name = "Test Group", OrganizerUserId = organizer.Id };
        var participant = new GroupParticipant
        {
            Group = group,
            UserId = organizer.Id,
            BudgetSuggestion = 100.00m
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.Add(participant);
            await context.SaveChangesAsync();
        }

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", nonParticipantToken);

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}/participants/me/budget-suggestion");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var problemDetails = await response.Content.ReadAsStringAsync();
        Assert.Contains("NotParticipant", problemDetails);
        Assert.Contains("not a participant", problemDetails);
    }

    [Fact]
    public async Task GET_MyBudgetSuggestion_AsParticipantNotOrganizer_ReturnsOwnBudget()
    {
        // Arrange
        var (organizer, _) = await CreateAndAuthenticateUser("Organizer", "User");
        var (participant, participantToken) = await CreateAndAuthenticateUser("Regular", "Participant");

        var group = new Group { Name = "Test Group", OrganizerUserId = organizer.Id };
        var organizerParticipant = new GroupParticipant
        {
            Group = group,
            UserId = organizer.Id,
            BudgetSuggestion = 150.00m
        };
        var regularParticipant = new GroupParticipant
        {
            Group = group,
            UserId = participant.Id,
            BudgetSuggestion = 75.00m
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.AddRange(organizerParticipant, regularParticipant);
            await context.SaveChangesAsync();
        }

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", participantToken);

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}/participants/me/budget-suggestion");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GetMyBudgetSuggestionResponse>();
        Assert.NotNull(result);
        Assert.Equal(75.00m, result.BudgetSuggestion); // Should get their own budget, not organizer's
    }

    #endregion

    #region Error Tests

    [Fact]
    public async Task GET_MyBudgetSuggestion_ForNonExistentGroup_Returns404()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var nonExistentGroupId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/groups/{nonExistentGroupId}/participants/me/budget-suggestion");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problemDetails = await response.Content.ReadAsStringAsync();
        Assert.Contains("GroupNotFound", problemDetails);
        Assert.Contains("Group not found", problemDetails);
    }

    [Fact]
    public async Task GET_MyBudgetSuggestion_OnlyReturnsOwnBudget_NotOthers()
    {
        // Arrange
        var (organizer, _) = await CreateAndAuthenticateUser("Organizer", "User");
        var (participant1, participant1Token) = await CreateAndAuthenticateUser("Participant", "One");
        var (participant2, _) = await CreateAndAuthenticateUser("Participant", "Two");

        var group = new Group { Name = "Test Group", OrganizerUserId = organizer.Id };
        var organizerParticipant = new GroupParticipant
        {
            Group = group,
            UserId = organizer.Id,
            BudgetSuggestion = 100.00m
        };
        var participant1Record = new GroupParticipant
        {
            Group = group,
            UserId = participant1.Id,
            BudgetSuggestion = 50.00m
        };
        var participant2Record = new GroupParticipant
        {
            Group = group,
            UserId = participant2.Id,
            BudgetSuggestion = 75.00m
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.AddRange(organizerParticipant, participant1Record, participant2Record);
            await context.SaveChangesAsync();
        }

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", participant1Token);

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}/participants/me/budget-suggestion");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GetMyBudgetSuggestionResponse>();
        Assert.NotNull(result);
        Assert.Equal(50.00m, result.BudgetSuggestion); // Should only see their own budget
        Assert.NotEqual(100.00m, result.BudgetSuggestion);
        Assert.NotEqual(75.00m, result.BudgetSuggestion);
    }

    #endregion
}
