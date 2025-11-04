using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SantaVibe.Api.Data;
using SantaVibe.Api.Data.Entities;
using SantaVibe.Api.Features.Authentication.Login;
using SantaVibe.Api.Features.Gifts.GenerateGiftSuggestions;
using SantaVibe.Api.Services.AI;
using SantaVibe.Tests.Infrastructure;

namespace SantaVibe.Tests.Features.Gifts.GenerateGiftSuggestions;

/// <summary>
/// Integration tests for the Generate Gift Suggestions endpoint (POST /api/groups/{groupId}/my-assignment/gift-suggestions).
/// Tests AI-powered gift suggestion generation with rate limiting and authorization checks.
/// </summary>
public class GenerateGiftSuggestionsIntegrationTests : IClassFixture<SantaVibeWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly SantaVibeWebApplicationFactory _factory;

    public GenerateGiftSuggestionsIntegrationTests(SantaVibeWebApplicationFactory factory)
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
                RecipientUserId = participantUserIds[nextIndex]
            });
        }

        await context.SaveChangesAsync();
        return group;
    }

    [Fact]
    public async Task POST_GenerateGiftSuggestions_WithValidRequest_Returns200WithMarkdown()
    {
        // Arrange
        var (user1, token1) = await CreateAndAuthenticateUser("Maria", "Kowalska");
        var (user2, token2) = await CreateAndAuthenticateUser("Jan", "Nowak");

        var wishlists = new Dictionary<string, string?>
        {
            [user2.Id] = "Lubię książki kryminalne i narzędzia ogrodnicze."
        };

        var group = await CreateGroupWithDrawCompleted(
            user1.Id,
            new List<string> { user1.Id, user2.Id },
            wishlists
        );

        // Mock the AI service to return markdown
        var mockAiService = Substitute.For<IGiftSuggestionService>();
        mockAiService.GenerateGiftSuggestionsAsync(
            Arg.Any<GiftSuggestionContext>(),
            Arg.Any<CancellationToken>()
        ).Returns(new GiftSuggestionsResult(
            RecipientFirstName: "Jan",
            Budget: 100m,
            SuggestionsMarkdown: "## Sugestie prezentów\n\n1. **Książka kryminalna** - 45 PLN\n2. **Narzędzia ogrodnicze** - 85 PLN",
            GeneratedAt: DateTime.UtcNow,
            AiModel: "test-model"
        ));

        // Replace the service in DI container
        var factoryWithMock = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IGiftSuggestionService));
                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddScoped(_ => mockAiService);
            });
        });

        var client = factoryWithMock.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token1);

        // Act
        var response = await client.PostAsync($"/api/groups/{group.Id}/my-assignment/gift-suggestions", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GiftSuggestionsResponse>();
        Assert.NotNull(result);
        Assert.Equal(group.Id, result.GroupId);
        Assert.Equal("Jan", result.RecipientFirstName);
        Assert.Equal(100m, result.Budget);
        Assert.Contains("Sugestie prezentów", result.SuggestionsMarkdown);
        Assert.Contains("Książka kryminalna", result.SuggestionsMarkdown);
    }

    [Fact]
    public async Task POST_GenerateGiftSuggestions_Unauthorized_Returns401()
    {
        // Arrange
        var groupId = Guid.NewGuid();

        // Act - No authorization header
        var response = await _client.PostAsync($"/api/groups/{groupId}/my-assignment/gift-suggestions", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task POST_GenerateGiftSuggestions_GroupNotFound_Returns404()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var nonExistentGroupId = Guid.NewGuid();

        // Act
        var response = await _client.PostAsync($"/api/groups/{nonExistentGroupId}/my-assignment/gift-suggestions", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task POST_GenerateGiftSuggestions_NotParticipant_Returns403()
    {
        // Arrange
        var (organizer, _) = await CreateAndAuthenticateUser("Organizer", "User");
        var (user1, _) = await CreateAndAuthenticateUser("User1", "Test");
        var (user2, _) = await CreateAndAuthenticateUser("User2", "Test");
        var (nonParticipant, tokenNonParticipant) = await CreateAndAuthenticateUser("NonParticipant", "User");

        var group = await CreateGroupWithDrawCompleted(
            organizer.Id,
            new List<string> { organizer.Id, user1.Id, user2.Id }
        );

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenNonParticipant);

        // Act
        var response = await _client.PostAsync($"/api/groups/{group.Id}/my-assignment/gift-suggestions", null);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task POST_GenerateGiftSuggestions_DrawNotCompleted_Returns403()
    {
        // Arrange
        var (user1, token1) = await CreateAndAuthenticateUser();
        var (user2, _) = await CreateAndAuthenticateUser();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = $"Test Group {Guid.NewGuid()}",
            OrganizerUserId = user1.Id,
            InvitationToken = Guid.NewGuid(),
            Budget = 100m,
            DrawCompletedAt = null, // Draw not completed
            CreatedAt = DateTimeOffset.UtcNow
        };

        context.Groups.Add(group);

        context.GroupParticipants.Add(new GroupParticipant
        {
            GroupId = group.Id,
            UserId = user1.Id,
            JoinedAt = DateTimeOffset.UtcNow
        });

        context.GroupParticipants.Add(new GroupParticipant
        {
            GroupId = group.Id,
            UserId = user2.Id,
            JoinedAt = DateTimeOffset.UtcNow
        });

        await context.SaveChangesAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token1);

        // Act
        var response = await _client.PostAsync($"/api/groups/{group.Id}/my-assignment/gift-suggestions", null);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task POST_GenerateGiftSuggestions_NoAssignment_Returns404()
    {
        // Arrange
        var (user1, token1) = await CreateAndAuthenticateUser();
        var (user2, _) = await CreateAndAuthenticateUser();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = $"Test Group {Guid.NewGuid()}",
            OrganizerUserId = user1.Id,
            InvitationToken = Guid.NewGuid(),
            Budget = 100m,
            DrawCompletedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };

        context.Groups.Add(group);

        context.GroupParticipants.Add(new GroupParticipant
        {
            GroupId = group.Id,
            UserId = user1.Id,
            JoinedAt = DateTimeOffset.UtcNow
        });

        context.GroupParticipants.Add(new GroupParticipant
        {
            GroupId = group.Id,
            UserId = user2.Id,
            JoinedAt = DateTimeOffset.UtcNow
        });

        // No assignments created

        await context.SaveChangesAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token1);

        // Act
        var response = await _client.PostAsync($"/api/groups/{group.Id}/my-assignment/gift-suggestions", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
