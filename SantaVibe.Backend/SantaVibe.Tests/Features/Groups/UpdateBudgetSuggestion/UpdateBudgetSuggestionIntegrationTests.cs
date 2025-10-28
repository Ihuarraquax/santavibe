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
using SantaVibe.Api.Features.Groups.UpdateBudgetSuggestion;
using SantaVibe.Tests.Infrastructure;
using Xunit;

namespace SantaVibe.Tests.Features.Groups.UpdateBudgetSuggestion;

/// <summary>
/// Integration tests for the Update Budget Suggestion endpoint (PUT /api/groups/{groupId}/participants/me/budget-suggestion)
/// </summary>
public class UpdateBudgetSuggestionIntegrationTests : IClassFixture<SantaVibeWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly SantaVibeWebApplicationFactory _factory;

    public UpdateBudgetSuggestionIntegrationTests(SantaVibeWebApplicationFactory factory)
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
    public async Task PUT_BudgetSuggestion_WithValidRequest_Returns200WithCorrectData()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser("John", "Doe");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = new Group { Name = "Test Group", OrganizerUserId = user.Id };
        var participant = new GroupParticipant { Group = group, UserId = user.Id };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.Add(participant);
            await context.SaveChangesAsync();
        }

        var request = new UpdateBudgetSuggestionRequest
        {
            BudgetSuggestion = 120.00m
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/groups/{group.Id}/participants/me/budget-suggestion", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<UpdateBudgetSuggestionResponse>();
        Assert.NotNull(result);
        Assert.Equal(group.Id, result.GroupId);
        Assert.Equal(120.00m, result.BudgetSuggestion);
        Assert.True(result.UpdatedAt <= DateTimeOffset.UtcNow);
        Assert.True(result.UpdatedAt >= DateTimeOffset.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task PUT_BudgetSuggestion_WithNullValue_ClearsBudgetSuggestion()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser("Jane", "Smith");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = new Group { Name = "Test Group", OrganizerUserId = user.Id };
        var participant = new GroupParticipant
        {
            Group = group,
            UserId = user.Id,
            BudgetSuggestion = 100.00m
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.Add(participant);
            await context.SaveChangesAsync();
        }

        var request = new UpdateBudgetSuggestionRequest
        {
            BudgetSuggestion = null
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/groups/{group.Id}/participants/me/budget-suggestion", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<UpdateBudgetSuggestionResponse>();
        Assert.NotNull(result);
        Assert.Null(result.BudgetSuggestion);
    }

    [Fact]
    public async Task PUT_BudgetSuggestion_UpdatesDatabaseCorrectly()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser("Alice", "Wonder");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = new Group { Name = "Test Group", OrganizerUserId = user.Id };
        var participant = new GroupParticipant { Group = group, UserId = user.Id };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.Add(participant);
            await context.SaveChangesAsync();
        }

        var request = new UpdateBudgetSuggestionRequest
        {
            BudgetSuggestion = 250.50m
        };

        // Act
        await _client.PutAsJsonAsync($"/api/groups/{group.Id}/participants/me/budget-suggestion", request);

        // Assert - Verify database was updated
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var updatedParticipant = await context.GroupParticipants
                .FirstOrDefaultAsync(gp => gp.GroupId == group.Id && gp.UserId == user.Id);

            Assert.NotNull(updatedParticipant);
            Assert.Equal(250.50m, updatedParticipant.BudgetSuggestion);
        }
    }

    [Fact]
    public async Task PUT_BudgetSuggestion_WithMinimumValue_Succeeds()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = new Group { Name = "Test Group", OrganizerUserId = user.Id };
        var participant = new GroupParticipant { Group = group, UserId = user.Id };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.Add(participant);
            await context.SaveChangesAsync();
        }

        var request = new UpdateBudgetSuggestionRequest
        {
            BudgetSuggestion = 0.01m
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/groups/{group.Id}/participants/me/budget-suggestion", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<UpdateBudgetSuggestionResponse>();
        Assert.NotNull(result);
        Assert.Equal(0.01m, result.BudgetSuggestion);
    }

    [Fact]
    public async Task PUT_BudgetSuggestion_WithMaximumValue_Succeeds()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = new Group { Name = "Test Group", OrganizerUserId = user.Id };
        var participant = new GroupParticipant { Group = group, UserId = user.Id };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.Add(participant);
            await context.SaveChangesAsync();
        }

        var request = new UpdateBudgetSuggestionRequest
        {
            BudgetSuggestion = 99999999.99m
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/groups/{group.Id}/participants/me/budget-suggestion", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<UpdateBudgetSuggestionResponse>();
        Assert.NotNull(result);
        Assert.Equal(99999999.99m, result.BudgetSuggestion);
    }

    [Fact]
    public async Task PUT_BudgetSuggestion_MultipleUpdates_UpdatesSuccessfully()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser("Rudolph", "Reindeer");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = new Group { Name = "Test Group", OrganizerUserId = user.Id };
        var participant = new GroupParticipant { Group = group, UserId = user.Id };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.Add(participant);
            await context.SaveChangesAsync();
        }

        // Act - Make multiple updates
        var request1 = new UpdateBudgetSuggestionRequest { BudgetSuggestion = 50.00m };
        var response1 = await _client.PutAsJsonAsync($"/api/groups/{group.Id}/participants/me/budget-suggestion", request1);
        var result1 = await response1.Content.ReadFromJsonAsync<UpdateBudgetSuggestionResponse>();

        var request2 = new UpdateBudgetSuggestionRequest { BudgetSuggestion = 100.00m };
        var response2 = await _client.PutAsJsonAsync($"/api/groups/{group.Id}/participants/me/budget-suggestion", request2);
        var result2 = await response2.Content.ReadFromJsonAsync<UpdateBudgetSuggestionResponse>();

        var request3 = new UpdateBudgetSuggestionRequest { BudgetSuggestion = 150.00m };
        var response3 = await _client.PutAsJsonAsync($"/api/groups/{group.Id}/participants/me/budget-suggestion", request3);
        var result3 = await response3.Content.ReadFromJsonAsync<UpdateBudgetSuggestionResponse>();

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotNull(result3);

        Assert.Equal(50.00m, result1.BudgetSuggestion);
        Assert.Equal(100.00m, result2.BudgetSuggestion);
        Assert.Equal(150.00m, result3.BudgetSuggestion);

        // Verify final database state
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var updatedParticipant = await context.GroupParticipants
                .FirstOrDefaultAsync(gp => gp.GroupId == group.Id && gp.UserId == user.Id);

            Assert.NotNull(updatedParticipant);
            Assert.Equal(150.00m, updatedParticipant.BudgetSuggestion);
        }
    }

    #endregion

    #region Authentication and Authorization Tests

    [Fact]
    public async Task PUT_BudgetSuggestion_WithoutAuthentication_Returns401()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var request = new UpdateBudgetSuggestionRequest
        {
            BudgetSuggestion = 100.00m
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/groups/{groupId}/participants/me/budget-suggestion", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PUT_BudgetSuggestion_WithInvalidToken_Returns401()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

        var groupId = Guid.NewGuid();
        var request = new UpdateBudgetSuggestionRequest
        {
            BudgetSuggestion = 100.00m
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/groups/{groupId}/participants/me/budget-suggestion", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PUT_BudgetSuggestion_AsNonParticipant_Returns403()
    {
        // Arrange
        var (organizer, _) = await CreateAndAuthenticateUser("Organizer", "User");
        var (nonParticipant, nonParticipantToken) = await CreateAndAuthenticateUser("NonParticipant", "User");

        var group = new Group { Name = "Test Group", OrganizerUserId = organizer.Id };
        var participant = new GroupParticipant { Group = group, UserId = organizer.Id };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.Add(participant);
            await context.SaveChangesAsync();
        }

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", nonParticipantToken);

        var request = new UpdateBudgetSuggestionRequest
        {
            BudgetSuggestion = 100.00m
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/groups/{group.Id}/participants/me/budget-suggestion", request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var problemDetails = await response.Content.ReadAsStringAsync();
        Assert.Contains("NotParticipant", problemDetails);
        Assert.Contains("not a participant", problemDetails);
    }

    [Fact]
    public async Task PUT_BudgetSuggestion_AsParticipantNotOrganizer_UpdatesSuccessfully()
    {
        // Arrange
        var (organizer, _) = await CreateAndAuthenticateUser("Organizer", "User");
        var (participant, participantToken) = await CreateAndAuthenticateUser("Regular", "Participant");

        var group = new Group { Name = "Test Group", OrganizerUserId = organizer.Id };
        var organizerParticipant = new GroupParticipant { Group = group, UserId = organizer.Id };
        var regularParticipant = new GroupParticipant { Group = group, UserId = participant.Id };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.AddRange(organizerParticipant, regularParticipant);
            await context.SaveChangesAsync();
        }

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", participantToken);

        var request = new UpdateBudgetSuggestionRequest
        {
            BudgetSuggestion = 75.00m
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/groups/{group.Id}/participants/me/budget-suggestion", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<UpdateBudgetSuggestionResponse>();
        Assert.NotNull(result);
        Assert.Equal(75.00m, result.BudgetSuggestion);
    }

    #endregion

    #region Error Tests

    [Fact]
    public async Task PUT_BudgetSuggestion_ForNonExistentGroup_Returns404()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var nonExistentGroupId = Guid.NewGuid();
        var request = new UpdateBudgetSuggestionRequest
        {
            BudgetSuggestion = 100.00m
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/groups/{nonExistentGroupId}/participants/me/budget-suggestion", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problemDetails = await response.Content.ReadAsStringAsync();
        Assert.Contains("GroupNotFound", problemDetails);
        Assert.Contains("Group not found", problemDetails);
    }

    [Fact]
    public async Task PUT_BudgetSuggestion_BelowMinimum_Returns400()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = new Group { Name = "Test Group", OrganizerUserId = user.Id };
        var participant = new GroupParticipant { Group = group, UserId = user.Id };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.Add(participant);
            await context.SaveChangesAsync();
        }

        var request = new UpdateBudgetSuggestionRequest
        {
            BudgetSuggestion = 0.00m // Below minimum of 0.01
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/groups/{group.Id}/participants/me/budget-suggestion", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problemDetails = await response.Content.ReadAsStringAsync();
        Assert.Contains("Budget suggestion must be between", problemDetails);
    }

    [Fact]
    public async Task PUT_BudgetSuggestion_AboveMaximum_Returns400()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = new Group { Name = "Test Group", OrganizerUserId = user.Id };
        var participant = new GroupParticipant { Group = group, UserId = user.Id };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.Add(participant);
            await context.SaveChangesAsync();
        }

        var request = new UpdateBudgetSuggestionRequest
        {
            BudgetSuggestion = 100000000.00m // Above maximum of 99999999.99
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/groups/{group.Id}/participants/me/budget-suggestion", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problemDetails = await response.Content.ReadAsStringAsync();
        Assert.Contains("Budget suggestion must be between", problemDetails);
    }

    [Fact]
    public async Task PUT_BudgetSuggestion_NegativeValue_Returns400()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = new Group { Name = "Test Group", OrganizerUserId = user.Id };
        var participant = new GroupParticipant { Group = group, UserId = user.Id };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.Add(participant);
            await context.SaveChangesAsync();
        }

        var request = new UpdateBudgetSuggestionRequest
        {
            BudgetSuggestion = -50.00m
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/groups/{group.Id}/participants/me/budget-suggestion", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problemDetails = await response.Content.ReadAsStringAsync();
        Assert.Contains("Budget suggestion must be between", problemDetails);
    }

    #endregion

    #region Business Logic Tests

    [Fact]
    public async Task PUT_BudgetSuggestion_WhenDrawCompleted_Returns400()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser("Santa", "Claus");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = new Group
        {
            Name = "Test Group",
            OrganizerUserId = user.Id,
            DrawCompletedAt = DateTimeOffset.UtcNow.AddHours(-1)
        };
        var participant = new GroupParticipant { Group = group, UserId = user.Id };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.Add(participant);
            await context.SaveChangesAsync();
        }

        var request = new UpdateBudgetSuggestionRequest
        {
            BudgetSuggestion = 100.00m
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/groups/{group.Id}/participants/me/budget-suggestion", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problemDetails = await response.Content.ReadAsStringAsync();
        Assert.Contains("DrawAlreadyCompleted", problemDetails);
        Assert.Contains("Cannot modify budget suggestion after draw has been completed", problemDetails);
    }

    [Fact]
    public async Task PUT_BudgetSuggestion_WhenDrawNotCompleted_UpdatesSuccessfully()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser("Elf", "Helper");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = new Group
        {
            Name = "Test Group",
            OrganizerUserId = user.Id,
            DrawCompletedAt = null // Draw not completed
        };
        var participant = new GroupParticipant { Group = group, UserId = user.Id };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.Add(participant);
            await context.SaveChangesAsync();
        }

        var request = new UpdateBudgetSuggestionRequest
        {
            BudgetSuggestion = 200.00m
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/groups/{group.Id}/participants/me/budget-suggestion", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<UpdateBudgetSuggestionResponse>();
        Assert.NotNull(result);
        Assert.Equal(200.00m, result.BudgetSuggestion);
    }

    [Fact]
    public async Task PUT_BudgetSuggestion_UpdateExistingSuggestion_ReplacesValue()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser("Bob", "Builder");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = new Group { Name = "Test Group", OrganizerUserId = user.Id };
        var participant = new GroupParticipant
        {
            Group = group,
            UserId = user.Id,
            BudgetSuggestion = 50.00m // Existing suggestion
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.Add(participant);
            await context.SaveChangesAsync();
        }

        var request = new UpdateBudgetSuggestionRequest
        {
            BudgetSuggestion = 150.00m // New suggestion
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/groups/{group.Id}/participants/me/budget-suggestion", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<UpdateBudgetSuggestionResponse>();
        Assert.NotNull(result);
        Assert.Equal(150.00m, result.BudgetSuggestion);

        // Verify database was updated
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var updatedParticipant = await context.GroupParticipants
                .FirstOrDefaultAsync(gp => gp.GroupId == group.Id && gp.UserId == user.Id);

            Assert.NotNull(updatedParticipant);
            Assert.Equal(150.00m, updatedParticipant.BudgetSuggestion);
        }
    }

    [Fact]
    public async Task PUT_BudgetSuggestion_DoesNotAffectOtherParticipants()
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

        var request = new UpdateBudgetSuggestionRequest
        {
            BudgetSuggestion = 200.00m
        };

        // Act
        await _client.PutAsJsonAsync($"/api/groups/{group.Id}/participants/me/budget-suggestion", request);

        // Assert - Verify only participant1's suggestion was updated
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var participants = await context.GroupParticipants
                .Where(gp => gp.GroupId == group.Id)
                .ToListAsync();

            var organizerRecord = participants.First(p => p.UserId == organizer.Id);
            var participant1UpdatedRecord = participants.First(p => p.UserId == participant1.Id);
            var participant2UpdatedRecord = participants.First(p => p.UserId == participant2.Id);

            Assert.Equal(100.00m, organizerRecord.BudgetSuggestion);
            Assert.Equal(200.00m, participant1UpdatedRecord.BudgetSuggestion);
            Assert.Equal(75.00m, participant2UpdatedRecord.BudgetSuggestion);
        }
    }

    #endregion
}
