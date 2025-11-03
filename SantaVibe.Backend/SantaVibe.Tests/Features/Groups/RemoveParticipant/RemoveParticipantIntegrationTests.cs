using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SantaVibe.Api.Data;
using SantaVibe.Api.Data.Entities;
using SantaVibe.Api.Features.Authentication.Login;
using SantaVibe.Tests.Infrastructure;

namespace SantaVibe.Tests.Features.Groups.RemoveParticipant;

/// <summary>
/// Integration tests for the Remove Participant endpoint (DELETE /api/groups/{groupId}/participants/{userId})
/// </summary>
public class RemoveParticipantIntegrationTests : IClassFixture<SantaVibeWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly SantaVibeWebApplicationFactory _factory;

    public RemoveParticipantIntegrationTests(SantaVibeWebApplicationFactory factory)
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
        bool drawCompleted = false)
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
            Budget = drawCompleted ? 50.00m : null,
            DrawCompletedAt = drawCompleted ? DateTimeOffset.UtcNow : null,
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

        await context.SaveChangesAsync();
        return group;
    }

    #region Happy Path Tests

    [Fact]
    public async Task DELETE_RemoveParticipant_WithValidRequest_Returns204()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("Organizer", "Test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = await CreateGroupWithParticipants(organizer.Id, participantCount: 4);

        // Get a participant to remove (not the organizer)
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var participantToRemove = await context.GroupParticipants
            .Where(gp => gp.GroupId == group.Id && gp.UserId != organizer.Id)
            .Select(gp => gp.UserId)
            .FirstAsync();

        // Act
        var response = await _client.DeleteAsync($"/api/groups/{group.Id}/participants/{participantToRemove}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DELETE_RemoveParticipant_RemovesParticipantFromDatabase()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("Organizer", "Test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = await CreateGroupWithParticipants(organizer.Id, participantCount: 4);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var participantToRemove = await context.GroupParticipants
            .Where(gp => gp.GroupId == group.Id && gp.UserId != organizer.Id)
            .Select(gp => gp.UserId)
            .FirstAsync();

        // Act
        var response = await _client.DeleteAsync($"/api/groups/{group.Id}/participants/{participantToRemove}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var participantExists = await verifyContext.GroupParticipants
            .AnyAsync(gp => gp.GroupId == group.Id && gp.UserId == participantToRemove);

        Assert.False(participantExists);

        // Verify remaining participant count
        var remainingCount = await verifyContext.GroupParticipants
            .CountAsync(gp => gp.GroupId == group.Id);

        Assert.Equal(3, remainingCount);
    }

    [Fact]
    public async Task DELETE_RemoveParticipant_CleansUpRelatedExclusionRules()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("Organizer", "Test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = await CreateGroupWithParticipants(organizer.Id, participantCount: 4);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var participants = await context.GroupParticipants
            .Where(gp => gp.GroupId == group.Id)
            .Select(gp => gp.UserId)
            .ToListAsync();

        var participantToRemove = participants.First(p => p != organizer.Id);
        var otherParticipant = participants.First(p => p != organizer.Id && p != participantToRemove);

        // Add exclusion rules involving the participant to remove
        context.ExclusionRules.Add(new ExclusionRule
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            UserId1 = participantToRemove,
            UserId2 = otherParticipant,
            CreatedByUserId = organizer.Id,
            CreatedAt = DateTimeOffset.UtcNow
        });

        context.ExclusionRules.Add(new ExclusionRule
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            UserId1 = organizer.Id,
            UserId2 = participantToRemove,
            CreatedByUserId = organizer.Id,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await context.SaveChangesAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.DeleteAsync($"/api/groups/{group.Id}/participants/{participantToRemove}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var exclusionRules = await verifyContext.ExclusionRules
            .Where(er => er.GroupId == group.Id &&
                        (er.UserId1 == participantToRemove || er.UserId2 == participantToRemove))
            .ToListAsync();

        Assert.Empty(exclusionRules);
    }

    [Fact]
    public async Task DELETE_RemoveParticipant_UpdatesGroupModifiedTimestamp()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("Organizer", "Test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = await CreateGroupWithParticipants(organizer.Id, participantCount: 4);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var participantToRemove = await context.GroupParticipants
            .Where(gp => gp.GroupId == group.Id && gp.UserId != organizer.Id)
            .Select(gp => gp.UserId)
            .FirstAsync();

        // Get original timestamp from database
        var originalGroup = await context.Groups.AsNoTracking().FirstAsync(g => g.Id == group.Id);
        var originalUpdatedAt = originalGroup.UpdatedAt;

        // Act
        await Task.Delay(100); // Ensure time difference
        var response = await _client.DeleteAsync($"/api/groups/{group.Id}/participants/{participantToRemove}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var updatedGroup = await verifyContext.Groups.FindAsync(group.Id);
        Assert.NotNull(updatedGroup);
        Assert.NotNull(updatedGroup.UpdatedAt);

        // UpdatedAt should be set and greater than original (if original was null, it should now be set)
        if (originalUpdatedAt.HasValue)
        {
            Assert.True(updatedGroup.UpdatedAt > originalUpdatedAt,
                $"UpdatedAt should be updated. Original: {originalUpdatedAt}, New: {updatedGroup.UpdatedAt}");
        }
        else
        {
            Assert.NotNull(updatedGroup.UpdatedAt);
        }
    }

    #endregion

    #region Business Rule Validation Tests

    [Fact]
    public async Task DELETE_RemoveParticipant_CannotRemoveOrganizer_Returns400()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("Organizer", "Test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = await CreateGroupWithParticipants(organizer.Id, participantCount: 4);

        // Act - Try to remove the organizer
        var response = await _client.DeleteAsync($"/api/groups/{group.Id}/participants/{organizer.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        // Verify organizer still exists
        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var organizerExists = await verifyContext.GroupParticipants
            .AnyAsync(gp => gp.GroupId == group.Id && gp.UserId == organizer.Id);

        Assert.True(organizerExists);
    }

    [Fact]
    public async Task DELETE_RemoveParticipant_AfterDrawCompleted_Returns400()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("Organizer", "Test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = await CreateGroupWithParticipants(organizer.Id, participantCount: 4, drawCompleted: true);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var participantToRemove = await context.GroupParticipants
            .Where(gp => gp.GroupId == group.Id && gp.UserId != organizer.Id)
            .Select(gp => gp.UserId)
            .FirstAsync();

        // Act
        var response = await _client.DeleteAsync($"/api/groups/{group.Id}/participants/{participantToRemove}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        // Verify participant still exists
        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var participantExists = await verifyContext.GroupParticipants
            .AnyAsync(gp => gp.GroupId == group.Id && gp.UserId == participantToRemove);

        Assert.True(participantExists);
    }

    #endregion

    #region Authorization Tests

    [Fact]
    public async Task DELETE_RemoveParticipant_WithoutAuthentication_Returns401()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();

        // Act
        var response = await _client.DeleteAsync($"/api/groups/{groupId}/participants/{userId}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DELETE_RemoveParticipant_WithInvalidToken_Returns401()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
        var groupId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();

        // Act
        var response = await _client.DeleteAsync($"/api/groups/{groupId}/participants/{userId}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DELETE_RemoveParticipant_AsNonOrganizer_Returns403()
    {
        // Arrange
        var (organizer, _) = await CreateAndAuthenticateUser("Organizer", "Test");
        var (nonOrganizer, nonOrganizerToken) = await CreateAndAuthenticateUser("NonOrganizer", "Test");

        var group = await CreateGroupWithParticipants(organizer.Id, participantCount: 4);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var participantToRemove = await context.GroupParticipants
            .Where(gp => gp.GroupId == group.Id && gp.UserId != organizer.Id)
            .Select(gp => gp.UserId)
            .FirstAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", nonOrganizerToken);

        // Act
        var response = await _client.DeleteAsync($"/api/groups/{group.Id}/participants/{participantToRemove}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        // Verify participant still exists
        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var participantExists = await verifyContext.GroupParticipants
            .AnyAsync(gp => gp.GroupId == group.Id && gp.UserId == participantToRemove);

        Assert.True(participantExists);
    }

    [Fact]
    public async Task DELETE_RemoveParticipant_AsOrganizer_Returns204()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("Organizer", "Test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = await CreateGroupWithParticipants(organizer.Id, participantCount: 4);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var participantToRemove = await context.GroupParticipants
            .Where(gp => gp.GroupId == group.Id && gp.UserId != organizer.Id)
            .Select(gp => gp.UserId)
            .FirstAsync();

        // Act
        var response = await _client.DeleteAsync($"/api/groups/{group.Id}/participants/{participantToRemove}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    #endregion

    #region Not Found Tests

    [Fact]
    public async Task DELETE_RemoveParticipant_WithNonExistentGroup_Returns404()
    {
        // Arrange
        var (_, token) = await CreateAndAuthenticateUser("User", "Test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var nonExistentGroupId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();

        // Act
        var response = await _client.DeleteAsync($"/api/groups/{nonExistentGroupId}/participants/{userId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DELETE_RemoveParticipant_WithNonExistentParticipant_Returns404()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("Organizer", "Test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = await CreateGroupWithParticipants(organizer.Id, participantCount: 4);
        var nonExistentUserId = Guid.NewGuid().ToString();

        // Act
        var response = await _client.DeleteAsync($"/api/groups/{group.Id}/participants/{nonExistentUserId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DELETE_RemoveParticipant_WithInvalidGuid_Returns404()
    {
        // Arrange
        var (_, token) = await CreateAndAuthenticateUser("User", "Test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.DeleteAsync("/api/groups/invalid-guid/participants/some-user-id");

        // Assert - Route constraint {groupId:guid} causes 404 when invalid GUID format
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Transactionality Tests

    [Fact]
    public async Task DELETE_RemoveParticipant_IsTransactional()
    {
        // Arrange
        var (organizer, token) = await CreateAndAuthenticateUser("Organizer", "Test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = await CreateGroupWithParticipants(organizer.Id, participantCount: 4);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var participants = await context.GroupParticipants
            .Where(gp => gp.GroupId == group.Id)
            .Select(gp => gp.UserId)
            .ToListAsync();

        var participantToRemove = participants.First(p => p != organizer.Id);
        var otherParticipant = participants.First(p => p != organizer.Id && p != participantToRemove);

        // Add exclusion rule
        context.ExclusionRules.Add(new ExclusionRule
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            UserId1 = participantToRemove,
            UserId2 = otherParticipant,
            CreatedByUserId = organizer.Id,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await context.SaveChangesAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.DeleteAsync($"/api/groups/{group.Id}/participants/{participantToRemove}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Verify all changes were committed atomically
        var participantExists = await verifyContext.GroupParticipants
            .AnyAsync(gp => gp.GroupId == group.Id && gp.UserId == participantToRemove);

        var exclusionRules = await verifyContext.ExclusionRules
            .Where(er => er.GroupId == group.Id &&
                        (er.UserId1 == participantToRemove || er.UserId2 == participantToRemove))
            .CountAsync();

        var updatedGroup = await verifyContext.Groups.FindAsync(group.Id);

        Assert.False(participantExists);
        Assert.Equal(0, exclusionRules);
        Assert.NotNull(updatedGroup!.UpdatedAt);
    }

    #endregion
}
