using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SantaVibe.Api.Data;
using SantaVibe.Api.Data.Entities;
using SantaVibe.Api.Features.Authentication.Login;
using SantaVibe.Api.Features.Groups.GetRecipientWishlist;
using SantaVibe.Tests.Infrastructure;

namespace SantaVibe.Tests.Features.Groups.GetRecipientWishlist;

/// <summary>
/// Integration tests for the Get Recipient Wishlist endpoint (GET /api/groups/{groupId}/my-assignment/wishlist).
/// Tests authorization checks and wishlist retrieval for Secret Santa recipients.
/// </summary>
public class GetRecipientWishlistIntegrationTests : IClassFixture<SantaVibeWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly SantaVibeWebApplicationFactory _factory;

    public GetRecipientWishlistIntegrationTests(SantaVibeWebApplicationFactory factory)
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
        List<string> participantUserIds,
        Dictionary<string, string?>? wishlistContents = null)
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

        // Add all participants with optional wishlist content
        foreach (var userId in participantUserIds)
        {
            var wishlistContent = wishlistContents?.GetValueOrDefault(userId);
            context.GroupParticipants.Add(new GroupParticipant
            {
                GroupId = group.Id,
                UserId = userId,
                JoinedAt = DateTimeOffset.UtcNow,
                WishlistContent = wishlistContent,
                WishlistLastModified = wishlistContent != null ? DateTimeOffset.UtcNow : null
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
    public async Task GET_RecipientWishlist_WithWishlistContent_Returns200()
    {
        // Arrange
        var (organizer, organizerToken) = await CreateAndAuthenticateUser("Alice", "Smith");
        var (participant1, participant1Token) = await CreateAndAuthenticateUser("Bob", "Jones");
        var (participant2, _) = await CreateAndAuthenticateUser("Carol", "Davis");

        // Setup: organizer -> participant1 (has wishlist) -> participant2 -> organizer
        var wishlists = new Dictionary<string, string?>
        {
            { organizer.Id, null },
            { participant1.Id, "I love mystery novels, especially Agatha Christie. I also enjoy gardening tools." },
            { participant2.Id, null }
        };

        var group = await CreateGroupWithDrawCompleted(
            organizer.Id,
            new List<string> { organizer.Id, participant1.Id, participant2.Id },
            wishlists);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", organizerToken);

        // Act - Organizer requests their recipient's (participant1) wishlist
        var response = await _client.GetAsync($"/api/groups/{group.Id}/my-assignment/wishlist");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GetRecipientWishlistResponse>();
        Assert.NotNull(result);
        Assert.Equal(group.Id, result.GroupId);
        Assert.Equal(participant1.Id, result.RecipientId);
        Assert.Equal("Bob", result.RecipientFirstName);
        Assert.Equal("Jones", result.RecipientLastName);
        Assert.Equal("I love mystery novels, especially Agatha Christie. I also enjoy gardening tools.", result.WishlistContent);
        Assert.NotNull(result.LastModified);
    }

    [Fact]
    public async Task GET_RecipientWishlist_WithEmptyWishlist_Returns200WithNullContent()
    {
        // Arrange
        var (organizer, _) = await CreateAndAuthenticateUser("Alice", "Smith");
        var (participant1, participant1Token) = await CreateAndAuthenticateUser("Bob", "Jones");
        var (participant2, _) = await CreateAndAuthenticateUser("Carol", "Davis");

        // Setup: participant2 has NO wishlist
        var wishlists = new Dictionary<string, string?>
        {
            { organizer.Id, null },
            { participant1.Id, "Some content" },
            { participant2.Id, null }  // No wishlist
        };

        var group = await CreateGroupWithDrawCompleted(
            organizer.Id,
            new List<string> { organizer.Id, participant1.Id, participant2.Id },
            wishlists);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", participant1Token);

        // Act - participant1 requests their recipient's (participant2) wishlist
        var response = await _client.GetAsync($"/api/groups/{group.Id}/my-assignment/wishlist");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GetRecipientWishlistResponse>();
        Assert.NotNull(result);
        Assert.Equal(group.Id, result.GroupId);
        Assert.Equal(participant2.Id, result.RecipientId);
        Assert.Equal("Carol", result.RecipientFirstName);
        Assert.Equal("Davis", result.RecipientLastName);
        Assert.Null(result.WishlistContent);  // Empty wishlist
        Assert.Null(result.LastModified);      // No modification date
    }

    [Fact]
    public async Task GET_RecipientWishlist_WithoutAuthentication_Returns401()
    {
        // Arrange
        var (organizer, _) = await CreateAndAuthenticateUser("Alice", "Smith");
        var (participant1, _) = await CreateAndAuthenticateUser("Bob", "Jones");

        var group = await CreateGroupWithDrawCompleted(
            organizer.Id,
            new List<string> { organizer.Id, participant1.Id });

        // Don't set authorization header

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}/my-assignment/wishlist");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GET_RecipientWishlist_ForNonExistentGroup_Returns404()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser("Alice", "Smith");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var nonExistentGroupId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/groups/{nonExistentGroupId}/my-assignment/wishlist");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GET_RecipientWishlist_WhenNotAParticipant_Returns403()
    {
        // Arrange
        var (organizer, _) = await CreateAndAuthenticateUser("Alice", "Smith");
        var (participant1, _) = await CreateAndAuthenticateUser("Bob", "Jones");
        var (nonParticipant, nonParticipantToken) = await CreateAndAuthenticateUser("Charlie", "Wilson");

        var wishlists = new Dictionary<string, string?>
        {
            { organizer.Id, null },
            { participant1.Id, "Some wishlist" }
        };

        var group = await CreateGroupWithDrawCompleted(
            organizer.Id,
            new List<string> { organizer.Id, participant1.Id },
            wishlists);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", nonParticipantToken);

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}/my-assignment/wishlist");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GET_RecipientWishlist_WhenDrawNotCompleted_Returns403()
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
            JoinedAt = DateTimeOffset.UtcNow,
            WishlistContent = "Some content"
        });

        context.GroupParticipants.Add(new GroupParticipant
        {
            GroupId = group.Id,
            UserId = participant1.Id,
            JoinedAt = DateTimeOffset.UtcNow,
            WishlistContent = "Another wishlist"
        });

        await context.SaveChangesAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", organizerToken);

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}/my-assignment/wishlist");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GET_RecipientWishlist_WhenNoAssignment_Returns404()
    {
        // Arrange - This is an edge case that shouldn't happen but we handle it
        var (organizer, organizerToken) = await CreateAndAuthenticateUser("Alice", "Smith");
        var (participant1, _) = await CreateAndAuthenticateUser("Bob", "Jones");

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Create group WITH draw completed but NO assignments (invalid state)
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = $"Test Group {Guid.NewGuid()}",
            OrganizerUserId = organizer.Id,
            InvitationToken = Guid.NewGuid(),
            Budget = 100m,
            DrawCompletedAt = DateTimeOffset.UtcNow, // Draw marked as completed
            CreatedAt = DateTimeOffset.UtcNow
        };

        context.Groups.Add(group);

        context.GroupParticipants.Add(new GroupParticipant
        {
            GroupId = group.Id,
            UserId = organizer.Id,
            JoinedAt = DateTimeOffset.UtcNow,
            WishlistContent = "Some content"
        });

        context.GroupParticipants.Add(new GroupParticipant
        {
            GroupId = group.Id,
            UserId = participant1.Id,
            JoinedAt = DateTimeOffset.UtcNow,
            WishlistContent = "Another wishlist"
        });

        // NOTE: No assignments created
        await context.SaveChangesAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", organizerToken);

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}/my-assignment/wishlist");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GET_RecipientWishlist_MultipleParticipants_EachSeesOnlyTheirRecipientWishlist()
    {
        // Arrange
        var (organizer, organizerToken) = await CreateAndAuthenticateUser("Alice", "Smith");
        var (participant1, participant1Token) = await CreateAndAuthenticateUser("Bob", "Jones");
        var (participant2, participant2Token) = await CreateAndAuthenticateUser("Carol", "Davis");

        var wishlists = new Dictionary<string, string?>
        {
            { organizer.Id, "Alice's wishlist" },
            { participant1.Id, "Bob's wishlist" },
            { participant2.Id, "Carol's wishlist" }
        };

        // Circular assignment: organizer -> participant1 -> participant2 -> organizer
        var group = await CreateGroupWithDrawCompleted(
            organizer.Id,
            new List<string> { organizer.Id, participant1.Id, participant2.Id },
            wishlists);

        // Act & Assert - Each participant sees different recipient's wishlist
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", organizerToken);
        var organizerResponse = await _client.GetAsync($"/api/groups/{group.Id}/my-assignment/wishlist");
        var organizerResult = await organizerResponse.Content.ReadFromJsonAsync<GetRecipientWishlistResponse>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", participant1Token);
        var participant1Response = await _client.GetAsync($"/api/groups/{group.Id}/my-assignment/wishlist");
        var participant1Result = await participant1Response.Content.ReadFromJsonAsync<GetRecipientWishlistResponse>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", participant2Token);
        var participant2Response = await _client.GetAsync($"/api/groups/{group.Id}/my-assignment/wishlist");
        var participant2Result = await participant2Response.Content.ReadFromJsonAsync<GetRecipientWishlistResponse>();

        // All three should get different recipients' wishlists
        Assert.Equal(HttpStatusCode.OK, organizerResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, participant1Response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, participant2Response.StatusCode);

        // Verify each sees their correct recipient
        Assert.Equal(participant1.Id, organizerResult!.RecipientId);
        Assert.Equal("Bob's wishlist", organizerResult.WishlistContent);

        Assert.Equal(participant2.Id, participant1Result!.RecipientId);
        Assert.Equal("Carol's wishlist", participant1Result.WishlistContent);

        Assert.Equal(organizer.Id, participant2Result!.RecipientId);
        Assert.Equal("Alice's wishlist", participant2Result.WishlistContent);
    }
}
