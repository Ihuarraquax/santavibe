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
using SantaVibe.Api.Features.Invitations.AcceptInvitation;
using SantaVibe.Tests.Infrastructure;
using Xunit;

namespace SantaVibe.Tests.Features.Invitations.AcceptInvitation;

/// <summary>
/// Integration tests for the Accept Invitation endpoint (POST /api/invitations/{token}/accept)
/// </summary>
public class AcceptInvitationIntegrationTests : IClassFixture<SantaVibeWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly SantaVibeWebApplicationFactory _factory;

    public AcceptInvitationIntegrationTests(SantaVibeWebApplicationFactory factory)
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

    private async Task<Group> CreateGroup(ApplicationUser organizer, string groupName = "Test Group")
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = groupName,
            OrganizerUserId = organizer.Id,
            InvitationToken = Guid.NewGuid(),
            Budget = null,
            DrawCompletedAt = null,
            CreatedAt = DateTimeOffset.UtcNow
        };

        context.Groups.Add(group);

        // Add organizer as first participant
        var participant = new GroupParticipant
        {
            GroupId = group.Id,
            UserId = organizer.Id,
            JoinedAt = DateTimeOffset.UtcNow
        };

        context.GroupParticipants.Add(participant);
        await context.SaveChangesAsync();

        return group;
    }

    #region Happy Path Tests

    [Fact]
    public async Task POST_AcceptInvitation_WithValidToken_Returns201WithCorrectData()
    {
        // Arrange
        var (organizer, _) = await CreateAndAuthenticateUser("John", "Organizer");
        var (participant, participantToken) = await CreateAndAuthenticateUser("Jane", "Participant");
        var group = await CreateGroup(organizer, "Family Secret Santa 2025");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", participantToken);

        var request = new AcceptInvitationRequest
        {
            BudgetSuggestion = null
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/invitations/{group.InvitationToken}/accept", request);

        // Diagnostic: Print response content if not successful
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Expected 201 Created but got {response.StatusCode}. Response: {errorContent}");
        }

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<AcceptInvitationResponse>();
        Assert.NotNull(result);

        // Verify response fields
        Assert.Equal(group.Id, result.GroupId);
        Assert.Equal("Family Secret Santa 2025", result.GroupName);
        Assert.Equal("John Organizer", result.OrganizerName);
        Assert.Equal(2, result.ParticipantCount); // Organizer + new participant
        Assert.Null(result.Budget);
        Assert.False(result.DrawCompleted);
        Assert.True(result.JoinedAt <= DateTimeOffset.UtcNow);
        Assert.True(result.JoinedAt >= DateTimeOffset.UtcNow.AddMinutes(-1));

        // Verify Location header
        Assert.NotNull(response.Headers.Location);
        Assert.Equal($"/api/groups/{result.GroupId}", response.Headers.Location.ToString());
    }

    [Fact]
    public async Task POST_AcceptInvitation_WithBudgetSuggestion_StoresBudgetCorrectly()
    {
        // Arrange
        var (organizer, _) = await CreateAndAuthenticateUser("Bob", "Organizer");
        var (participant, participantToken) = await CreateAndAuthenticateUser("Alice", "Participant");
        var group = await CreateGroup(organizer, "Office Secret Santa");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", participantToken);

        var request = new AcceptInvitationRequest
        {
            BudgetSuggestion = 80.50m
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/invitations/{group.InvitationToken}/accept", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Verify budget suggestion stored in database
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var storedParticipant = await context.GroupParticipants
            .FirstOrDefaultAsync(gp => gp.GroupId == group.Id && gp.UserId == participant.Id);

        Assert.NotNull(storedParticipant);
        Assert.Equal(80.50m, storedParticipant.BudgetSuggestion);
    }

    [Fact]
    public async Task POST_AcceptInvitation_WithValidToken_CreatesParticipantInDatabase()
    {
        // Arrange
        var (organizer, _) = await CreateAndAuthenticateUser("Tom", "Organizer");
        var (participant, participantToken) = await CreateAndAuthenticateUser("Sarah", "Participant");
        var group = await CreateGroup(organizer, "Friends Gift Exchange");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", participantToken);

        var request = new AcceptInvitationRequest();

        // Act
        var response = await _client.PostAsJsonAsync($"/api/invitations/{group.InvitationToken}/accept", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Verify participant exists in database
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var storedParticipant = await context.GroupParticipants
            .FirstOrDefaultAsync(gp => gp.GroupId == group.Id && gp.UserId == participant.Id);

        Assert.NotNull(storedParticipant);
        Assert.Null(storedParticipant.BudgetSuggestion);
        Assert.Null(storedParticipant.WishlistContent);
        Assert.True(storedParticipant.JoinedAt <= DateTimeOffset.UtcNow);
        Assert.True(storedParticipant.JoinedAt >= DateTimeOffset.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task POST_AcceptInvitation_WithMinimumBudget_Returns201()
    {
        // Arrange
        var (organizer, _) = await CreateAndAuthenticateUser();
        var (_, participantToken) = await CreateAndAuthenticateUser();
        var group = await CreateGroup(organizer);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", participantToken);

        var request = new AcceptInvitationRequest
        {
            BudgetSuggestion = 0.01m // Minimum valid budget
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/invitations/{group.InvitationToken}/accept", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task POST_AcceptInvitation_WithMaximumBudget_Returns201()
    {
        // Arrange
        var (organizer, _) = await CreateAndAuthenticateUser();
        var (_, participantToken) = await CreateAndAuthenticateUser();
        var group = await CreateGroup(organizer);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", participantToken);

        var request = new AcceptInvitationRequest
        {
            BudgetSuggestion = 99999999.99m // Maximum valid budget
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/invitations/{group.InvitationToken}/accept", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    #endregion

    #region Validation Error Tests

    [Fact]
    public async Task POST_AcceptInvitation_WithBudgetBelowMinimum_Returns400()
    {
        // Arrange
        var (organizer, _) = await CreateAndAuthenticateUser();
        var (_, participantToken) = await CreateAndAuthenticateUser();
        var group = await CreateGroup(organizer);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", participantToken);

        var request = new AcceptInvitationRequest
        {
            BudgetSuggestion = 0.00m // Below minimum
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/invitations/{group.InvitationToken}/accept", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problemDetails = await response.Content.ReadAsStringAsync();
        Assert.Contains("Budget suggestion must be between", problemDetails);
    }

    [Fact]
    public async Task POST_AcceptInvitation_WithBudgetAboveMaximum_Returns400()
    {
        // Arrange
        var (organizer, _) = await CreateAndAuthenticateUser();
        var (_, participantToken) = await CreateAndAuthenticateUser();
        var group = await CreateGroup(organizer);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", participantToken);

        var request = new AcceptInvitationRequest
        {
            BudgetSuggestion = 100000000.00m // Above maximum
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/invitations/{group.InvitationToken}/accept", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problemDetails = await response.Content.ReadAsStringAsync();
        Assert.Contains("Budget suggestion must be between", problemDetails);
    }

    [Fact]
    public async Task POST_AcceptInvitation_WithNegativeBudget_Returns400()
    {
        // Arrange
        var (organizer, _) = await CreateAndAuthenticateUser();
        var (_, participantToken) = await CreateAndAuthenticateUser();
        var group = await CreateGroup(organizer);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", participantToken);

        var request = new AcceptInvitationRequest
        {
            BudgetSuggestion = -10.00m
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/invitations/{group.InvitationToken}/accept", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Authentication Error Tests

    [Fact]
    public async Task POST_AcceptInvitation_WithoutAuthentication_Returns401()
    {
        // Arrange
        var (organizer, _) = await CreateAndAuthenticateUser();
        var group = await CreateGroup(organizer);

        var request = new AcceptInvitationRequest();

        // Act
        var response = await _client.PostAsJsonAsync($"/api/invitations/{group.InvitationToken}/accept", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task POST_AcceptInvitation_WithInvalidToken_Returns401()
    {
        // Arrange
        var (organizer, _) = await CreateAndAuthenticateUser();
        var group = await CreateGroup(organizer);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

        var request = new AcceptInvitationRequest();

        // Act
        var response = await _client.PostAsJsonAsync($"/api/invitations/{group.InvitationToken}/accept", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Business Logic Error Tests

    [Fact]
    public async Task POST_AcceptInvitation_WithInvalidInvitationToken_Returns404()
    {
        // Arrange
        var (_, participantToken) = await CreateAndAuthenticateUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", participantToken);

        var invalidToken = Guid.NewGuid();
        var request = new AcceptInvitationRequest();

        // Act
        var response = await _client.PostAsJsonAsync($"/api/invitations/{invalidToken}/accept", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problemDetails = await response.Content.ReadAsStringAsync();
        Assert.Contains("invalid or has expired", problemDetails);
    }

    [Fact]
    public async Task POST_AcceptInvitation_WhenAlreadyParticipant_Returns409()
    {
        // Arrange
        var (organizer, _) = await CreateAndAuthenticateUser("Mike", "Organizer");
        var (participant, participantToken) = await CreateAndAuthenticateUser("Emma", "Participant");
        var group = await CreateGroup(organizer);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", participantToken);

        var request = new AcceptInvitationRequest();

        // Act - First join (should succeed)
        var firstResponse = await _client.PostAsJsonAsync($"/api/invitations/{group.InvitationToken}/accept", request);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        // Act - Second join attempt (should fail with 409)
        var secondResponse = await _client.PostAsJsonAsync($"/api/invitations/{group.InvitationToken}/accept", request);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);

        var problemDetails = await secondResponse.Content.ReadAsStringAsync();
        Assert.Contains("already a participant", problemDetails);
    }

    [Fact]
    public async Task POST_AcceptInvitation_WhenDrawCompleted_Returns410()
    {
        // Arrange
        var (organizer, _) = await CreateAndAuthenticateUser("Chris", "Organizer");
        var (_, participantToken) = await CreateAndAuthenticateUser("Laura", "Participant");
        var group = await CreateGroup(organizer);

        // Mark draw as completed
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var storedGroup = await context.Groups.FindAsync(group.Id);
            storedGroup!.DrawCompletedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync();
        }

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", participantToken);

        var request = new AcceptInvitationRequest();

        // Act
        var response = await _client.PostAsJsonAsync($"/api/invitations/{group.InvitationToken}/accept", request);

        // Assert
        Assert.Equal(HttpStatusCode.Gone, response.StatusCode);

        var problemDetails = await response.Content.ReadAsStringAsync();
        Assert.Contains("already completed the draw", problemDetails);
    }

    #endregion

    #region Participant Count Tests

    [Fact]
    public async Task POST_AcceptInvitation_IncrementsParticipantCount()
    {
        // Arrange
        var (organizer, _) = await CreateAndAuthenticateUser("David", "Organizer");
        var group = await CreateGroup(organizer, "Growing Group");

        // Act - Add 3 participants
        for (int i = 1; i <= 3; i++)
        {
            var (_, token) = await CreateAndAuthenticateUser($"Participant{i}", "User");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.PostAsJsonAsync($"/api/invitations/{group.InvitationToken}/accept", new AcceptInvitationRequest());
            var result = await response.Content.ReadFromJsonAsync<AcceptInvitationResponse>();

            // Assert - Participant count increases correctly
            Assert.Equal(i + 1, result!.ParticipantCount); // +1 for organizer
        }

        // Verify final count in database
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var participantCount = await context.GroupParticipants
            .CountAsync(gp => gp.GroupId == group.Id);

        Assert.Equal(4, participantCount); // Organizer + 3 participants
    }

    [Fact]
    public async Task POST_AcceptInvitation_MultipleUsers_AllSucceed()
    {
        // Arrange
        var (organizer, _) = await CreateAndAuthenticateUser("Helen", "Organizer");
        var group = await CreateGroup(organizer, "Large Group");

        // Act - Add 5 different participants
        for (int i = 1; i <= 5; i++)
        {
            var (_, token) = await CreateAndAuthenticateUser($"User{i}", $"Participant{i}");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.PostAsJsonAsync($"/api/invitations/{group.InvitationToken}/accept", new AcceptInvitationRequest());

            // Assert - All succeed
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        // Verify all participants in database
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var participants = await context.GroupParticipants
            .Where(gp => gp.GroupId == group.Id)
            .ToListAsync();

        Assert.Equal(6, participants.Count); // Organizer + 5 participants
    }

    #endregion
}
