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
using SantaVibe.Tests.Infrastructure;
using Xunit;

namespace SantaVibe.Tests.Features.ExclusionRules.DeleteExclusionRule;

/// <summary>
/// Integration tests for DELETE /api/groups/{groupId}/exclusion-rules/{ruleId} endpoint
/// </summary>
public class DeleteExclusionRuleIntegrationTests : IClassFixture<SantaVibeWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly SantaVibeWebApplicationFactory _factory;

    public DeleteExclusionRuleIntegrationTests(SantaVibeWebApplicationFactory factory)
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
            CreatedAt = DateTimeOffset.UtcNow
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

    private async Task<bool> ExclusionRuleExists(Guid ruleId)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await context.ExclusionRules.AnyAsync(er => er.Id == ruleId);
    }

    #endregion

    #region Happy Path Tests

    [Fact]
    public async Task DELETE_ExclusionRule_AsOrganizer_Returns204()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("John", "Organizer");
        var group = await CreateGroupWithParticipants(organizer.Id, 3);
        var participantIds = await GetParticipantIds(group.Id);
        var rule = await CreateExclusionRule(group.Id, participantIds[0], participantIds[1], organizer.Id);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.DeleteAsync($"/api/groups/{group.Id}/exclusion-rules/{rule.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DELETE_ExclusionRule_RemovesFromDatabase()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("Alice", "Organizer");
        var group = await CreateGroupWithParticipants(organizer.Id, 3);
        var participantIds = await GetParticipantIds(group.Id);
        var rule = await CreateExclusionRule(group.Id, participantIds[0], participantIds[1], organizer.Id);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Verify rule exists before deletion
        Assert.True(await ExclusionRuleExists(rule.Id));

        // Act
        var response = await _client.DeleteAsync($"/api/groups/{group.Id}/exclusion-rules/{rule.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify rule no longer exists
        Assert.False(await ExclusionRuleExists(rule.Id));
    }

    [Fact]
    public async Task DELETE_ExclusionRule_DoesNotAffectOtherRules()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("Bob", "Organizer");
        var group = await CreateGroupWithParticipants(organizer.Id, 5);
        var participantIds = await GetParticipantIds(group.Id);

        // Create 3 rules
        var rule1 = await CreateExclusionRule(group.Id, participantIds[0], participantIds[1], organizer.Id);
        var rule2 = await CreateExclusionRule(group.Id, participantIds[2], participantIds[3], organizer.Id);
        var rule3 = await CreateExclusionRule(group.Id, participantIds[0], participantIds[4], organizer.Id);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - Delete middle rule
        var response = await _client.DeleteAsync($"/api/groups/{group.Id}/exclusion-rules/{rule2.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify only rule2 was deleted
        Assert.True(await ExclusionRuleExists(rule1.Id));
        Assert.False(await ExclusionRuleExists(rule2.Id));
        Assert.True(await ExclusionRuleExists(rule3.Id));
    }

    [Fact]
    public async Task DELETE_ExclusionRule_MultipleSequentialDeletes_AllSucceed()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser();
        var group = await CreateGroupWithParticipants(organizer.Id, 6);
        var participantIds = await GetParticipantIds(group.Id);

        var rule1 = await CreateExclusionRule(group.Id, participantIds[0], participantIds[1], organizer.Id);
        var rule2 = await CreateExclusionRule(group.Id, participantIds[2], participantIds[3], organizer.Id);
        var rule3 = await CreateExclusionRule(group.Id, participantIds[4], participantIds[5], organizer.Id);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - Delete all rules sequentially
        var response1 = await _client.DeleteAsync($"/api/groups/{group.Id}/exclusion-rules/{rule1.Id}");
        var response2 = await _client.DeleteAsync($"/api/groups/{group.Id}/exclusion-rules/{rule2.Id}");
        var response3 = await _client.DeleteAsync($"/api/groups/{group.Id}/exclusion-rules/{rule3.Id}");

        // Assert - All deletions succeed
        Assert.Equal(HttpStatusCode.NoContent, response1.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, response2.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, response3.StatusCode);

        // Verify all rules are deleted
        Assert.False(await ExclusionRuleExists(rule1.Id));
        Assert.False(await ExclusionRuleExists(rule2.Id));
        Assert.False(await ExclusionRuleExists(rule3.Id));
    }

    #endregion

    #region Authorization Tests

    [Fact]
    public async Task DELETE_ExclusionRule_WithoutAuthentication_Returns401()
    {
        // Arrange
        var (organizer, _) = await CreateAndAuthenticateUser();
        var group = await CreateGroupWithParticipants(organizer.Id, 3);
        var participantIds = await GetParticipantIds(group.Id);
        var rule = await CreateExclusionRule(group.Id, participantIds[0], participantIds[1], organizer.Id);

        // Act - No authorization header
        var response = await _client.DeleteAsync($"/api/groups/{group.Id}/exclusion-rules/{rule.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        // Verify rule still exists
        Assert.True(await ExclusionRuleExists(rule.Id));
    }

    [Fact]
    public async Task DELETE_ExclusionRule_WithInvalidToken_Returns401()
    {
        // Arrange
        var (organizer, _) = await CreateAndAuthenticateUser();
        var group = await CreateGroupWithParticipants(organizer.Id, 3);
        var participantIds = await GetParticipantIds(group.Id);
        var rule = await CreateExclusionRule(group.Id, participantIds[0], participantIds[1], organizer.Id);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

        // Act
        var response = await _client.DeleteAsync($"/api/groups/{group.Id}/exclusion-rules/{rule.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DELETE_ExclusionRule_AsNonOrganizer_Returns403()
    {
        // Arrange
        var (organizer, _) = await CreateAndAuthenticateUser("Organizer", "User");
        var (otherUser, otherToken) = await CreateAndAuthenticateUser("Other", "User");
        var group = await CreateGroupWithParticipants(organizer.Id, 3);
        var participantIds = await GetParticipantIds(group.Id);
        var rule = await CreateExclusionRule(group.Id, participantIds[0], participantIds[1], organizer.Id);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);

        // Act
        var response = await _client.DeleteAsync($"/api/groups/{group.Id}/exclusion-rules/{rule.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        // Verify rule still exists
        Assert.True(await ExclusionRuleExists(rule.Id));
    }

    [Fact]
    public async Task DELETE_ExclusionRule_AsParticipantButNotOrganizer_Returns403()
    {
        // Arrange
        var (organizer, _) = await CreateAndAuthenticateUser("Organizer", "User");
        var group = await CreateGroupWithParticipants(organizer.Id, 3);
        var participantIds = await GetParticipantIds(group.Id);

        // Get a participant who is not the organizer
        var participantId = participantIds.First(id => id != organizer.Id);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var participant = await userManager.FindByIdAsync(participantId);

        var loginRequest = new LoginRequest { Email = participant!.Email!, Password = "Password123!" };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        var rule = await CreateExclusionRule(group.Id, participantIds[0], participantIds[1], organizer.Id);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult!.Token);

        // Act
        var response = await _client.DeleteAsync($"/api/groups/{group.Id}/exclusion-rules/{rule.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region Draw Completed Tests

    [Fact]
    public async Task DELETE_ExclusionRule_AfterDrawCompleted_Returns400()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser();
        var group = await CreateGroupWithParticipants(organizer.Id, 3, drawCompleted: true);
        var participantIds = await GetParticipantIds(group.Id);
        var rule = await CreateExclusionRule(group.Id, participantIds[0], participantIds[1], organizer.Id);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.DeleteAsync($"/api/groups/{group.Id}/exclusion-rules/{rule.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("draw", content.ToLower());
        Assert.Contains("completed", content.ToLower());

        // Verify rule still exists
        Assert.True(await ExclusionRuleExists(rule.Id));
    }

    #endregion

    #region Error Tests

    [Fact]
    public async Task DELETE_ExclusionRule_ForNonExistentGroup_Returns404()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser();
        var nonExistentGroupId = Guid.NewGuid();
        var nonExistentRuleId = Guid.NewGuid();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.DeleteAsync($"/api/groups/{nonExistentGroupId}/exclusion-rules/{nonExistentRuleId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DELETE_ExclusionRule_ForNonExistentRule_Returns404()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser();
        var group = await CreateGroupWithParticipants(organizer.Id, 3);
        var nonExistentRuleId = Guid.NewGuid();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.DeleteAsync($"/api/groups/{group.Id}/exclusion-rules/{nonExistentRuleId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DELETE_ExclusionRule_ForRuleInDifferentGroup_Returns404()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser();
        var group1 = await CreateGroupWithParticipants(organizer.Id, 3);
        var group2 = await CreateGroupWithParticipants(organizer.Id, 3);

        var participantIds1 = await GetParticipantIds(group1.Id);
        var rule = await CreateExclusionRule(group1.Id, participantIds1[0], participantIds1[1], organizer.Id);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - Try to delete rule from group1 using group2's ID
        var response = await _client.DeleteAsync($"/api/groups/{group2.Id}/exclusion-rules/{rule.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        // Verify rule still exists
        Assert.True(await ExclusionRuleExists(rule.Id));
    }

    [Fact]
    public async Task DELETE_ExclusionRule_WithInvalidGroupGuid_Returns404()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser();
        var ruleId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.DeleteAsync($"/api/groups/invalid-guid/exclusion-rules/{ruleId}");

        // Assert - ASP.NET Core returns 404 when route parameter binding fails
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DELETE_ExclusionRule_WithInvalidRuleGuid_Returns404()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser();
        var group = await CreateGroupWithParticipants(organizer.Id, 3);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.DeleteAsync($"/api/groups/{group.Id}/exclusion-rules/invalid-guid");

        // Assert - ASP.NET Core returns 404 when route parameter binding fails
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DELETE_ExclusionRule_AlreadyDeleted_Returns404()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser();
        var group = await CreateGroupWithParticipants(organizer.Id, 3);
        var participantIds = await GetParticipantIds(group.Id);
        var rule = await CreateExclusionRule(group.Id, participantIds[0], participantIds[1], organizer.Id);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Delete once
        var response1 = await _client.DeleteAsync($"/api/groups/{group.Id}/exclusion-rules/{rule.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response1.StatusCode);

        // Act - Try to delete again
        var response2 = await _client.DeleteAsync($"/api/groups/{group.Id}/exclusion-rules/{rule.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response2.StatusCode);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task DELETE_ExclusionRule_LastRuleInGroup_Succeeds()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser();
        var group = await CreateGroupWithParticipants(organizer.Id, 3);
        var participantIds = await GetParticipantIds(group.Id);
        var rule = await CreateExclusionRule(group.Id, participantIds[0], participantIds[1], organizer.Id);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.DeleteAsync($"/api/groups/{group.Id}/exclusion-rules/{rule.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify group still exists but has no rules
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var groupExists = await context.Groups.AnyAsync(g => g.Id == group.Id);
        var ruleCount = await context.ExclusionRules.CountAsync(er => er.GroupId == group.Id);

        Assert.True(groupExists);
        Assert.Equal(0, ruleCount);
    }

    [Fact]
    public async Task DELETE_ExclusionRule_DoesNotAffectGroupParticipants()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser();
        var group = await CreateGroupWithParticipants(organizer.Id, 3);
        var participantIds = await GetParticipantIds(group.Id);
        var rule = await CreateExclusionRule(group.Id, participantIds[0], participantIds[1], organizer.Id);

        var participantCountBefore = participantIds.Count;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.DeleteAsync($"/api/groups/{group.Id}/exclusion-rules/{rule.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify participants unchanged
        var participantCountAfter = (await GetParticipantIds(group.Id)).Count;
        Assert.Equal(participantCountBefore, participantCountAfter);
    }

    #endregion
}
