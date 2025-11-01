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
using SantaVibe.Api.Features.Groups.ValidateDraw;
using SantaVibe.Tests.Infrastructure;
using Xunit;

namespace SantaVibe.Tests.Features.Groups.ValidateDraw;

/// <summary>
/// Integration tests for the Validate Draw endpoint (GET /api/groups/{groupId}/draw/validate)
/// </summary>
public class ValidateDrawIntegrationTests : IClassFixture<SantaVibeWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly SantaVibeWebApplicationFactory _factory;

    public ValidateDrawIntegrationTests(SantaVibeWebApplicationFactory factory)
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

    private async Task<Group> CreateGroupWithParticipants(
        string organizerUserId,
        int participantCount,
        List<(string userId1, string userId2)>? exclusionPairs = null)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Create group
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = $"Test Group {Guid.NewGuid()}",
            OrganizerUserId = organizerUserId,
            InvitationToken = Guid.NewGuid(),
            Budget = null,
            DrawCompletedAt = null,
            CreatedAt = DateTimeOffset.UtcNow
        };

        context.Groups.Add(group);

        // Add organizer as first participant
        context.GroupParticipants.Add(new GroupParticipant
        {
            GroupId = group.Id,
            UserId = organizerUserId,
            JoinedAt = DateTimeOffset.UtcNow
        });

        // Create additional participants
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

        // Add exclusion rules if provided
        if (exclusionPairs != null)
        {
            foreach (var (userId1, userId2) in exclusionPairs)
            {
                context.ExclusionRules.Add(new ExclusionRule
                {
                    Id = Guid.NewGuid(),
                    GroupId = group.Id,
                    UserId1 = userId1,
                    UserId2 = userId2,
                    CreatedByUserId = organizerUserId,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
        }

        await context.SaveChangesAsync();
        return group;
    }

    #region Happy Path Tests

    [Fact]
    public async Task GET_ValidateDraw_WithValidGroup_Returns200WithValidationSuccess()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("Organizer", "Test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = await CreateGroupWithParticipants(organizer.Id, participantCount: 3);

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}/draw/validate");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ValidateDrawResponse>();
        Assert.NotNull(result);
        Assert.Equal(group.Id, result.GroupId);
        Assert.True(result.IsValid);
        Assert.True(result.CanDraw);
        Assert.Equal(3, result.ParticipantCount);
        Assert.Equal(0, result.ExclusionRuleCount);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task GET_ValidateDraw_WithMinimumParticipants_ReturnsValid()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("Organizer", "Test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = await CreateGroupWithParticipants(organizer.Id, participantCount: 3);

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}/draw/validate");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ValidateDrawResponse>();
        Assert.True(result!.IsValid);
        Assert.True(result.CanDraw);
        Assert.Equal(3, result.ParticipantCount);
    }

    [Fact]
    public async Task GET_ValidateDraw_WithManyParticipants_ReturnsValid()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("Organizer", "Test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = await CreateGroupWithParticipants(organizer.Id, participantCount: 10);

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}/draw/validate");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ValidateDrawResponse>();
        Assert.True(result!.IsValid);
        Assert.True(result.CanDraw);
        Assert.Equal(10, result.ParticipantCount);
    }

    [Fact]
    public async Task GET_ValidateDraw_WithValidExclusionRules_ReturnsValid()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("Organizer", "Test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Create group with 4 participants
        var group = await CreateGroupWithParticipants(organizer.Id, participantCount: 4);

        // Get participant IDs
        var participantIds = await context.GroupParticipants
            .Where(gp => gp.GroupId == group.Id)
            .Select(gp => gp.UserId)
            .ToListAsync();

        // Add one exclusion rule (not too restrictive for 4 participants)
        var exclusionRule = new ExclusionRule
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            UserId1 = participantIds[0],
            UserId2 = participantIds[1],
            CreatedByUserId = organizer.Id,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.ExclusionRules.Add(exclusionRule);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}/draw/validate");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ValidateDrawResponse>();
        Assert.True(result!.IsValid);
        Assert.True(result.CanDraw);
        Assert.Equal(4, result.ParticipantCount);
        Assert.Equal(1, result.ExclusionRuleCount);
        Assert.Empty(result.Errors);
    }

    #endregion

    #region Validation Error Tests

    [Fact]
    public async Task GET_ValidateDraw_WithInsufficientParticipants_ReturnsInvalid()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("Organizer", "Test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = await CreateGroupWithParticipants(organizer.Id, participantCount: 2);

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}/draw/validate");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ValidateDrawResponse>();
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.False(result.CanDraw);
        Assert.Equal(2, result.ParticipantCount);
        Assert.Contains(result.Errors, e => e.Contains("Minimum 3 participants"));
    }

    [Fact]
    public async Task GET_ValidateDraw_WithOnlyOrganizer_ReturnsInvalid()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("Organizer", "Test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = await CreateGroupWithParticipants(organizer.Id, participantCount: 1);

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}/draw/validate");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ValidateDrawResponse>();
        Assert.False(result!.IsValid);
        Assert.False(result.CanDraw);
        Assert.Equal(1, result.ParticipantCount);
    }

    [Fact]
    public async Task GET_ValidateDraw_WithOverlyRestrictiveExclusionRules_ReturnsInvalid()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("Organizer", "Test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Create group with 3 participants
        var group = await CreateGroupWithParticipants(organizer.Id, participantCount: 3);

        // Get participant IDs
        var participantIds = await context.GroupParticipants
            .Where(gp => gp.GroupId == group.Id)
            .Select(gp => gp.UserId)
            .ToListAsync();

        // Add exclusion rules that make draw impossible
        // In a 3-person group, if person A can't draw B, and B can't draw C, and C can't draw A
        // it becomes impossible
        context.ExclusionRules.Add(new ExclusionRule
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            UserId1 = participantIds[0],
            UserId2 = participantIds[1],
            CreatedByUserId = organizer.Id,
            CreatedAt = DateTimeOffset.UtcNow
        });
        context.ExclusionRules.Add(new ExclusionRule
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            UserId1 = participantIds[1],
            UserId2 = participantIds[2],
            CreatedByUserId = organizer.Id,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}/draw/validate");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ValidateDrawResponse>();
        Assert.False(result!.IsValid);
        Assert.False(result.CanDraw);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task GET_ValidateDraw_WithCompletedDraw_ReturnsWarning()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("Organizer", "Test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var group = await CreateGroupWithParticipants(organizer.Id, participantCount: 3);

        // Mark draw as completed
        var dbGroup = await context.Groups.FindAsync(group.Id);
        dbGroup!.DrawCompletedAt = DateTimeOffset.UtcNow;
        dbGroup.Budget = 50.00m;
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}/draw/validate");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ValidateDrawResponse>();
        Assert.False(result!.CanDraw); // Can't draw again
        Assert.Contains(result.Warnings, w => w.Contains("already been completed"));
    }

    #endregion

    #region Authorization Tests

    [Fact]
    public async Task GET_ValidateDraw_WithoutAuthentication_Returns401()
    {
        // Arrange
        var groupId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/groups/{groupId}/draw/validate");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GET_ValidateDraw_WithInvalidToken_Returns401()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
        var groupId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/groups/{groupId}/draw/validate");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GET_ValidateDraw_AsNonOrganizer_Returns403()
    {
        // Arrange
        var (organizer, _) = await CreateAndAuthenticateUser("Organizer", "Test");
        var (nonOrganizer, nonOrganizerToken) = await CreateAndAuthenticateUser("NonOrganizer", "Test");

        var group = await CreateGroupWithParticipants(organizer.Id, participantCount: 3);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", nonOrganizerToken);

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}/draw/validate");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GET_ValidateDraw_AsOrganizer_Returns200()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("Organizer", "Test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = await CreateGroupWithParticipants(organizer.Id, participantCount: 3);

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}/draw/validate");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region Not Found Tests

    [Fact]
    public async Task GET_ValidateDraw_WithNonExistentGroup_Returns404()
    {
        // Arrange
        var (_, token) = await CreateAndAuthenticateUser("User", "Test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var nonExistentGroupId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/groups/{nonExistentGroupId}/draw/validate");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GET_ValidateDraw_WithInvalidGuid_Returns404()
    {
        // Arrange
        var (_, token) = await CreateAndAuthenticateUser("User", "Test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/groups/invalid-guid/draw/validate");

        // Assert - Route constraint {groupId:guid} causes 404 when invalid GUID format
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Business Logic Tests

    [Fact]
    public async Task GET_ValidateDraw_IncludesCorrectParticipantCount()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("Organizer", "Test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = await CreateGroupWithParticipants(organizer.Id, participantCount: 5);

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}/draw/validate");

        // Assert
        var result = await response.Content.ReadFromJsonAsync<ValidateDrawResponse>();
        Assert.Equal(5, result!.ParticipantCount);
    }

    [Fact]
    public async Task GET_ValidateDraw_IncludesCorrectExclusionRuleCount()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("Organizer", "Test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var group = await CreateGroupWithParticipants(organizer.Id, participantCount: 5);

        // Get participant IDs
        var participantIds = await context.GroupParticipants
            .Where(gp => gp.GroupId == group.Id)
            .Select(gp => gp.UserId)
            .ToListAsync();

        // Add 2 exclusion rules
        context.ExclusionRules.Add(new ExclusionRule
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            UserId1 = participantIds[0],
            UserId2 = participantIds[1],
            CreatedByUserId = organizer.Id,
            CreatedAt = DateTimeOffset.UtcNow
        });
        context.ExclusionRules.Add(new ExclusionRule
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            UserId1 = participantIds[2],
            UserId2 = participantIds[3],
            CreatedByUserId = organizer.Id,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}/draw/validate");

        // Assert
        var result = await response.Content.ReadFromJsonAsync<ValidateDrawResponse>();
        Assert.Equal(2, result!.ExclusionRuleCount);
    }

    [Fact]
    public async Task GET_ValidateDraw_MultipleCallsReturnConsistentResults()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("Organizer", "Test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = await CreateGroupWithParticipants(organizer.Id, participantCount: 4);

        // Act - Call validation multiple times
        var response1 = await _client.GetAsync($"/api/groups/{group.Id}/draw/validate");
        var response2 = await _client.GetAsync($"/api/groups/{group.Id}/draw/validate");
        var response3 = await _client.GetAsync($"/api/groups/{group.Id}/draw/validate");

        // Assert - All should return same result
        var result1 = await response1.Content.ReadFromJsonAsync<ValidateDrawResponse>();
        var result2 = await response2.Content.ReadFromJsonAsync<ValidateDrawResponse>();
        var result3 = await response3.Content.ReadFromJsonAsync<ValidateDrawResponse>();

        Assert.Equal(result1!.IsValid, result2!.IsValid);
        Assert.Equal(result1.IsValid, result3!.IsValid);
        Assert.Equal(result1.CanDraw, result2.CanDraw);
        Assert.Equal(result1.CanDraw, result3.CanDraw);
    }

    #endregion
}
