using System;
using System.Collections.Generic;
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
using SantaVibe.Api.Features.ExclusionRules.GetExclusionRules;
using SantaVibe.Tests.Infrastructure;
using Xunit;

namespace SantaVibe.Tests.Features.ExclusionRules.GetExclusionRules;

/// <summary>
/// Integration tests for GET /api/groups/{groupId}/exclusion-rules endpoint
/// </summary>
public class GetExclusionRulesIntegrationTests : IClassFixture<SantaVibeWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly SantaVibeWebApplicationFactory _factory;

    public GetExclusionRulesIntegrationTests(SantaVibeWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    #region Test Helpers

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

    private async Task<Group> CreateGroupWithParticipants(
        string organizerId,
        int participantCount = 3,
        bool drawCompleted = false)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var group = new Group
        {
            Name = $"Test Group {Guid.NewGuid()}",
            OrganizerUserId = organizerId,
            DrawCompletedAt = drawCompleted ? DateTimeOffset.UtcNow : null,
            CreatedAt = DateTimeOffset.UtcNow
        };

        context.Groups.Add(group);
        await context.SaveChangesAsync(); // Save group first to get ID

        // Add organizer as participant
        context.GroupParticipants.Add(new GroupParticipant
        {
            GroupId = group.Id,
            UserId = organizerId,
            JoinedAt = DateTimeOffset.UtcNow
        });

        // Add additional participants
        for (int i = 1; i < participantCount; i++)
        {
            var email = $"participant{i}.{Guid.NewGuid()}@example.com";
            var participant = new ApplicationUser
            {
                FirstName = $"Participant{i}",
                LastName = "Test",
                Email = email,
                UserName = email
            };

            await userManager.CreateAsync(participant, "Password123!");

            context.GroupParticipants.Add(new GroupParticipant
            {
                GroupId = group.Id,
                UserId = participant.Id,
                JoinedAt = DateTimeOffset.UtcNow
            });
        }

        await context.SaveChangesAsync();
        return group;
    }

    private async Task<ExclusionRule> CreateExclusionRule(Guid groupId, string userId1, string userId2, string createdBy)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var rule = new ExclusionRule
        {
            GroupId = groupId,
            UserId1 = userId1,
            UserId2 = userId2,
            CreatedByUserId = createdBy,
            CreatedAt = DateTimeOffset.UtcNow,
            Id = Guid.NewGuid()
        };

        context.ExclusionRules.Add(rule);
        await context.SaveChangesAsync();
        return rule;
    }

    private async Task<List<string>> GetParticipantIds(Guid groupId)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await context.GroupParticipants
            .Where(gp => gp.GroupId == groupId)
            .Select(gp => gp.UserId)
            .ToListAsync();
    }

    #endregion

    #region Happy Path Tests

    [Fact]
    public async Task GET_ExclusionRules_AsOrganizer_Returns200WithEmptyList()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("John", "Organizer");
        var group = await CreateGroupWithParticipants(organizer.Id, 3);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}/exclusion-rules");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GetExclusionRulesResponse>();
        Assert.NotNull(result);
        Assert.Equal(group.Id, result.GroupId);
        Assert.Empty(result.ExclusionRules);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task GET_ExclusionRules_AsOrganizer_Returns200WithRules()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("Alice", "Organizer");
        var group = await CreateGroupWithParticipants(organizer.Id, 4);
        var participantIds = await GetParticipantIds(group.Id);

        // Create 2 exclusion rules
        var rule1 = await CreateExclusionRule(group.Id, participantIds[0], participantIds[1], organizer.Id);
        var rule2 = await CreateExclusionRule(group.Id, participantIds[2], participantIds[3], organizer.Id);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}/exclusion-rules");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GetExclusionRulesResponse>();
        Assert.NotNull(result);
        Assert.Equal(group.Id, result.GroupId);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.ExclusionRules.Count);

        // Verify first rule
        var returnedRule1 = result.ExclusionRules.First(r => r.RuleId == rule1.Id);
        Assert.Equal(rule1.UserId1, returnedRule1.User1.UserId);
        Assert.Equal(rule1.UserId2, returnedRule1.User2.UserId);
        Assert.NotNull(returnedRule1.User1.FirstName);
        Assert.NotNull(returnedRule1.User2.FirstName);
    }

    [Fact]
    public async Task GET_ExclusionRules_ReturnsRulesOrderedByCreatedAt()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("Bob", "Organizer");
        var group = await CreateGroupWithParticipants(organizer.Id, 6);
        var participantIds = await GetParticipantIds(group.Id);

        // Create 3 rules with slight delays
        var rule1 = await CreateExclusionRule(group.Id, participantIds[0], participantIds[1], organizer.Id);
        await Task.Delay(10);
        var rule2 = await CreateExclusionRule(group.Id, participantIds[2], participantIds[3], organizer.Id);
        await Task.Delay(10);
        var rule3 = await CreateExclusionRule(group.Id, participantIds[4], participantIds[5], organizer.Id);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}/exclusion-rules");

        // Assert
        var result = await response.Content.ReadFromJsonAsync<GetExclusionRulesResponse>();
        Assert.NotNull(result);
        Assert.Equal(3, result.ExclusionRules.Count);

        // Verify rules are ordered by creation time
        Assert.Equal(rule1.Id, result.ExclusionRules[0].RuleId);
        Assert.Equal(rule2.Id, result.ExclusionRules[1].RuleId);
        Assert.Equal(rule3.Id, result.ExclusionRules[2].RuleId);
    }

    #endregion

    #region Authorization Tests

    [Fact]
    public async Task GET_ExclusionRules_WithoutAuthentication_Returns401()
    {
        // Arrange
        var (organizer, _) = await CreateAndAuthenticateUser();
        var group = await CreateGroupWithParticipants(organizer.Id);

        // Act - No authorization header
        var response = await _client.GetAsync($"/api/groups/{group.Id}/exclusion-rules");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GET_ExclusionRules_WithInvalidToken_Returns401()
    {
        // Arrange
        var (organizer, _) = await CreateAndAuthenticateUser();
        var group = await CreateGroupWithParticipants(organizer.Id);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}/exclusion-rules");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GET_ExclusionRules_AsNonOrganizer_Returns403()
    {
        // Arrange
        var (organizer, _) = await CreateAndAuthenticateUser("Organizer", "User");
        var (otherUser, otherToken) = await CreateAndAuthenticateUser("Other", "User");
        var group = await CreateGroupWithParticipants(organizer.Id);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}/exclusion-rules");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region Error Tests

    [Fact]
    public async Task GET_ExclusionRules_ForNonExistentGroup_Returns404()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser();
        var nonExistentGroupId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync($"/api/groups/{nonExistentGroupId}/exclusion-rules");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GET_ExclusionRules_WithInvalidGuid_Returns404()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/groups/invalid-guid/exclusion-rules");

        // Assert - ASP.NET Core returns 404 when route parameter binding fails
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Data Integrity Tests

    [Fact]
    public async Task GET_ExclusionRules_ReturnsCompleteUserInformation()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("Test", "Organizer");
        var group = await CreateGroupWithParticipants(organizer.Id, 3);
        var participantIds = await GetParticipantIds(group.Id);

        await CreateExclusionRule(group.Id, participantIds[0], participantIds[1], organizer.Id);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}/exclusion-rules");

        // Assert
        var result = await response.Content.ReadFromJsonAsync<GetExclusionRulesResponse>();
        Assert.NotNull(result);
        Assert.Single(result.ExclusionRules);

        var rule = result.ExclusionRules[0];
        Assert.NotNull(rule.User1.UserId);
        Assert.NotNull(rule.User1.FirstName);
        Assert.NotNull(rule.User1.LastName);
        Assert.NotNull(rule.User2.UserId);
        Assert.NotNull(rule.User2.FirstName);
        Assert.NotNull(rule.User2.LastName);
        Assert.True(rule.CreatedAt <= DateTimeOffset.UtcNow);
    }

    #endregion
}
