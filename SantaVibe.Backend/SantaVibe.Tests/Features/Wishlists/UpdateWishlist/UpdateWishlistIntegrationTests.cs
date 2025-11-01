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
using SantaVibe.Api.Features.Wishlists.UpdateWishlist;
using SantaVibe.Tests.Infrastructure;
using Xunit;

namespace SantaVibe.Tests.Features.Wishlists.UpdateWishlist;

/// <summary>
/// Integration tests for the Update Wishlist endpoint (PUT /api/groups/{groupId}/participants/me/wishlist)
/// </summary>
public class UpdateWishlistIntegrationTests : IClassFixture<SantaVibeWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly SantaVibeWebApplicationFactory _factory;

    public UpdateWishlistIntegrationTests(SantaVibeWebApplicationFactory factory)
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
    public async Task PUT_Wishlist_WithValidRequest_Returns200WithCorrectData()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser("John", "Doe");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = new Group { Name = "Test Group", OrganizerUserId = user.Id, DrawCompletedAt = DateTimeOffset.UtcNow.AddHours(-1) };
        var participant = new GroupParticipant { Group = group, UserId = user.Id };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.Add(participant);
            await context.SaveChangesAsync();
        }

        var request = new UpdateWishlistRequest
        {
            WishlistContent = "I would love a new book or board game!"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/groups/{group.Id}/participants/me/wishlist", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<UpdateWishlistResponse>();
        Assert.NotNull(result);
        Assert.Equal(group.Id, result.GroupId);
        Assert.Equal("I would love a new book or board game!", result.WishlistContent);
        Assert.True(result.LastModified <= DateTimeOffset.UtcNow);
        Assert.True(result.LastModified >= DateTimeOffset.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task PUT_Wishlist_WithNullContent_ClearsWishlist()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser("Jane", "Smith");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = new Group { Name = "Test Group", OrganizerUserId = user.Id, DrawCompletedAt = DateTimeOffset.UtcNow.AddHours(-1) };
        var participant = new GroupParticipant
        {
            Group = group,
            UserId = user.Id,
            WishlistContent = "Old wishlist content"
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.Add(participant);
            await context.SaveChangesAsync();
        }

        var request = new UpdateWishlistRequest
        {
            WishlistContent = null
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/groups/{group.Id}/participants/me/wishlist", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<UpdateWishlistResponse>();
        Assert.NotNull(result);
        Assert.Null(result.WishlistContent);
    }

    [Fact]
    public async Task PUT_Wishlist_UpdatesLastModifiedTimestamp()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser("Bob", "Johnson");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var oldTimestamp = DateTimeOffset.UtcNow.AddHours(-1);
        var group = new Group { Name = "Test Group", OrganizerUserId = user.Id, DrawCompletedAt = DateTimeOffset.UtcNow.AddHours(-2) };
        var participant = new GroupParticipant
        {
            Group = group,
            UserId = user.Id,
            WishlistContent = "Old content",
            WishlistLastModified = oldTimestamp
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.Add(participant);
            await context.SaveChangesAsync();
        }

        var request = new UpdateWishlistRequest
        {
            WishlistContent = "New content"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/groups/{group.Id}/participants/me/wishlist", request);

        // Assert
        var result = await response.Content.ReadFromJsonAsync<UpdateWishlistResponse>();
        Assert.NotNull(result);
        Assert.True(result.LastModified > oldTimestamp);
        Assert.True(result.LastModified <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task PUT_Wishlist_UpdatesDatabaseCorrectly()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser("Alice", "Wonder");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = new Group { Name = "Test Group", OrganizerUserId = user.Id, DrawCompletedAt = DateTimeOffset.UtcNow.AddHours(-1) };
        var participant = new GroupParticipant { Group = group, UserId = user.Id };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.Add(participant);
            await context.SaveChangesAsync();
        }

        var request = new UpdateWishlistRequest
        {
            WishlistContent = "Board games, books, or puzzles"
        };

        // Act
        await _client.PutAsJsonAsync($"/api/groups/{group.Id}/participants/me/wishlist", request);

        // Assert - Verify database was updated
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var updatedParticipant = await context.GroupParticipants
                .FirstOrDefaultAsync(gp => gp.GroupId == group.Id && gp.UserId == user.Id);

            Assert.NotNull(updatedParticipant);
            Assert.Equal("Board games, books, or puzzles", updatedParticipant.WishlistContent);
            Assert.NotNull(updatedParticipant.WishlistLastModified);
            Assert.True(updatedParticipant.WishlistLastModified <= DateTimeOffset.UtcNow);
        }
    }

    [Fact]
    public async Task PUT_Wishlist_WithEmptyString_SavesEmptyString()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = new Group { Name = "Test Group", OrganizerUserId = user.Id, DrawCompletedAt = DateTimeOffset.UtcNow.AddHours(-1) };
        var participant = new GroupParticipant
        {
            Group = group,
            UserId = user.Id,
            WishlistContent = "Some content"
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.Add(participant);
            await context.SaveChangesAsync();
        }

        var request = new UpdateWishlistRequest
        {
            WishlistContent = ""
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/groups/{group.Id}/participants/me/wishlist", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<UpdateWishlistResponse>();
        Assert.NotNull(result);
        Assert.Equal("", result.WishlistContent);
    }

    #endregion

    #region Authentication and Authorization Tests

    [Fact]
    public async Task PUT_Wishlist_WithoutAuthentication_Returns401()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var request = new UpdateWishlistRequest
        {
            WishlistContent = "Test wishlist"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/groups/{groupId}/participants/me/wishlist", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PUT_Wishlist_WithInvalidToken_Returns401()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

        var groupId = Guid.NewGuid();
        var request = new UpdateWishlistRequest
        {
            WishlistContent = "Test wishlist"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/groups/{groupId}/participants/me/wishlist", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PUT_Wishlist_AsNonParticipant_ReturnsForbiddenError()
    {
        // Arrange
        var (organizer, _) = await CreateAndAuthenticateUser("Organizer", "User");
        var (nonParticipant, nonParticipantToken) = await CreateAndAuthenticateUser("NonParticipant", "User");

        var group = new Group { Name = "Test Group", OrganizerUserId = organizer.Id };
        var participant = new GroupParticipant { Group = group, UserId = organizer.Id };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.Add(participant);
            await context.SaveChangesAsync();
        }

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", nonParticipantToken);

        var request = new UpdateWishlistRequest
        {
            WishlistContent = "Test wishlist"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/groups/{group.Id}/participants/me/wishlist", request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var problemDetails = await response.Content.ReadAsStringAsync();
        Assert.Contains("NotParticipant", problemDetails);
        Assert.Contains("not a participant", problemDetails);
    }

    #endregion

    #region Error Tests

    [Fact]
    public async Task PUT_Wishlist_ForNonExistentGroup_ReturnsNotFoundError()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var nonExistentGroupId = Guid.NewGuid();
        var request = new UpdateWishlistRequest
        {
            WishlistContent = "Test wishlist"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/groups/{nonExistentGroupId}/participants/me/wishlist", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problemDetails = await response.Content.ReadAsStringAsync();
        Assert.Contains("GroupNotFound", problemDetails);
        Assert.Contains("Group not found", problemDetails);
    }

    #endregion

    #region Business Logic Tests

    [Fact]
    public async Task PUT_Wishlist_WhenDrawCompleted_DoesNotError()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser("Santa", "Claus");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = new Group
        {
            Name = "Test Group",
            OrganizerUserId = user.Id,
            DrawCompletedAt = DateTimeOffset.UtcNow.AddHours(-1)
        };
        var participant = new GroupParticipant { Group = group, UserId = user.Id };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.Add(participant);
            await context.SaveChangesAsync();
        }

        var request = new UpdateWishlistRequest
        {
            WishlistContent = "Updated wishlist after draw"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/groups/{group.Id}/participants/me/wishlist", request);

        // Assert - Should succeed and notification would be published (tested in unit tests)
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<UpdateWishlistResponse>();
        Assert.NotNull(result);
        Assert.Equal("Updated wishlist after draw", result.WishlistContent);
    }

    [Fact]
    public async Task PUT_Wishlist_WhenDrawNotCompleted_Returns403()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser("Elf", "Helper");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = new Group
        {
            Name = "Test Group",
            OrganizerUserId = user.Id,
            DrawCompletedAt = null // Draw not completed
        };
        var participant = new GroupParticipant { Group = group, UserId = user.Id };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.Add(participant);
            await context.SaveChangesAsync();
        }

        var request = new UpdateWishlistRequest
        {
            WishlistContent = "Wishlist before draw"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/groups/{group.Id}/participants/me/wishlist", request);

        // Assert - Should return 403 Forbidden since draw not completed (per PRD FR-004 and US-022)
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var problemDetails = await response.Content.ReadAsStringAsync();
        Assert.Contains("DrawNotCompleted", problemDetails);
        Assert.Contains("Wishlist can only be created/modified after the draw has been completed", problemDetails);
    }

    [Fact]
    public async Task PUT_Wishlist_MultipleUpdates_UpdatesLastModifiedEachTime()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser("Rudolph", "Reindeer");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var group = new Group { Name = "Test Group", OrganizerUserId = user.Id, DrawCompletedAt = DateTimeOffset.UtcNow.AddHours(-1) };
        var participant = new GroupParticipant { Group = group, UserId = user.Id };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.Add(participant);
            await context.SaveChangesAsync();
        }

        // Act - Make multiple updates
        var request1 = new UpdateWishlistRequest { WishlistContent = "First update" };
        var response1 = await _client.PutAsJsonAsync($"/api/groups/{group.Id}/participants/me/wishlist", request1);
        var result1 = await response1.Content.ReadFromJsonAsync<UpdateWishlistResponse>();

        await Task.Delay(100); // Small delay to ensure timestamp difference

        var request2 = new UpdateWishlistRequest { WishlistContent = "Second update" };
        var response2 = await _client.PutAsJsonAsync($"/api/groups/{group.Id}/participants/me/wishlist", request2);
        var result2 = await response2.Content.ReadFromJsonAsync<UpdateWishlistResponse>();

        await Task.Delay(100); // Small delay to ensure timestamp difference

        var request3 = new UpdateWishlistRequest { WishlistContent = "Third update" };
        var response3 = await _client.PutAsJsonAsync($"/api/groups/{group.Id}/participants/me/wishlist", request3);
        var result3 = await response3.Content.ReadFromJsonAsync<UpdateWishlistResponse>();

        // Assert - Each LastModified should be later than the previous
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotNull(result3);

        Assert.True(result2.LastModified >= result1.LastModified);
        Assert.True(result3.LastModified >= result2.LastModified);

        // Verify final database state
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var updatedParticipant = await context.GroupParticipants
                .FirstOrDefaultAsync(gp => gp.GroupId == group.Id && gp.UserId == user.Id);

            Assert.NotNull(updatedParticipant);
            Assert.Equal("Third update", updatedParticipant.WishlistContent);
        }
    }

    [Fact]
    public async Task PUT_Wishlist_AsParticipantNotOrganizer_UpdatesSuccessfully()
    {
        // Arrange
        var (organizer, _) = await CreateAndAuthenticateUser("Organizer", "User");
        var (participant, participantToken) = await CreateAndAuthenticateUser("Regular", "Participant");

        var group = new Group { Name = "Test Group", OrganizerUserId = organizer.Id, DrawCompletedAt = DateTimeOffset.UtcNow.AddHours(-1) };
        var organizerParticipant = new GroupParticipant { Group = group, UserId = organizer.Id };
        var regularParticipant = new GroupParticipant { Group = group, UserId = participant.Id };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.AddRange(organizerParticipant, regularParticipant);
            await context.SaveChangesAsync();
        }

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", participantToken);

        var request = new UpdateWishlistRequest
        {
            WishlistContent = "Participant's wishlist"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/groups/{group.Id}/participants/me/wishlist", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<UpdateWishlistResponse>();
        Assert.NotNull(result);
        Assert.Equal("Participant's wishlist", result.WishlistContent);
    }

    #endregion
}
