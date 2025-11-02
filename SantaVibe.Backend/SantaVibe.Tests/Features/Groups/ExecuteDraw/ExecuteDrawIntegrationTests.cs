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
using SantaVibe.Api.Features.Groups.ExecuteDraw;
using SantaVibe.Tests.Infrastructure;
using Xunit;

namespace SantaVibe.Tests.Features.Groups.ExecuteDraw;

/// <summary>
/// Integration tests for the Execute Draw endpoint (POST /api/groups/{groupId}/draw)
/// </summary>
public class ExecuteDrawIntegrationTests : IClassFixture<SantaVibeWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly SantaVibeWebApplicationFactory _factory;

    public ExecuteDrawIntegrationTests(SantaVibeWebApplicationFactory factory)
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
    public async Task POST_ExecuteDraw_WithValidRequest_Returns200WithSuccessResponse()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("Organizer", "Test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = await CreateGroupWithParticipants(organizer.Id, participantCount: 4);
        var request = new ExecuteDrawRequest(Budget: 50.00m);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/draw", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ExecuteDrawResponse>();
        Assert.NotNull(result);
        Assert.Equal(group.Id, result.GroupId);
        Assert.Equal(50.00m, result.Budget);
        Assert.True(result.DrawCompleted);
        Assert.InRange(result.DrawCompletedAt, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(1));
        Assert.Equal(4, result.ParticipantCount);
        Assert.Equal(4, result.AssignmentsCreated);
        Assert.NotNull(result.MyAssignment);
        Assert.NotEqual(organizer.Id, result.MyAssignment.RecipientId); // Can't be assigned to self
    }

    [Fact]
    public async Task POST_ExecuteDraw_CreatesAssignmentsForAllParticipants()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("Organizer", "Test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = await CreateGroupWithParticipants(organizer.Id, participantCount: 5);
        var request = new ExecuteDrawRequest(Budget: 100.00m);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/draw", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var assignments = await context.Assignments
            .Where(a => a.GroupId == group.Id)
            .ToListAsync();

        Assert.Equal(5, assignments.Count);

        // Verify each participant is a Santa exactly once
        var santas = assignments.Select(a => a.SantaUserId).ToList();
        Assert.Equal(5, santas.Distinct().Count());

        // Verify each participant is a recipient exactly once
        var recipients = assignments.Select(a => a.RecipientUserId).ToList();
        Assert.Equal(5, recipients.Distinct().Count());

        // Verify no self-assignments
        Assert.All(assignments, a => Assert.NotEqual(a.SantaUserId, a.RecipientUserId));
    }

    [Fact]
    public async Task POST_ExecuteDraw_UpdatesGroupState()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("Organizer", "Test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = await CreateGroupWithParticipants(organizer.Id, participantCount: 4);
        var request = new ExecuteDrawRequest(Budget: 75.50m);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/draw", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var updatedGroup = await context.Groups.FindAsync(group.Id);
        Assert.NotNull(updatedGroup);
        Assert.Equal(75.50m, updatedGroup!.Budget);
        Assert.NotNull(updatedGroup.DrawCompletedAt);
        Assert.InRange(updatedGroup.DrawCompletedAt.Value, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(1));
    }

    [Fact]
    public async Task POST_ExecuteDraw_SchedulesEmailNotifications()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("Organizer", "Test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = await CreateGroupWithParticipants(organizer.Id, participantCount: 5);
        var request = new ExecuteDrawRequest(Budget: 50.00m);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/draw", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var emailNotifications = await context.EmailNotifications
            .Where(en => en.GroupId == group.Id)
            .ToListAsync();

        Assert.Equal(5, emailNotifications.Count);
        Assert.All(emailNotifications, en =>
        {
            Assert.Equal(EmailNotificationType.DrawCompleted, en.Type);
            Assert.NotNull(en.ScheduledAt);
            Assert.Null(en.SentAt);
            Assert.Equal(0, en.AttemptCount);
        });
    }

    [Fact]
    public async Task POST_ExecuteDraw_WithMinimumParticipants_Succeeds()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("Organizer", "Test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = await CreateGroupWithParticipants(organizer.Id, participantCount: 4);
        var request = new ExecuteDrawRequest(Budget: 25.00m);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/draw", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ExecuteDrawResponse>();
        Assert.Equal(4, result!.ParticipantCount);
    }

    [Fact]
    public async Task POST_ExecuteDraw_WithValidExclusionRules_RespectsConstraints()
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

        // Add one exclusion rule
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

        var request = new ExecuteDrawRequest(Budget: 50.00m);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/draw", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify the exclusion rule was respected
        var assignments = await context.Assignments
            .Where(a => a.GroupId == group.Id)
            .ToListAsync();

        // User1 should not be assigned to User2 and vice versa
        Assert.DoesNotContain(assignments, a => a.SantaUserId == participantIds[0] && a.RecipientUserId == participantIds[1]);
        Assert.DoesNotContain(assignments, a => a.SantaUserId == participantIds[1] && a.RecipientUserId == participantIds[0]);
    }

    #endregion

    #region Budget Validation Tests

    [Fact]
    public async Task POST_ExecuteDraw_WithValidBudget_Succeeds()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("Organizer", "Test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = await CreateGroupWithParticipants(organizer.Id, participantCount: 4);
        var request = new ExecuteDrawRequest(Budget: 99.99m);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/draw", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ExecuteDrawResponse>();
        Assert.Equal(99.99m, result!.Budget);
    }

    [Fact]
    public async Task POST_ExecuteDraw_WithMinimumBudget_Succeeds()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("Organizer", "Test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = await CreateGroupWithParticipants(organizer.Id, participantCount: 4);
        var request = new ExecuteDrawRequest(Budget: 0.01m);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/draw", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ExecuteDrawResponse>();
        Assert.Equal(0.01m, result!.Budget);
    }

    [Fact]
    public async Task POST_ExecuteDraw_WithMaximumBudget_Succeeds()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("Organizer", "Test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = await CreateGroupWithParticipants(organizer.Id, participantCount: 4);
        var request = new ExecuteDrawRequest(Budget: 99999999.99m);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/draw", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ExecuteDrawResponse>();
        Assert.Equal(99999999.99m, result!.Budget);
    }

    [Fact]
    public async Task POST_ExecuteDraw_WithZeroBudget_Returns400()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("Organizer", "Test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = await CreateGroupWithParticipants(organizer.Id, participantCount: 3);
        var request = new ExecuteDrawRequest(Budget: 0.00m);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/draw", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task POST_ExecuteDraw_WithNegativeBudget_Returns400()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("Organizer", "Test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = await CreateGroupWithParticipants(organizer.Id, participantCount: 3);
        var request = new ExecuteDrawRequest(Budget: -10.00m);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/draw", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task POST_ExecuteDraw_WithExcessiveBudget_Returns400()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("Organizer", "Test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = await CreateGroupWithParticipants(organizer.Id, participantCount: 3);
        var request = new ExecuteDrawRequest(Budget: 100000000.00m);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/draw", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task POST_ExecuteDraw_WithThreeDecimalPlaces_Returns400()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("Organizer", "Test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = await CreateGroupWithParticipants(organizer.Id, participantCount: 3);
        var request = new ExecuteDrawRequest(Budget: 50.123m);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/draw", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Participant Validation Tests

    [Fact]
    public async Task POST_ExecuteDraw_WithInsufficientParticipants_Returns400()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("Organizer", "Test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = await CreateGroupWithParticipants(organizer.Id, participantCount: 2);
        var request = new ExecuteDrawRequest(Budget: 50.00m);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/draw", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task POST_ExecuteDraw_WithOnlyOrganizer_Returns400()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("Organizer", "Test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = await CreateGroupWithParticipants(organizer.Id, participantCount: 1);
        var request = new ExecuteDrawRequest(Budget: 50.00m);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/draw", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Draw Already Completed Tests

    [Fact]
    public async Task POST_ExecuteDraw_WhenDrawAlreadyCompleted_Returns400()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("Organizer", "Test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = await CreateGroupWithParticipants(organizer.Id, participantCount: 4);
        var request = new ExecuteDrawRequest(Budget: 50.00m);

        // Execute draw first time
        var firstResponse = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/draw", request);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        // Act - Try to execute draw again
        var secondResponse = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/draw", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, secondResponse.StatusCode);
    }

    [Fact]
    public async Task POST_ExecuteDraw_WhenDrawAlreadyCompleted_DoesNotCreateDuplicateAssignments()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("Organizer", "Test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = await CreateGroupWithParticipants(organizer.Id, participantCount: 4);
        var request = new ExecuteDrawRequest(Budget: 50.00m);

        // Execute draw first time
        await _client.PostAsJsonAsync($"/api/groups/{group.Id}/draw", request);

        // Act - Try to execute draw again
        await _client.PostAsJsonAsync($"/api/groups/{group.Id}/draw", request);

        // Assert - Should still only have 4 assignments from first draw
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var assignments = await context.Assignments
            .Where(a => a.GroupId == group.Id)
            .ToListAsync();

                    Assert.Equal(4, assignments.Count);
    }

    #endregion

    #region Authorization Tests

    [Fact]
    public async Task POST_ExecuteDraw_WithoutAuthentication_Returns401()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var request = new ExecuteDrawRequest(Budget: 50.00m);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/groups/{groupId}/draw", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task POST_ExecuteDraw_WithInvalidToken_Returns401()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
        var groupId = Guid.NewGuid();
        var request = new ExecuteDrawRequest(Budget: 50.00m);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/groups/{groupId}/draw", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task POST_ExecuteDraw_AsNonOrganizer_Returns403()
    {
        // Arrange
        var (organizer, _) = await CreateAndAuthenticateUser("Organizer", "Test");
        var (nonOrganizer, nonOrganizerToken) = await CreateAndAuthenticateUser("NonOrganizer", "Test");

        var group = await CreateGroupWithParticipants(organizer.Id, participantCount: 3);
        var request = new ExecuteDrawRequest(Budget: 50.00m);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", nonOrganizerToken);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/draw", request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task POST_ExecuteDraw_AsOrganizer_Returns200()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("Organizer", "Test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = await CreateGroupWithParticipants(organizer.Id, participantCount: 4);
        var request = new ExecuteDrawRequest(Budget: 50.00m);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/draw", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task POST_ExecuteDraw_AsParticipantButNotOrganizer_Returns403()
    {
        // Arrange
        var (organizer, _) = await CreateAndAuthenticateUser("Organizer", "Test");
        var (participant, participantToken) = await CreateAndAuthenticateUser("Participant", "Test");

        var group = await CreateGroupWithParticipants(organizer.Id, participantCount: 3);

        // Add participant to group manually
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.GroupParticipants.Add(new GroupParticipant
        {
            GroupId = group.Id,
            UserId = participant.Id,
            JoinedAt = DateTimeOffset.UtcNow
        });
        await context.SaveChangesAsync();

        var request = new ExecuteDrawRequest(Budget: 50.00m);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", participantToken);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/draw", request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region Not Found Tests

    [Fact]
    public async Task POST_ExecuteDraw_WithNonExistentGroup_Returns404()
    {
        // Arrange
        var (_, token) = await CreateAndAuthenticateUser("User", "Test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var nonExistentGroupId = Guid.NewGuid();
        var request = new ExecuteDrawRequest(Budget: 50.00m);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/groups/{nonExistentGroupId}/draw", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task POST_ExecuteDraw_WithInvalidGuid_Returns404()
    {
        // Arrange
        var (_, token) = await CreateAndAuthenticateUser("User", "Test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new ExecuteDrawRequest(Budget: 50.00m);

        // Act
        var response = await _client.PostAsJsonAsync("/api/groups/invalid-guid/draw", request);

        // Assert - Route constraint {groupId:guid} causes 404 when invalid GUID format
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Exclusion Rule Validation Tests

    [Fact]
    public async Task POST_ExecuteDraw_WithImpossibleExclusionRules_Returns400()
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

        var request = new ExecuteDrawRequest(Budget: 50.00m);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/draw", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Assignment Verification Tests

    [Fact]
    public async Task POST_ExecuteDraw_OrganizerReceivesTheirAssignment()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("Organizer", "Test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = await CreateGroupWithParticipants(organizer.Id, participantCount: 4);
        var request = new ExecuteDrawRequest(Budget: 50.00m);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/draw", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ExecuteDrawResponse>();
        Assert.NotNull(result!.MyAssignment);
        Assert.NotEqual(organizer.Id, result.MyAssignment.RecipientId); // Not self
        Assert.NotEmpty(result.MyAssignment.RecipientFirstName);
        Assert.NotEmpty(result.MyAssignment.RecipientLastName);
    }

    [Fact]
    public async Task POST_ExecuteDraw_AssignmentsFormCompleteCycle()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("Organizer", "Test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = await CreateGroupWithParticipants(organizer.Id, participantCount: 5);
        var request = new ExecuteDrawRequest(Budget: 50.00m);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/draw", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var assignments = await context.Assignments
            .Where(a => a.GroupId == group.Id)
            .ToListAsync();

        // Build assignment map
        var assignmentMap = assignments.ToDictionary(a => a.SantaUserId, a => a.RecipientUserId);

        // Verify we can traverse from any participant back to themselves
        var startUser = assignments.First().SantaUserId;
        var currentUser = startUser;
        var visited = new HashSet<string>();

        // Follow the chain
        for (int i = 0; i < assignments.Count; i++)
        {
            visited.Add(currentUser);
            currentUser = assignmentMap[currentUser];
        }

        // Should return to start and visit all participants
        Assert.Equal(startUser, currentUser);
        Assert.Equal(5, visited.Count);
    }

    #endregion

    #region Transactionality Tests

    [Fact]
    public async Task POST_ExecuteDraw_IsTransactional_OnSuccess()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("Organizer", "Test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = await CreateGroupWithParticipants(organizer.Id, participantCount: 4);
        var request = new ExecuteDrawRequest(Budget: 50.00m);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/draw", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Verify all changes were committed atomically
        var updatedGroup = await context.Groups.FindAsync(group.Id);
        var assignments = await context.Assignments.Where(a => a.GroupId == group.Id).ToListAsync();
        var notifications = await context.EmailNotifications.Where(en => en.GroupId == group.Id).ToListAsync();

        Assert.NotNull(updatedGroup!.DrawCompletedAt);
        Assert.Equal(50.00m, updatedGroup.Budget);
        Assert.Equal(4, assignments.Count);
        Assert.Equal(4, notifications.Count);
    }

    #endregion
}
