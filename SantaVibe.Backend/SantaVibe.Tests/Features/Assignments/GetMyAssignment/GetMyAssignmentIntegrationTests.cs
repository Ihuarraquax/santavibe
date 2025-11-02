using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SantaVibe.Api.Data;
using SantaVibe.Api.Data.Entities;
using SantaVibe.Api.Features.Assignments.GetMyAssignment;
using SantaVibe.Api.Features.Authentication.Login;
using SantaVibe.Tests.Infrastructure;

namespace SantaVibe.Tests.Features.Assignments.GetMyAssignment;

/// <summary>
/// Integration tests for the Get My Assignment endpoint (GET /api/groups/{groupId}/my-assignment)
/// </summary>
public class GetMyAssignmentIntegrationTests : IClassFixture<SantaVibeWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly SantaVibeWebApplicationFactory _factory;

    public GetMyAssignmentIntegrationTests(SantaVibeWebApplicationFactory factory)
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

    private async Task<Group> CreateGroupWithDrawCompleted(
        string organizerUserId,
        List<string> participantUserIds)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = $"Test Group {Guid.NewGuid()}",
            OrganizerUserId = organizerUserId,
            InvitationToken = Guid.NewGuid(),
            Budget = 100m,
            DrawCompletedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };

        context.Groups.Add(group);

        // Add all participants
        foreach (var userId in participantUserIds)
        {
            context.GroupParticipants.Add(new GroupParticipant
            {
                GroupId = group.Id,
                UserId = userId,
                JoinedAt = DateTimeOffset.UtcNow,
                WishlistContent = userId == participantUserIds[1] ? "Some wishlist content" : null,
                WishlistLastModified = userId == participantUserIds[1] ? DateTimeOffset.UtcNow : null
            });
        }

        // Create assignments (simple circular assignment for testing)
        for (int i = 0; i < participantUserIds.Count; i++)
        {
            var nextIndex = (i + 1) % participantUserIds.Count;
            context.Assignments.Add(new Assignment
            {
                Id = Guid.NewGuid(),
                GroupId = group.Id,
                SantaUserId = participantUserIds[i],
                RecipientUserId = participantUserIds[nextIndex],
                AssignedAt = DateTimeOffset.UtcNow
            });
        }

        await context.SaveChangesAsync();
        return group;
    }

    [Fact]
    public async Task GET_MyAssignment_AsParticipant_Returns200()
    {
        // Arrange
        var (organizer, organizerToken) = await CreateAndAuthenticateUser("Alice", "Smith");
        var (participant1, participant1Token) = await CreateAndAuthenticateUser("Bob", "Jones");
        var (participant2, _) = await CreateAndAuthenticateUser("Carol", "Davis");

        var group = await CreateGroupWithDrawCompleted(
            organizer.Id,
            new List<string> { organizer.Id, participant1.Id, participant2.Id });

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", participant1Token);

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}/my-assignment");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GetMyAssignmentResponse>();
        Assert.NotNull(result);
        Assert.Equal(group.Id, result.GroupId);
        Assert.Equal(group.Name, result.GroupName);
        Assert.Equal(100m, result.Budget);
        Assert.NotNull(result.DrawCompletedAt);
        Assert.NotNull(result.Recipient);
        Assert.Equal(participant2.Id, result.Recipient.UserId);
        Assert.Equal("Carol", result.Recipient.FirstName);
        Assert.Equal("Davis", result.Recipient.LastName);
    }

    [Fact]
    public async Task GET_MyAssignment_WithRecipientHavingWishlist_ReturnsWishlistMetadata()
    {
        // Arrange
        var (organizer, organizerToken) = await CreateAndAuthenticateUser("Alice", "Smith");
        var (participant1, _) = await CreateAndAuthenticateUser("Bob", "Jones");
        var (participant2, _) = await CreateAndAuthenticateUser("Carol", "Davis");

        // Circular assignment: organizer -> participant1 -> participant2 -> organizer
        // So organizer's recipient is participant1, who has a wishlist
        var group = await CreateGroupWithDrawCompleted(
            organizer.Id,
            new List<string> { organizer.Id, participant1.Id, participant2.Id });

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", organizerToken);

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}/my-assignment");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GetMyAssignmentResponse>();
        Assert.NotNull(result);
        Assert.Equal(participant1.Id, result.Recipient.UserId); // Organizer gets participant1
        Assert.True(result.Recipient.HasWishlist); // participant1 has wishlist
        Assert.NotNull(result.Recipient.WishlistLastModified);
    }

    [Fact]
    public async Task GET_MyAssignment_WithoutAuthentication_Returns401()
    {
        // Arrange
        var (organizer, _) = await CreateAndAuthenticateUser("Alice", "Smith");
        var (participant1, _) = await CreateAndAuthenticateUser("Bob", "Jones");

        var group = await CreateGroupWithDrawCompleted(
            organizer.Id,
            new List<string> { organizer.Id, participant1.Id });

        // Don't set authorization header

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}/my-assignment");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GET_MyAssignment_ForNonExistentGroup_Returns404()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser("Alice", "Smith");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var nonExistentGroupId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/groups/{nonExistentGroupId}/my-assignment");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GET_MyAssignment_WhenDrawNotCompleted_Returns403()
    {
        // Arrange
        var (organizer, organizerToken) = await CreateAndAuthenticateUser("Alice", "Smith");
        var (participant1, _) = await CreateAndAuthenticateUser("Bob", "Jones");

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Create group WITHOUT completing draw
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = $"Test Group {Guid.NewGuid()}",
            OrganizerUserId = organizer.Id,
            InvitationToken = Guid.NewGuid(),
            Budget = null,
            DrawCompletedAt = null, // Draw not completed
            CreatedAt = DateTimeOffset.UtcNow
        };

        context.Groups.Add(group);

        context.GroupParticipants.Add(new GroupParticipant
        {
            GroupId = group.Id,
            UserId = organizer.Id,
            JoinedAt = DateTimeOffset.UtcNow
        });

        context.GroupParticipants.Add(new GroupParticipant
        {
            GroupId = group.Id,
            UserId = participant1.Id,
            JoinedAt = DateTimeOffset.UtcNow
        });

        await context.SaveChangesAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", organizerToken);

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}/my-assignment");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GET_MyAssignment_WhenNotAParticipant_Returns403()
    {
        // Arrange
        var (organizer, _) = await CreateAndAuthenticateUser("Alice", "Smith");
        var (participant1, _) = await CreateAndAuthenticateUser("Bob", "Jones");
        var (nonParticipant, nonParticipantToken) = await CreateAndAuthenticateUser("Charlie", "Wilson");

        var group = await CreateGroupWithDrawCompleted(
            organizer.Id,
            new List<string> { organizer.Id, participant1.Id });

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", nonParticipantToken);

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}/my-assignment");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GET_MyAssignment_AsOrganizer_ReturnsOwnAssignment()
    {
        // Arrange
        var (organizer, organizerToken) = await CreateAndAuthenticateUser("Alice", "Smith");
        var (participant1, _) = await CreateAndAuthenticateUser("Bob", "Jones");
        var (participant2, _) = await CreateAndAuthenticateUser("Carol", "Davis");

        var group = await CreateGroupWithDrawCompleted(
            organizer.Id,
            new List<string> { organizer.Id, participant1.Id, participant2.Id });

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", organizerToken);

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}/my-assignment");

        // Assert - Organizer has no special privileges, sees only their assignment
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GetMyAssignmentResponse>();
        Assert.NotNull(result);
        Assert.Equal(participant1.Id, result.Recipient.UserId); // Organizer's recipient (circular assignment)
    }

    [Fact]
    public async Task GET_MyAssignment_MultipleParticipants_EachSeesOnlyTheirOwnAssignment()
    {
        // Arrange
        var (organizer, organizerToken) = await CreateAndAuthenticateUser("Alice", "Smith");
        var (participant1, participant1Token) = await CreateAndAuthenticateUser("Bob", "Jones");
        var (participant2, participant2Token) = await CreateAndAuthenticateUser("Carol", "Davis");

        var group = await CreateGroupWithDrawCompleted(
            organizer.Id,
            new List<string> { organizer.Id, participant1.Id, participant2.Id });

        // Act & Assert - Each participant sees different recipient
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", organizerToken);
        var organizerResponse = await _client.GetAsync($"/api/groups/{group.Id}/my-assignment");
        var organizerResult = await organizerResponse.Content.ReadFromJsonAsync<GetMyAssignmentResponse>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", participant1Token);
        var participant1Response = await _client.GetAsync($"/api/groups/{group.Id}/my-assignment");
        var participant1Result = await participant1Response.Content.ReadFromJsonAsync<GetMyAssignmentResponse>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", participant2Token);
        var participant2Response = await _client.GetAsync($"/api/groups/{group.Id}/my-assignment");
        var participant2Result = await participant2Response.Content.ReadFromJsonAsync<GetMyAssignmentResponse>();

        // All three should get different recipients due to circular assignment
        Assert.NotEqual(organizerResult!.Recipient.UserId, participant1Result!.Recipient.UserId);
        Assert.NotEqual(participant1Result.Recipient.UserId, participant2Result!.Recipient.UserId);
        Assert.NotEqual(participant2Result.Recipient.UserId, organizerResult.Recipient.UserId);
    }
}
