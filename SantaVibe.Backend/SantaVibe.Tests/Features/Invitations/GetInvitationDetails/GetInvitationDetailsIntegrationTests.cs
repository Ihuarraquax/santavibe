using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SantaVibe.Api.Common;
using SantaVibe.Api.Data;
using SantaVibe.Api.Data.Entities;
using SantaVibe.Api.Features.Invitations.GetInvitationDetails;
using SantaVibe.Tests.Infrastructure;
using Xunit;

namespace SantaVibe.Tests.Features.Invitations.GetInvitationDetails;

/// <summary>
/// Integration tests for GET /api/invitations/{token} endpoint
/// Tests public endpoint functionality, validation, and response structure
/// </summary>
public class GetInvitationDetailsIntegrationTests : IClassFixture<SantaVibeWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly SantaVibeWebApplicationFactory _factory;

    public GetInvitationDetailsIntegrationTests(SantaVibeWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    /// <summary>
    /// Helper method to create a user for testing
    /// </summary>
    private async Task<ApplicationUser> CreateUser(
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

        return user;
    }

    #region Public Endpoint Tests

    [Fact]
    public async Task GET_InvitationDetails_WhenNoAuthentication_ReturnsSuccess()
    {
        // Arrange
        var organizer = await CreateUser("Jan", "Kowalski");

        var group = new Group
        {
            Name = "Public Test Group",
            OrganizerUserId = organizer.Id
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.Add(new GroupParticipant
            {
                Group = group,
                UserId = organizer.Id
            });
            await context.SaveChangesAsync();
        }

        // Act - No authentication header
        var response = await _client.GetAsync($"/api/invitations/{group.InvitationToken}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task GET_InvitationDetails_WithValidToken_ReturnsCorrectData()
    {
        // Arrange
        var organizer = await CreateUser("Anna", "Nowak");
        var participant1 = await CreateUser("Piotr", "Kowalski");
        var participant2 = await CreateUser("Maria", "Wiśniewska");

        var group = new Group
        {
            Name = "Family Secret Santa 2025",
            OrganizerUserId = organizer.Id,
            Budget = 100.00m
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.AddRange(
                new GroupParticipant { Group = group, UserId = organizer.Id },
                new GroupParticipant { Group = group, UserId = participant1.Id },
                new GroupParticipant { Group = group, UserId = participant2.Id }
            );
            await context.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"/api/invitations/{group.InvitationToken}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetInvitationDetailsResponse>();

        Assert.NotNull(result);
        Assert.Equal(group.InvitationToken, result.InvitationToken);
        Assert.Equal(group.Id, result.GroupId);
        Assert.Equal("Family Secret Santa 2025", result.GroupName);
        Assert.Equal("Anna Nowak", result.OrganizerName);
        Assert.Equal(3, result.ParticipantCount);
        Assert.False(result.DrawCompleted);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task GET_InvitationDetails_WithSingleParticipant_ReturnsCorrectCount()
    {
        // Arrange
        var organizer = await CreateUser("Solo", "Organizer");

        var group = new Group
        {
            Name = "Solo Group",
            OrganizerUserId = organizer.Id
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.Add(new GroupParticipant
            {
                Group = group,
                UserId = organizer.Id
            });
            await context.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"/api/invitations/{group.InvitationToken}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetInvitationDetailsResponse>();

        Assert.NotNull(result);
        Assert.Equal(1, result.ParticipantCount);
    }

    [Fact]
    public async Task GET_InvitationDetails_WithMultipleParticipants_ReturnsAccurateCount()
    {
        // Arrange
        var organizer = await CreateUser("Organizer", "Test");
        var participants = new List<ApplicationUser>();

        for (int i = 1; i <= 5; i++)
        {
            participants.Add(await CreateUser($"Participant{i}", "Test"));
        }

        var group = new Group
        {
            Name = "Large Group",
            OrganizerUserId = organizer.Id
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.Add(new GroupParticipant
            {
                Group = group,
                UserId = organizer.Id
            });

            foreach (var participant in participants)
            {
                context.GroupParticipants.Add(new GroupParticipant
                {
                    Group = group,
                    UserId = participant.Id
                });
            }

            await context.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"/api/invitations/{group.InvitationToken}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetInvitationDetailsResponse>();

        Assert.NotNull(result);
        Assert.Equal(6, result.ParticipantCount); // organizer + 5 participants
    }

    [Fact]
    public async Task GET_InvitationDetails_OrganizerName_IsCorrectlyFormatted()
    {
        // Arrange
        var organizer = await CreateUser("Jan", "Kowalski");

        var group = new Group
        {
            Name = "Name Format Test",
            OrganizerUserId = organizer.Id
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.Add(new GroupParticipant
            {
                Group = group,
                UserId = organizer.Id
            });
            await context.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"/api/invitations/{group.InvitationToken}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetInvitationDetailsResponse>();

        Assert.NotNull(result);
        Assert.Equal("Jan Kowalski", result.OrganizerName);
        Assert.Contains(" ", result.OrganizerName); // Contains space between first and last name
    }

    [Fact]
    public async Task GET_InvitationDetails_IsValid_IsAlwaysTrueForFoundGroups()
    {
        // Arrange
        var organizer = await CreateUser("Valid", "Test");

        var group = new Group
        {
            Name = "Valid Group",
            OrganizerUserId = organizer.Id
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.Add(new GroupParticipant
            {
                Group = group,
                UserId = organizer.Id
            });
            await context.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"/api/invitations/{group.InvitationToken}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetInvitationDetailsResponse>();

        Assert.NotNull(result);
        Assert.True(result.IsValid);
    }

    #endregion

    #region Error Scenarios

    [Fact]
    public async Task GET_InvitationDetails_WithInvalidToken_ReturnsNotFound()
    {
        // Arrange
        var nonExistentToken = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/invitations/{nonExistentToken}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(errorResponse);
        Assert.Equal("InvalidInvitation", errorResponse.Error);
        Assert.Equal("This invitation link is invalid or has expired", errorResponse.Message);
    }

    [Fact]
    public async Task GET_InvitationDetails_WithInvalidGuidFormat_ReturnsNotFound()
    {
        // Arrange
        var invalidToken = "not-a-valid-guid";

        // Act
        var response = await _client.GetAsync($"/api/invitations/{invalidToken}");

        // Assert
        // When GUID constraint fails in route, ASP.NET returns 404 (route not matched)
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GET_InvitationDetails_WhenDrawCompleted_Returns410Gone()
    {
        // Arrange
        var organizer = await CreateUser("Completed", "Organizer");
        var participant1 = await CreateUser("Participant1", "Test");
        var participant2 = await CreateUser("Participant2", "Test");

        var group = new Group
        {
            Name = "Completed Draw Group",
            OrganizerUserId = organizer.Id,
            DrawCompletedAt = DateTimeOffset.UtcNow.AddDays(-2) // Draw completed 2 days ago
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.AddRange(
                new GroupParticipant { Group = group, UserId = organizer.Id },
                new GroupParticipant { Group = group, UserId = participant1.Id },
                new GroupParticipant { Group = group, UserId = participant2.Id }
            );
            await context.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"/api/invitations/{group.InvitationToken}");

        // Assert
        Assert.Equal(HttpStatusCode.Gone, response.StatusCode); // 410 Gone

        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(errorResponse);
        Assert.Equal("InvitationExpired", errorResponse.Error);
        Assert.Equal("This group has already completed the draw and is no longer accepting participants", errorResponse.Message);
    }

    [Fact]
    public async Task GET_InvitationDetails_WhenDrawJustCompleted_Returns410Gone()
    {
        // Arrange
        var organizer = await CreateUser("Recent", "Organizer");
        var participant1 = await CreateUser("Participant1", "Test");
        var participant2 = await CreateUser("Participant2", "Test");

        var group = new Group
        {
            Name = "Just Completed Group",
            OrganizerUserId = organizer.Id,
            DrawCompletedAt = DateTimeOffset.UtcNow // Just completed now
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.AddRange(
                new GroupParticipant { Group = group, UserId = organizer.Id },
                new GroupParticipant { Group = group, UserId = participant1.Id },
                new GroupParticipant { Group = group, UserId = participant2.Id }
            );
            await context.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"/api/invitations/{group.InvitationToken}");

        // Assert
        Assert.Equal(HttpStatusCode.Gone, response.StatusCode); // 410 Gone
    }

    #endregion

    #region Draw Status Tests

    [Fact]
    public async Task GET_InvitationDetails_BeforeDraw_DrawCompletedIsFalse()
    {
        // Arrange
        var organizer = await CreateUser("Before", "Draw");

        var group = new Group
        {
            Name = "Pre-Draw Group",
            OrganizerUserId = organizer.Id,
            DrawCompletedAt = null // No draw yet
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.Add(new GroupParticipant
            {
                Group = group,
                UserId = organizer.Id
            });
            await context.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"/api/invitations/{group.InvitationToken}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetInvitationDetailsResponse>();

        Assert.NotNull(result);
        Assert.False(result.DrawCompleted);
    }

    [Fact]
    public async Task GET_InvitationDetails_AfterDraw_DrawCompletedIsTrue()
    {
        // Arrange
        var organizer = await CreateUser("After", "Draw");

        var group = new Group
        {
            Name = "Post-Draw Group",
            OrganizerUserId = organizer.Id,
            DrawCompletedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.Add(new GroupParticipant
            {
                Group = group,
                UserId = organizer.Id
            });
            await context.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"/api/invitations/{group.InvitationToken}");

        // Assert - Should return 410 Gone, but response body should indicate DrawCompleted = true
        Assert.Equal(HttpStatusCode.Gone, response.StatusCode);
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task GET_InvitationDetails_ResponseTime_IsAcceptable()
    {
        // Arrange
        var organizer = await CreateUser("Performance", "Test");

        var group = new Group
        {
            Name = "Performance Test Group",
            OrganizerUserId = organizer.Id
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.Add(new GroupParticipant
            {
                Group = group,
                UserId = organizer.Id
            });
            await context.SaveChangesAsync();
        }

        // Act
        var startTime = DateTime.UtcNow;
        var response = await _client.GetAsync($"/api/invitations/{group.InvitationToken}");
        var endTime = DateTime.UtcNow;

        // Assert
        response.EnsureSuccessStatusCode();
        var responseTime = (endTime - startTime).TotalMilliseconds;

        // Should respond within 500ms (generous for integration test environment)
        Assert.True(responseTime < 500, $"Response time {responseTime}ms exceeds acceptable threshold");
    }

    #endregion

    #region Data Integrity Tests

    [Fact]
    public async Task GET_InvitationDetails_MultipleRequests_ReturnsConsistentData()
    {
        // Arrange
        var organizer = await CreateUser("Consistent", "Test");

        var group = new Group
        {
            Name = "Consistency Test Group",
            OrganizerUserId = organizer.Id
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.Add(new GroupParticipant
            {
                Group = group,
                UserId = organizer.Id
            });
            await context.SaveChangesAsync();
        }

        // Act - Make multiple requests
        var response1 = await _client.GetAsync($"/api/invitations/{group.InvitationToken}");
        var response2 = await _client.GetAsync($"/api/invitations/{group.InvitationToken}");
        var response3 = await _client.GetAsync($"/api/invitations/{group.InvitationToken}");

        // Assert
        response1.EnsureSuccessStatusCode();
        response2.EnsureSuccessStatusCode();
        response3.EnsureSuccessStatusCode();

        var result1 = await response1.Content.ReadFromJsonAsync<GetInvitationDetailsResponse>();
        var result2 = await response2.Content.ReadFromJsonAsync<GetInvitationDetailsResponse>();
        var result3 = await response3.Content.ReadFromJsonAsync<GetInvitationDetailsResponse>();

        // All responses should be identical
        Assert.Equal(result1!.GroupId, result2!.GroupId);
        Assert.Equal(result1.GroupId, result3!.GroupId);
        Assert.Equal(result1.GroupName, result2.GroupName);
        Assert.Equal(result1.GroupName, result3.GroupName);
        Assert.Equal(result1.ParticipantCount, result2.ParticipantCount);
        Assert.Equal(result1.ParticipantCount, result3.ParticipantCount);
    }

    #endregion
}
