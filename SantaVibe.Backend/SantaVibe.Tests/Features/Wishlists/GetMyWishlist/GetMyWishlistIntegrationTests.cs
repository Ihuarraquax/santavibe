using System;
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
using SantaVibe.Api.Features.Wishlists.GetMyWishlist;
using SantaVibe.Tests.Infrastructure;
using Xunit;

namespace SantaVibe.Tests.Features.Wishlists.GetMyWishlist;

/// <summary>
/// Integration tests for the Get My Wishlist endpoint (GET /api/groups/{groupId}/participants/me/wishlist)
/// </summary>
public class GetMyWishlistIntegrationTests : IClassFixture<SantaVibeWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly SantaVibeWebApplicationFactory _factory;

    public GetMyWishlistIntegrationTests(SantaVibeWebApplicationFactory factory)
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
    public async Task GET_MyWishlist_WithExistingContent_Returns200WithWishlistData()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser("John", "Doe");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = new Group { Name = "Test Group", OrganizerUserId = user.Id };
        var lastModified = DateTimeOffset.UtcNow.AddHours(-2);
        var participant = new GroupParticipant
        {
            Group = group,
            UserId = user.Id,
            WishlistContent = "I would love books about cooking, especially Italian cuisine.",
            WishlistLastModified = lastModified
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.Add(participant);
            await context.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}/participants/me/wishlist");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GetMyWishlistResponse>();
        Assert.NotNull(result);
        Assert.Equal(group.Id, result.GroupId);
        Assert.Equal("I would love books about cooking, especially Italian cuisine.", result.WishlistContent);
        Assert.NotNull(result.LastModified);
        // Allow small precision difference due to database storage
        Assert.True(Math.Abs((result.LastModified.Value - lastModified).TotalMilliseconds) < 10);
    }

    [Fact]
    public async Task GET_MyWishlist_WithEmptyWishlist_Returns200WithNullValues()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser("Jane", "Smith");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = new Group { Name = "Test Group", OrganizerUserId = user.Id };
        var participant = new GroupParticipant
        {
            Group = group,
            UserId = user.Id,
            WishlistContent = null,
            WishlistLastModified = null
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.Add(participant);
            await context.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}/participants/me/wishlist");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GetMyWishlistResponse>();
        Assert.NotNull(result);
        Assert.Equal(group.Id, result.GroupId);
        Assert.Null(result.WishlistContent);
        Assert.Null(result.LastModified);
    }

    [Fact]
    public async Task GET_MyWishlist_WithEmptyString_ReturnsEmptyString()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser("Bob", "Johnson");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = new Group { Name = "Test Group", OrganizerUserId = user.Id };
        var lastModified = DateTimeOffset.UtcNow.AddDays(-1);
        var participant = new GroupParticipant
        {
            Group = group,
            UserId = user.Id,
            WishlistContent = "",
            WishlistLastModified = lastModified
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.Add(participant);
            await context.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}/participants/me/wishlist");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GetMyWishlistResponse>();
        Assert.NotNull(result);
        Assert.Equal(group.Id, result.GroupId);
        Assert.Equal("", result.WishlistContent);
        Assert.NotNull(result.LastModified);
    }

    [Fact]
    public async Task GET_MyWishlist_AsParticipantNotOrganizer_ReturnsOwnWishlist()
    {
        // Arrange
        var (organizer, _) = await CreateAndAuthenticateUser("Organizer", "User");
        var (participant, participantToken) = await CreateAndAuthenticateUser("Regular", "Participant");

        var group = new Group { Name = "Test Group", OrganizerUserId = organizer.Id };
        var organizerParticipant = new GroupParticipant
        {
            Group = group,
            UserId = organizer.Id,
            WishlistContent = "Organizer's wishlist"
        };
        var regularParticipant = new GroupParticipant
        {
            Group = group,
            UserId = participant.Id,
            WishlistContent = "Participant's wishlist",
            WishlistLastModified = DateTimeOffset.UtcNow.AddHours(-3)
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.AddRange(organizerParticipant, regularParticipant);
            await context.SaveChangesAsync();
        }

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", participantToken);

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}/participants/me/wishlist");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GetMyWishlistResponse>();
        Assert.NotNull(result);
        Assert.Equal("Participant's wishlist", result.WishlistContent);
        Assert.NotEqual("Organizer's wishlist", result.WishlistContent); // Should not see organizer's wishlist
    }

    #endregion

    #region Authentication and Authorization Tests

    [Fact]
    public async Task GET_MyWishlist_WithoutAuthentication_Returns401()
    {
        // Arrange
        var groupId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/groups/{groupId}/participants/me/wishlist");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GET_MyWishlist_WithInvalidToken_Returns401()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
        var groupId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/groups/{groupId}/participants/me/wishlist");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GET_MyWishlist_AsNonParticipant_ReturnsForbiddenError()
    {
        // Arrange
        var (organizer, _) = await CreateAndAuthenticateUser("Organizer", "User");
        var (nonParticipant, nonParticipantToken) = await CreateAndAuthenticateUser("NonParticipant", "User");

        var group = new Group { Name = "Test Group", OrganizerUserId = organizer.Id };
        var participant = new GroupParticipant
        {
            Group = group,
            UserId = organizer.Id,
            WishlistContent = "Organizer's private wishlist"
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.Add(participant);
            await context.SaveChangesAsync();
        }

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", nonParticipantToken);

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}/participants/me/wishlist");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var problemDetails = await response.Content.ReadAsStringAsync();
        Assert.Contains("NotParticipant", problemDetails);
        Assert.Contains("not a participant", problemDetails);
    }

    #endregion

    #region Error Tests

    [Fact]
    public async Task GET_MyWishlist_ForNonExistentGroup_ReturnsNotFoundError()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var nonExistentGroupId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/groups/{nonExistentGroupId}/participants/me/wishlist");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problemDetails = await response.Content.ReadAsStringAsync();
        Assert.Contains("GroupNotFound", problemDetails);
        Assert.Contains("Group not found", problemDetails);
    }

    [Fact]
    public async Task GET_MyWishlist_WithInvalidGroupIdFormat_Returns400()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync($"/api/groups/invalid-uuid/participants/me/wishlist");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Data Integrity Tests

    [Fact]
    public async Task GET_MyWishlist_DoesNotModifyDatabase()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser("Alice", "Wonder");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = new Group { Name = "Test Group", OrganizerUserId = user.Id };
        var lastModified = DateTimeOffset.UtcNow.AddHours(-5);
        var participant = new GroupParticipant
        {
            Group = group,
            UserId = user.Id,
            WishlistContent = "Original content",
            WishlistLastModified = lastModified
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.Add(participant);
            await context.SaveChangesAsync();
        }

        // Act - Make multiple GET requests
        await _client.GetAsync($"/api/groups/{group.Id}/participants/me/wishlist");
        await _client.GetAsync($"/api/groups/{group.Id}/participants/me/wishlist");
        await _client.GetAsync($"/api/groups/{group.Id}/participants/me/wishlist");

        // Assert - Verify database was NOT modified
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var unchangedParticipant = await context.GroupParticipants
                .FirstOrDefaultAsync(gp => gp.GroupId == group.Id && gp.UserId == user.Id);

            Assert.NotNull(unchangedParticipant);
            Assert.Equal("Original content", unchangedParticipant.WishlistContent);
            // Allow small precision difference due to database storage
            Assert.NotNull(unchangedParticipant.WishlistLastModified);
            Assert.True(Math.Abs((unchangedParticipant.WishlistLastModified.Value - lastModified).TotalMilliseconds) < 10);
        }
    }

    [Fact]
    public async Task GET_MyWishlist_ReturnsConsistentDataOnMultipleRequests()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser("Charlie", "Brown");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = new Group { Name = "Test Group", OrganizerUserId = user.Id };
        var lastModified = DateTimeOffset.UtcNow.AddDays(-2);
        var participant = new GroupParticipant
        {
            Group = group,
            UserId = user.Id,
            WishlistContent = "Consistent content",
            WishlistLastModified = lastModified
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.Add(participant);
            await context.SaveChangesAsync();
        }

        // Act - Make multiple GET requests
        var response1 = await _client.GetAsync($"/api/groups/{group.Id}/participants/me/wishlist");
        var result1 = await response1.Content.ReadFromJsonAsync<GetMyWishlistResponse>();

        var response2 = await _client.GetAsync($"/api/groups/{group.Id}/participants/me/wishlist");
        var result2 = await response2.Content.ReadFromJsonAsync<GetMyWishlistResponse>();

        var response3 = await _client.GetAsync($"/api/groups/{group.Id}/participants/me/wishlist");
        var result3 = await response3.Content.ReadFromJsonAsync<GetMyWishlistResponse>();

        // Assert - All responses should be identical
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotNull(result3);

        Assert.Equal(result1.GroupId, result2.GroupId);
        Assert.Equal(result1.GroupId, result3.GroupId);

        Assert.Equal(result1.WishlistContent, result2.WishlistContent);
        Assert.Equal(result1.WishlistContent, result3.WishlistContent);

        Assert.Equal(result1.LastModified, result2.LastModified);
        Assert.Equal(result1.LastModified, result3.LastModified);
    }

    #endregion

    #region Business Logic Tests

    [Fact]
    public async Task GET_MyWishlist_BeforeAndAfterDraw_ReturnsSameData()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser("Santa", "Claus");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = new Group
        {
            Name = "Test Group",
            OrganizerUserId = user.Id,
            DrawCompletedAt = null // No draw yet
        };
        var participant = new GroupParticipant
        {
            Group = group,
            UserId = user.Id,
            WishlistContent = "Same wishlist content",
            WishlistLastModified = DateTimeOffset.UtcNow.AddHours(-1)
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.Add(participant);
            await context.SaveChangesAsync();
        }

        // Act - Get wishlist before draw
        var response1 = await _client.GetAsync($"/api/groups/{group.Id}/participants/me/wishlist");
        var result1 = await response1.Content.ReadFromJsonAsync<GetMyWishlistResponse>();

        // Simulate draw completion
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var groupToUpdate = await context.Groups.FindAsync(group.Id);
            groupToUpdate!.DrawCompletedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync();
        }

        // Act - Get wishlist after draw
        var response2 = await _client.GetAsync($"/api/groups/{group.Id}/participants/me/wishlist");
        var result2 = await response2.Content.ReadFromJsonAsync<GetMyWishlistResponse>();

        // Assert - Should return same data regardless of draw status
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        Assert.Equal(result1.WishlistContent, result2.WishlistContent);
        Assert.Equal(result1.LastModified, result2.LastModified);
    }

    #endregion
}
