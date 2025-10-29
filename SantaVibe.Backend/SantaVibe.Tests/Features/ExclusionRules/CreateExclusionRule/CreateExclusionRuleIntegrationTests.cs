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
using SantaVibe.Api.Features.ExclusionRules.CreateExclusionRule;
using SantaVibe.Tests.Infrastructure;
using Xunit;

namespace SantaVibe.Tests.Features.ExclusionRules.CreateExclusionRule;

/// <summary>
/// Integration tests for POST /api/groups/{groupId}/exclusion-rules endpoint
/// </summary>
public class CreateExclusionRuleIntegrationTests : IClassFixture<SantaVibeWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly SantaVibeWebApplicationFactory _factory;

    public CreateExclusionRuleIntegrationTests(SantaVibeWebApplicationFactory factory)
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

    private async Task<List<string>> GetParticipantIds(Guid groupId)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await context.GroupParticipants
            .Where(gp => gp.GroupId == groupId)
            .Select(gp => gp.UserId)
            .ToListAsync();
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

    #endregion

    #region Happy Path Tests

    [Fact]
    public async Task POST_ExclusionRule_WithValidData_Returns201()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("John", "Organizer");
        var group = await CreateGroupWithParticipants(organizer.Id, 3);
        var participantIds = await GetParticipantIds(group.Id);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateExclusionRuleRequest(participantIds[0], participantIds[1]);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/exclusion-rules", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<CreateExclusionRuleResponse>();
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.RuleId);
        Assert.Equal(group.Id, result.GroupId);
        Assert.Equal(participantIds[0], result.User1.UserId);
        Assert.Equal(participantIds[1], result.User2.UserId);
        Assert.True(result.DrawValidation.IsValid);
        Assert.Empty(result.DrawValidation.Errors);
        Assert.True(result.CreatedAt <= DateTimeOffset.UtcNow);

        // Verify Location header
        Assert.NotNull(response.Headers.Location);
        Assert.Contains($"/api/groups/{group.Id}/exclusion-rules/{result.RuleId}", response.Headers.Location.ToString());
    }

    [Fact]
    public async Task POST_ExclusionRule_CreatesRuleInDatabase()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("Alice", "Organizer");
        var group = await CreateGroupWithParticipants(organizer.Id, 4);
        var participantIds = await GetParticipantIds(group.Id);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateExclusionRuleRequest(participantIds[1], participantIds[2]);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/exclusion-rules", request);
        var result = await response.Content.ReadFromJsonAsync<CreateExclusionRuleResponse>();

        // Assert - Verify rule exists in database
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var rule = await context.ExclusionRules.FindAsync(result!.RuleId);
        Assert.NotNull(rule);
        Assert.Equal(group.Id, rule.GroupId);
        Assert.Equal(participantIds[1], rule.UserId1);
        Assert.Equal(participantIds[2], rule.UserId2);
        Assert.Equal(organizer.Id, rule.CreatedByUserId);
    }

    [Fact]
    public async Task POST_ExclusionRule_ReturnsUserNames()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("Bob", "Organizer");
        var group = await CreateGroupWithParticipants(organizer.Id, 3);
        var participantIds = await GetParticipantIds(group.Id);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateExclusionRuleRequest(participantIds[0], participantIds[1]);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/exclusion-rules", request);

        // Assert
        var result = await response.Content.ReadFromJsonAsync<CreateExclusionRuleResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.User1.FirstName);
        Assert.NotNull(result.User1.LastName);
        Assert.NotNull(result.User2.FirstName);
        Assert.NotNull(result.User2.LastName);
    }

    #endregion

    #region Validation Tests - Same User

    [Fact]
    public async Task POST_ExclusionRule_WithSameUser_Returns400()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser();
        var group = await CreateGroupWithParticipants(organizer.Id, 3);
        var participantIds = await GetParticipantIds(group.Id);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateExclusionRuleRequest(participantIds[0], participantIds[0]);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/exclusion-rules", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("same user", content.ToLower());
    }

    #endregion

    #region Validation Tests - Participants

    [Fact]
    public async Task POST_ExclusionRule_WithNonParticipant_Returns404()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser();
        var (nonParticipant, _) = await CreateAndAuthenticateUser("Non", "Participant");
        var group = await CreateGroupWithParticipants(organizer.Id, 3);
        var participantIds = await GetParticipantIds(group.Id);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Use non-participant user
        var request = new CreateExclusionRuleRequest(participantIds[0], nonParticipant.Id);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/exclusion-rules", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("not found", content.ToLower());
    }

    [Fact]
    public async Task POST_ExclusionRule_WithBothNonParticipants_Returns404()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser();
        var (user1, _) = await CreateAndAuthenticateUser("User1", "Test");
        var (user2, _) = await CreateAndAuthenticateUser("User2", "Test");
        var group = await CreateGroupWithParticipants(organizer.Id, 3);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateExclusionRuleRequest(user1.Id, user2.Id);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/exclusion-rules", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Duplicate Rule Tests

    [Fact]
    public async Task POST_ExclusionRule_Duplicate_Returns409()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser();
        var group = await CreateGroupWithParticipants(organizer.Id, 3);
        var participantIds = await GetParticipantIds(group.Id);

        // Create first rule
        await CreateExclusionRule(group.Id, participantIds[0], participantIds[1], organizer.Id);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Try to create duplicate
        var request = new CreateExclusionRuleRequest(participantIds[0], participantIds[1]);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/exclusion-rules", request);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("already exists", content.ToLower());
    }

    [Fact]
    public async Task POST_ExclusionRule_DuplicateBidirectional_Returns409()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser();
        var group = await CreateGroupWithParticipants(organizer.Id, 3);
        var participantIds = await GetParticipantIds(group.Id);

        // Create rule with userId1 and userId2
        await CreateExclusionRule(group.Id, participantIds[0], participantIds[1], organizer.Id);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Try to create reverse rule (userId2 and userId1)
        var request = new CreateExclusionRuleRequest(participantIds[1], participantIds[0]);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/exclusion-rules", request);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    #endregion

    #region Draw Completed Tests

    [Fact]
    public async Task POST_ExclusionRule_AfterDrawCompleted_Returns400()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser();
        var group = await CreateGroupWithParticipants(organizer.Id, 3, drawCompleted: true);
        var participantIds = await GetParticipantIds(group.Id);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateExclusionRuleRequest(participantIds[0], participantIds[1]);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/exclusion-rules", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("draw", content.ToLower());
        Assert.Contains("completed", content.ToLower());
    }

    #endregion

    #region Draw Validation Tests

    [Fact]
    public async Task POST_ExclusionRule_ThatMakesDrawImpossible_Returns400AndRollsBack()
    {
        // Arrange - Create group with exactly 3 participants
        var (organizer, token) = await CreateAndAuthenticateUser();
        var group = await CreateGroupWithParticipants(organizer.Id, 3);
        var participantIds = await GetParticipantIds(group.Id);

        // Create exclusion rules that make draw impossible
        // If we have 3 people (A, B, C) and we exclude A-B, A-C, then A has no one to give to
        await CreateExclusionRule(group.Id, participantIds[0], participantIds[1], organizer.Id);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Try to create rule that makes draw impossible
        var request = new CreateExclusionRuleRequest(participantIds[0], participantIds[2]);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/exclusion-rules", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("impossible", content.ToLower());

        // Verify rule was not created (rollback worked)
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var ruleCount = await context.ExclusionRules
            .Where(er => er.GroupId == group.Id)
            .CountAsync();

        Assert.Equal(1, ruleCount); // Only the first rule should exist
    }

    [Fact]
    public async Task POST_ExclusionRule_WithValidConfiguration_ReturnsValidDrawValidation()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser();
        var group = await CreateGroupWithParticipants(organizer.Id, 5); // 5 participants for flexibility
        var participantIds = await GetParticipantIds(group.Id);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateExclusionRuleRequest(participantIds[0], participantIds[1]);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/exclusion-rules", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<CreateExclusionRuleResponse>();
        Assert.NotNull(result);
        Assert.True(result.DrawValidation.IsValid);
        Assert.Empty(result.DrawValidation.Errors);
    }

    #endregion

    #region Authorization Tests

    [Fact]
    public async Task POST_ExclusionRule_WithoutAuthentication_Returns401()
    {
        // Arrange
        var (organizer, _) = await CreateAndAuthenticateUser();
        var group = await CreateGroupWithParticipants(organizer.Id, 3);
        var participantIds = await GetParticipantIds(group.Id);

        var request = new CreateExclusionRuleRequest(participantIds[0], participantIds[1]);

        // Act - No authorization header
        var response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/exclusion-rules", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task POST_ExclusionRule_WithInvalidToken_Returns401()
    {
        // Arrange
        var (organizer, _) = await CreateAndAuthenticateUser();
        var group = await CreateGroupWithParticipants(organizer.Id, 3);
        var participantIds = await GetParticipantIds(group.Id);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

        var request = new CreateExclusionRuleRequest(participantIds[0], participantIds[1]);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/exclusion-rules", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task POST_ExclusionRule_AsNonOrganizer_Returns403()
    {
        // Arrange
        var (organizer, _) = await CreateAndAuthenticateUser("Organizer", "User");
        var (otherUser, otherToken) = await CreateAndAuthenticateUser("Other", "User");
        var group = await CreateGroupWithParticipants(organizer.Id, 3);
        var participantIds = await GetParticipantIds(group.Id);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);

        var request = new CreateExclusionRuleRequest(participantIds[0], participantIds[1]);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/exclusion-rules", request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region Error Tests

    [Fact]
    public async Task POST_ExclusionRule_ForNonExistentGroup_Returns404()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser();
        var nonExistentGroupId = Guid.NewGuid();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateExclusionRuleRequest("user1", "user2");

        // Act
        var response = await _client.PostAsJsonAsync($"/api/groups/{nonExistentGroupId}/exclusion-rules", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task POST_ExclusionRule_WithMissingUserId1_Returns404()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser();
        var group = await CreateGroupWithParticipants(organizer.Id, 3);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new { UserId2 = "user2" }; // Missing UserId1

        // Act
        var response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/exclusion-rules", request);

        // Assert - Missing required property results in null UserId1, treated as non-participant
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task POST_ExclusionRule_WithMissingUserId2_Returns404()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser();
        var group = await CreateGroupWithParticipants(organizer.Id, 3);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new { UserId1 = "user1" }; // Missing UserId2

        // Act
        var response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/exclusion-rules", request);

        // Assert - Missing required property results in null UserId2, treated as non-participant
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Multiple Rules Tests

    [Fact]
    public async Task POST_ExclusionRule_MultipleValidRules_AllSucceed()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser();
        var group = await CreateGroupWithParticipants(organizer.Id, 6);
        var participantIds = await GetParticipantIds(group.Id);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - Create 3 different exclusion rules
        var response1 = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/exclusion-rules",
            new CreateExclusionRuleRequest(participantIds[0], participantIds[1]));
        var response2 = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/exclusion-rules",
            new CreateExclusionRuleRequest(participantIds[2], participantIds[3]));
        var response3 = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/exclusion-rules",
            new CreateExclusionRuleRequest(participantIds[4], participantIds[5]));

        // Assert - All should succeed
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);
        Assert.Equal(HttpStatusCode.Created, response2.StatusCode);
        Assert.Equal(HttpStatusCode.Created, response3.StatusCode);

        // Verify all rules exist in database
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var ruleCount = await context.ExclusionRules
            .Where(er => er.GroupId == group.Id)
            .CountAsync();

        Assert.Equal(3, ruleCount);
    }

    #endregion
}
