using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SantaVibe.Api.Data;
using SantaVibe.Api.Data.Entities;
using SantaVibe.Api.Features.Authentication.Login;
using SantaVibe.Api.Features.Groups.GetGroupDetails;
using SantaVibe.Tests.Infrastructure;
using Xunit;

namespace SantaVibe.Tests.Features.Groups.GetGroupDetails;

/// <summary>
/// Integration tests for GET /api/groups/{groupId} endpoint
/// Tests authentication, authorization, and response structure for different scenarios
/// </summary>
public class GetGroupDetailsIntegrationTests : IClassFixture<SantaVibeWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly SantaVibeWebApplicationFactory _factory;

    public GetGroupDetailsIntegrationTests(SantaVibeWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    /// <summary>
    /// Helper method to create and authenticate a user for testing
    /// </summary>
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

    #region Authentication Tests

    [Fact]
    public async Task GET_GroupDetails_WhenUnauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        var groupId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/groups/{groupId}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Authorization Tests

    [Fact]
    public async Task GET_GroupDetails_WhenUserNotParticipant_ReturnsForbidden()
    {
        // Arrange
        var (organizer, _) = await CreateAndAuthenticateUser("Organizer", "User");
        var (nonParticipant, nonParticipantToken) = await CreateAndAuthenticateUser("NonParticipant", "User");

        var group = new Group { Name = "Test Group", OrganizerUserId = organizer.Id };

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

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", nonParticipantToken);

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GET_GroupDetails_WhenGroupNotFound_ReturnsNotFound()
    {
        // Arrange
        var (_, token) = await CreateAndAuthenticateUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var nonExistentGroupId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/groups/{nonExistentGroupId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Before Draw Tests

    [Fact]
    public async Task GET_GroupDetails_BeforeDraw_ReturnsCompleteParticipantList()
    {
        // Arrange
        var (organizer, organizerToken) = await CreateAndAuthenticateUser("Jan", "Kowalski");
        var (participant1, _) = await CreateAndAuthenticateUser("Anna", "Nowak");
        var (participant2, _) = await CreateAndAuthenticateUser("Piotr", "Wiśniewski");

        var group = new Group
        {
            Name = "Family Secret Santa 2025",
            OrganizerUserId = organizer.Id,
            Budget = 100.00m,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-5)
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.AddRange(
                new GroupParticipant
                {
                    Group = group,
                    UserId = organizer.Id,
                    BudgetSuggestion = 100.00m,
                    WishlistContent = "Test wishlist",
                    JoinedAt = DateTimeOffset.UtcNow.AddDays(-5)
                },
                new GroupParticipant
                {
                    Group = group,
                    UserId = participant1.Id,
                    BudgetSuggestion = 80.00m,
                    JoinedAt = DateTimeOffset.UtcNow.AddDays(-4)
                },
                new GroupParticipant
                {
                    Group = group,
                    UserId = participant2.Id,
                    WishlistContent = "Another wishlist",
                    JoinedAt = DateTimeOffset.UtcNow.AddDays(-3)
                }
            );
            await context.SaveChangesAsync();
        }

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", organizerToken);

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetGroupDetailsResponse>();

        Assert.NotNull(result);

        // Common fields
        Assert.Equal(group.Id, result.GroupId);
        Assert.Equal("Family Secret Santa 2025", result.Name);
        Assert.Equal(organizer.Id, result.OrganizerId);
        Assert.Equal("Jan Kowalski", result.OrganizerName);
        Assert.True(result.IsOrganizer);
        Assert.Equal(100.00m, result.Budget);
        Assert.False(result.DrawCompleted);
        Assert.Null(result.DrawCompletedAt);
        Assert.Equal(3, result.ParticipantCount);

        // Before draw fields
        Assert.NotNull(result.Participants);
        Assert.Equal(3, result.Participants.Count);
        Assert.NotNull(result.ExclusionRuleCount);
        Assert.Equal(0, result.ExclusionRuleCount);
        Assert.NotNull(result.InvitationLink);
        Assert.Contains($"/invite/{group.InvitationToken}", result.InvitationLink);
        Assert.NotNull(result.CanDraw);
        Assert.True(result.CanDraw); // 3 participants is valid
        Assert.NotNull(result.DrawValidation);
        Assert.True(result.DrawValidation.IsValid);
        Assert.Empty(result.DrawValidation.Errors);

        // Verify participant details
        var organizerParticipant = result.Participants.First(p => p.UserId == organizer.Id);
        Assert.Equal("Jan", organizerParticipant.FirstName);
        Assert.Equal("Kowalski", organizerParticipant.LastName);
        Assert.True(organizerParticipant.HasWishlist);
        Assert.True(organizerParticipant.IsOrganizer);

        var participant1Details = result.Participants.First(p => p.UserId == participant1.Id);
        Assert.Equal("Anna", participant1Details.FirstName);
        Assert.Equal("Nowak", participant1Details.LastName);
        Assert.False(participant1Details.HasWishlist);
        Assert.False(participant1Details.IsOrganizer);

        var participant2Details = result.Participants.First(p => p.UserId == participant2.Id);
        Assert.Equal("Piotr", participant2Details.FirstName);
        Assert.Equal("Wiśniewski", participant2Details.LastName);
        Assert.True(participant2Details.HasWishlist);
        Assert.False(participant2Details.IsOrganizer);

        // After draw fields should be null
        Assert.Null(result.MyAssignment);
    }

    [Fact]
    public async Task GET_GroupDetails_BeforeDraw_AsNonOrganizer_ReturnsCorrectIsOrganizerFlag()
    {
        // Arrange
        var (organizer, _) = await CreateAndAuthenticateUser("Organizer", "User");
        var (participant, participantToken) = await CreateAndAuthenticateUser("Participant", "User");

        var group = new Group
        {
            Name = "Test Group",
            OrganizerUserId = organizer.Id
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.AddRange(
                new GroupParticipant { Group = group, UserId = organizer.Id },
                new GroupParticipant { Group = group, UserId = participant.Id }
            );
            await context.SaveChangesAsync();
        }

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", participantToken);

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetGroupDetailsResponse>();

        Assert.NotNull(result);
        Assert.False(result.IsOrganizer);
        Assert.Equal(organizer.Id, result.OrganizerId);
        Assert.Null(result.InvitationLink); // Non-organizers should not see invitation link
    }

    [Fact]
    public async Task GET_GroupDetails_BeforeDraw_WithLessThan3Participants_ReturnsInvalidDrawValidation()
    {
        // Arrange
        var (organizer, organizerToken) = await CreateAndAuthenticateUser("Organizer", "User");
        var (participant, _) = await CreateAndAuthenticateUser("Participant", "User");

        var group = new Group
        {
            Name = "Small Group",
            OrganizerUserId = organizer.Id
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.AddRange(
                new GroupParticipant { Group = group, UserId = organizer.Id },
                new GroupParticipant { Group = group, UserId = participant.Id }
            );
            await context.SaveChangesAsync();
        }

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", organizerToken);

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetGroupDetailsResponse>();

        Assert.NotNull(result);
        Assert.Equal(2, result.ParticipantCount);
        Assert.NotNull(result.CanDraw);
        Assert.False(result.CanDraw);
        Assert.NotNull(result.DrawValidation);
        Assert.False(result.DrawValidation.IsValid);
        Assert.Single(result.DrawValidation.Errors);
        Assert.Contains("Minimum 3 participants required for draw", result.DrawValidation.Errors);
    }

    [Fact]
    public async Task GET_GroupDetails_BeforeDraw_WithExclusionRules_ReturnsCorrectCount()
    {
        // Arrange
        var (organizer, organizerToken) = await CreateAndAuthenticateUser("Organizer", "User");
        var (participant1, _) = await CreateAndAuthenticateUser("Participant1", "User");
        var (participant2, _) = await CreateAndAuthenticateUser("Participant2", "User");

        var group = new Group
        {
            Name = "Group with Exclusions",
            OrganizerUserId = organizer.Id
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

            // Add exclusion rules
            context.ExclusionRules.AddRange(
                new ExclusionRule
                {
                    GroupId = group.Id,
                    UserId1 = organizer.Id,
                    UserId2 = participant1.Id,
                    CreatedByUserId = organizer.Id
                },
                new ExclusionRule
                {
                    GroupId = group.Id,
                    UserId1 = participant1.Id,
                    UserId2 = participant2.Id,
                    CreatedByUserId = organizer.Id
                }
            );

            await context.SaveChangesAsync();
        }

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", organizerToken);

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetGroupDetailsResponse>();

        Assert.NotNull(result);
        Assert.NotNull(result.ExclusionRuleCount);
        Assert.Equal(2, result.ExclusionRuleCount);
    }

    #endregion

    #region After Draw Tests

    [Fact]
    public async Task GET_GroupDetails_AfterDraw_ReturnsUserAssignmentOnly()
    {
        // Arrange
        var (organizer, organizerToken) = await CreateAndAuthenticateUser("Jan", "Kowalski");
        var (participant1, _) = await CreateAndAuthenticateUser("Anna", "Nowak");
        var (participant2, _) = await CreateAndAuthenticateUser("Piotr", "Wiśniewski");
        var (participant3, _) = await CreateAndAuthenticateUser("Maria", "Lewandowska");

        var group = new Group
        {
            Name = "Completed Draw Group",
            OrganizerUserId = organizer.Id,
            Budget = 150.00m,
            DrawCompletedAt = DateTimeOffset.UtcNow.AddDays(-2),
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-10)
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.Add(group);
            context.GroupParticipants.AddRange(
                new GroupParticipant
                {
                    Group = group,
                    UserId = organizer.Id,
                    WishlistContent = "Organizer wishlist"
                },
                new GroupParticipant
                {
                    Group = group,
                    UserId = participant1.Id,
                    WishlistContent = "Participant1 wishlist"
                },
                new GroupParticipant
                {
                    Group = group,
                    UserId = participant2.Id
                },
                new GroupParticipant
                {
                    Group = group,
                    UserId = participant3.Id
                }
            );

            await context.SaveChangesAsync();

            // Create assignments: organizer -> participant1
            context.Assignments.Add(new Assignment
            {
                GroupId = group.Id,
                SantaUserId = organizer.Id,
                RecipientUserId = participant1.Id,
                AssignedAt = group.DrawCompletedAt.Value
            });

            await context.SaveChangesAsync();
        }

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", organizerToken);

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetGroupDetailsResponse>();

        Assert.NotNull(result);

        // Common fields
        Assert.Equal(group.Id, result.GroupId);
        Assert.Equal("Completed Draw Group", result.Name);
        Assert.Equal(organizer.Id, result.OrganizerId);
        Assert.Equal("Jan Kowalski", result.OrganizerName);
        Assert.True(result.IsOrganizer);
        Assert.Equal(150.00m, result.Budget);
        Assert.True(result.DrawCompleted);
        Assert.NotNull(result.DrawCompletedAt);
        Assert.Equal(4, result.ParticipantCount);

        // Before draw fields should be null
        Assert.Null(result.Participants);
        Assert.Null(result.ExclusionRuleCount);
        Assert.Null(result.InvitationLink);
        Assert.Null(result.CanDraw);
        Assert.Null(result.DrawValidation);

        // After draw fields
        Assert.NotNull(result.MyAssignment);
        Assert.Equal(participant1.Id, result.MyAssignment.RecipientId);
        Assert.Equal("Anna", result.MyAssignment.RecipientFirstName);
        Assert.Equal("Nowak", result.MyAssignment.RecipientLastName);
        Assert.True(result.MyAssignment.HasWishlist);
    }

    [Fact]
    public async Task GET_GroupDetails_AfterDraw_RecipientWithoutWishlist_ReturnsHasWishlistFalse()
    {
        // Arrange
        var (organizer, organizerToken) = await CreateAndAuthenticateUser("Organizer", "User");
        var (participant1, _) = await CreateAndAuthenticateUser("Participant1", "User");
        var (participant2, _) = await CreateAndAuthenticateUser("Participant2", "User");

        var group = new Group
        {
            Name = "Test Group",
            OrganizerUserId = organizer.Id,
            DrawCompletedAt = DateTimeOffset.UtcNow.AddDays(-1)
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

            // Organizer is buying for participant2 who has no wishlist
            context.Assignments.Add(new Assignment
            {
                GroupId = group.Id,
                SantaUserId = organizer.Id,
                RecipientUserId = participant2.Id
            });

            await context.SaveChangesAsync();
        }

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", organizerToken);

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetGroupDetailsResponse>();

        Assert.NotNull(result);
        Assert.NotNull(result.MyAssignment);
        Assert.False(result.MyAssignment.HasWishlist);
    }

    [Fact]
    public async Task GET_GroupDetails_AfterDraw_AsNonOrganizer_ReturnsOwnAssignment()
    {
        // Arrange
        var (organizer, _) = await CreateAndAuthenticateUser("Organizer", "User");
        var (participant1, participant1Token) = await CreateAndAuthenticateUser("Participant1", "User");
        var (participant2, _) = await CreateAndAuthenticateUser("Participant2", "User");

        var group = new Group
        {
            Name = "Test Group",
            OrganizerUserId = organizer.Id,
            DrawCompletedAt = DateTimeOffset.UtcNow.AddDays(-1)
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

            // participant1 is buying for organizer
            context.Assignments.Add(new Assignment
            {
                GroupId = group.Id,
                SantaUserId = participant1.Id,
                RecipientUserId = organizer.Id
            });

            await context.SaveChangesAsync();
        }

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", participant1Token);

        // Act
        var response = await _client.GetAsync($"/api/groups/{group.Id}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetGroupDetailsResponse>();

        Assert.NotNull(result);
        Assert.False(result.IsOrganizer); // Not the organizer
        Assert.NotNull(result.MyAssignment);
        Assert.Equal(organizer.Id, result.MyAssignment.RecipientId);
        Assert.Equal("Organizer", result.MyAssignment.RecipientFirstName);
        Assert.Equal("User", result.MyAssignment.RecipientLastName);
    }

    #endregion
}
