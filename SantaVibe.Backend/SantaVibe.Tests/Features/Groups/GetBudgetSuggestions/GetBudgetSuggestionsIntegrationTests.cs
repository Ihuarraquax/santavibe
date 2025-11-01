using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SantaVibe.Api.Data;
using SantaVibe.Api.Data.Entities;
using SantaVibe.Api.Features.Authentication.Login;
using SantaVibe.Api.Features.Groups.GetBudgetSuggestions;
using SantaVibe.Tests.Infrastructure;

namespace SantaVibe.Tests.Features.Groups.GetBudgetSuggestions;

/// <summary>
/// Integration tests for GET /api/groups/{groupId}/budget/suggestions endpoint
/// Tests authentication, authorization, data privacy, sorting, and response structure
/// </summary>
public class GetBudgetSuggestionsIntegrationTests : IClassFixture<SantaVibeWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly SantaVibeWebApplicationFactory _factory;

    public GetBudgetSuggestionsIntegrationTests(SantaVibeWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    /// <summary>
    /// Helper method to create and authenticate a user for testing
    /// </summary>
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

    #region Authentication Tests

    [Fact]
    public async Task GET_BudgetSuggestions_WhenUnauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        var groupId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/groups/{groupId}/budget/suggestions");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Authorization Tests

    [Fact]
    public async Task GET_BudgetSuggestions_WhenUserIsNotOrganizer_ReturnsForbidden()
    {
        // Arrange
        var (organizer, _) = await CreateAndAuthenticateUser("Organizer", "User");
        var (participant, participantToken) = await CreateAndAuthenticateUser("Participant", "User");

        var group = new Group
        {
            Name = "Test Group",
            OrganizerUserId = organizer.Id
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.AddRange(
                new GroupParticipant
                {
                    Group = group,
                    UserId = organizer.Id,
                    BudgetSuggestion = 100.00m
                },
                new GroupParticipant
                {
                    Group = group,
                    UserId = participant.Id,
                    BudgetSuggestion = 80.00m
                }
            );
            await context.SaveChangesAsync();
        }

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", participantToken);

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}/budget/suggestions");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GET_BudgetSuggestions_WhenGroupNotFound_ReturnsNotFound()
    {
        // Arrange
        var (_, token) = await CreateAndAuthenticateUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var nonExistentGroupId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/groups/{nonExistentGroupId}/budget/suggestions");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task GET_BudgetSuggestions_AsOrganizer_ReturnsAnonymousSortedSuggestions()
    {
        // Arrange
        var (organizer, organizerToken) = await CreateAndAuthenticateUser("Jan", "Kowalski");
        var (participant1, _) = await CreateAndAuthenticateUser("Anna", "Nowak");
        var (participant2, _) = await CreateAndAuthenticateUser("Piotr", "Wiśniewski");
        var (participant3, _) = await CreateAndAuthenticateUser("Maria", "Lewandowska");

        var group = new Group
        {
            Name = "Family Secret Santa 2025",
            OrganizerUserId = organizer.Id,
            Budget = null // No budget set yet
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.AddRange(
                new GroupParticipant
                {
                    Group = group,
                    UserId = organizer.Id,
                    BudgetSuggestion = 100.00m
                },
                new GroupParticipant
                {
                    Group = group,
                    UserId = participant1.Id,
                    BudgetSuggestion = 75.00m
                },
                new GroupParticipant
                {
                    Group = group,
                    UserId = participant2.Id,
                    BudgetSuggestion = 100.00m
                },
                new GroupParticipant
                {
                    Group = group,
                    UserId = participant3.Id,
                    BudgetSuggestion = 50.00m
                }
            );
            await context.SaveChangesAsync();
        }

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", organizerToken);

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}/budget/suggestions");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<BudgetSuggestionsResponse>();

        Assert.NotNull(result);
        Assert.Equal(group.Id, result.GroupId);

        // Verify suggestions are sorted in ascending order
        Assert.Equal(4, result.Suggestions.Count);
        Assert.Equal(50.00m, result.Suggestions[0]);
        Assert.Equal(75.00m, result.Suggestions[1]);
        Assert.Equal(100.00m, result.Suggestions[2]);
        Assert.Equal(100.00m, result.Suggestions[3]);

        // Verify counts
        Assert.Equal(4, result.Count);
        Assert.Equal(4, result.ParticipantCount);
        Assert.Equal(4, result.SuggestionsReceived);

        // Verify current budget is null
        Assert.Null(result.CurrentBudget);
    }

    [Fact]
    public async Task GET_BudgetSuggestions_WithSomeNullSuggestions_ReturnsOnlyNonNullSuggestions()
    {
        // Arrange
        var (organizer, organizerToken) = await CreateAndAuthenticateUser("Organizer", "User");
        var (participant1, _) = await CreateAndAuthenticateUser("Participant1", "User");
        var (participant2, _) = await CreateAndAuthenticateUser("Participant2", "User");
        var (participant3, _) = await CreateAndAuthenticateUser("Participant3", "User");
        var (participant4, _) = await CreateAndAuthenticateUser("Participant4", "User");

        var group = new Group
        {
            Name = "Test Group",
            OrganizerUserId = organizer.Id
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.AddRange(
                new GroupParticipant
                {
                    Group = group,
                    UserId = organizer.Id,
                    BudgetSuggestion = 80.00m
                },
                new GroupParticipant
                {
                    Group = group,
                    UserId = participant1.Id,
                    BudgetSuggestion = 50.00m
                },
                new GroupParticipant
                {
                    Group = group,
                    UserId = participant2.Id,
                    BudgetSuggestion = null // No suggestion
                },
                new GroupParticipant
                {
                    Group = group,
                    UserId = participant3.Id,
                    BudgetSuggestion = 100.00m
                },
                new GroupParticipant
                {
                    Group = group,
                    UserId = participant4.Id,
                    BudgetSuggestion = null // No suggestion
                }
            );
            await context.SaveChangesAsync();
        }

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", organizerToken);

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}/budget/suggestions");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<BudgetSuggestionsResponse>();

        Assert.NotNull(result);

        // Only 3 non-null suggestions should be returned
        Assert.Equal(3, result.Suggestions.Count);
        Assert.Equal(3, result.Count);
        Assert.Equal(3, result.SuggestionsReceived);

        // Total participants should still be 5
        Assert.Equal(5, result.ParticipantCount);

        // Verify suggestions are sorted
        Assert.Equal(50.00m, result.Suggestions[0]);
        Assert.Equal(80.00m, result.Suggestions[1]);
        Assert.Equal(100.00m, result.Suggestions[2]);
    }

    [Fact]
    public async Task GET_BudgetSuggestions_WithAllNullSuggestions_ReturnsEmptySuggestionsList()
    {
        // Arrange
        var (organizer, organizerToken) = await CreateAndAuthenticateUser("Organizer", "User");
        var (participant1, _) = await CreateAndAuthenticateUser("Participant1", "User");
        var (participant2, _) = await CreateAndAuthenticateUser("Participant2", "User");

        var group = new Group
        {
            Name = "Test Group",
            OrganizerUserId = organizer.Id
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.AddRange(
                new GroupParticipant
                {
                    Group = group,
                    UserId = organizer.Id,
                    BudgetSuggestion = null
                },
                new GroupParticipant
                {
                    Group = group,
                    UserId = participant1.Id,
                    BudgetSuggestion = null
                },
                new GroupParticipant
                {
                    Group = group,
                    UserId = participant2.Id,
                    BudgetSuggestion = null
                }
            );
            await context.SaveChangesAsync();
        }

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", organizerToken);

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}/budget/suggestions");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<BudgetSuggestionsResponse>();

        Assert.NotNull(result);

        // No suggestions received
        Assert.Empty(result.Suggestions);
        Assert.Equal(0, result.Count);
        Assert.Equal(0, result.SuggestionsReceived);

        // But participants exist
        Assert.Equal(3, result.ParticipantCount);
    }

    [Fact]
    public async Task GET_BudgetSuggestions_WithCurrentBudgetSet_ReturnsCurrentBudget()
    {
        // Arrange
        var (organizer, organizerToken) = await CreateAndAuthenticateUser("Organizer", "User");
        var (participant1, _) = await CreateAndAuthenticateUser("Participant1", "User");
        var (participant2, _) = await CreateAndAuthenticateUser("Participant2", "User");

        var group = new Group
        {
            Name = "Test Group",
            OrganizerUserId = organizer.Id,
            Budget = 75.00m // Budget already finalized
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.AddRange(
                new GroupParticipant
                {
                    Group = group,
                    UserId = organizer.Id,
                    BudgetSuggestion = 100.00m
                },
                new GroupParticipant
                {
                    Group = group,
                    UserId = participant1.Id,
                    BudgetSuggestion = 50.00m
                },
                new GroupParticipant
                {
                    Group = group,
                    UserId = participant2.Id,
                    BudgetSuggestion = 80.00m
                }
            );
            await context.SaveChangesAsync();
        }

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", organizerToken);

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}/budget/suggestions");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<BudgetSuggestionsResponse>();

        Assert.NotNull(result);
        Assert.Equal(75.00m, result.CurrentBudget);
    }

    [Fact]
    public async Task GET_BudgetSuggestions_VerifiesDataAnonymity_NoUserIdentifiersInResponse()
    {
        // Arrange
        var (organizer, organizerToken) = await CreateAndAuthenticateUser("Organizer", "User");
        var (participant1, _) = await CreateAndAuthenticateUser("Participant1", "User");

        var group = new Group
        {
            Name = "Test Group",
            OrganizerUserId = organizer.Id
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.AddRange(
                new GroupParticipant
                {
                    Group = group,
                    UserId = organizer.Id,
                    BudgetSuggestion = 100.00m
                },
                new GroupParticipant
                {
                    Group = group,
                    UserId = participant1.Id,
                    BudgetSuggestion = 50.00m
                }
            );
            await context.SaveChangesAsync();
        }

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", organizerToken);

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}/budget/suggestions");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<BudgetSuggestionsResponse>();

        Assert.NotNull(result);

        // Verify that suggestions array only contains decimal values
        Assert.All(result.Suggestions, suggestion =>
        {
            Assert.IsType<decimal>(suggestion);
        });

        // Verify no user IDs or names in response by checking raw JSON
        var rawJson = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(organizer.Id, rawJson);
        Assert.DoesNotContain(participant1.Id, rawJson);
        Assert.DoesNotContain(organizer.FirstName, rawJson);
        Assert.DoesNotContain(participant1.FirstName, rawJson);
    }

    [Fact]
    public async Task GET_BudgetSuggestions_WithDuplicateAmounts_RetainsAllDuplicates()
    {
        // Arrange
        var (organizer, organizerToken) = await CreateAndAuthenticateUser("Organizer", "User");
        var (participant1, _) = await CreateAndAuthenticateUser("Participant1", "User");
        var (participant2, _) = await CreateAndAuthenticateUser("Participant2", "User");
        var (participant3, _) = await CreateAndAuthenticateUser("Participant3", "User");

        var group = new Group
        {
            Name = "Test Group",
            OrganizerUserId = organizer.Id
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.AddRange(
                new GroupParticipant
                {
                    Group = group,
                    UserId = organizer.Id,
                    BudgetSuggestion = 100.00m
                },
                new GroupParticipant
                {
                    Group = group,
                    UserId = participant1.Id,
                    BudgetSuggestion = 100.00m
                },
                new GroupParticipant
                {
                    Group = group,
                    UserId = participant2.Id,
                    BudgetSuggestion = 100.00m
                },
                new GroupParticipant
                {
                    Group = group,
                    UserId = participant3.Id,
                    BudgetSuggestion = 50.00m
                }
            );
            await context.SaveChangesAsync();
        }

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", organizerToken);

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}/budget/suggestions");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<BudgetSuggestionsResponse>();

        Assert.NotNull(result);

        // All 4 suggestions should be returned, including duplicates
        Assert.Equal(4, result.Suggestions.Count);
        Assert.Equal(50.00m, result.Suggestions[0]);
        Assert.Equal(100.00m, result.Suggestions[1]);
        Assert.Equal(100.00m, result.Suggestions[2]);
        Assert.Equal(100.00m, result.Suggestions[3]);
    }

    #endregion
}
