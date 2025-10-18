
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
using SantaVibe.Api.Features.Groups.GetUserGroups;
using SantaVibe.Tests.Infrastructure;
using Xunit;

namespace SantaVibe.Tests.Features.Groups.GetUserGroups;

public class GetUserGroupsIntegrationTests : IClassFixture<SantaVibeWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly SantaVibeWebApplicationFactory _factory;

    public GetUserGroupsIntegrationTests(SantaVibeWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(ApplicationUser user, string token)> CreateAndAuthenticateUser(string firstName = "Test", string lastName = "User")
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


    [Fact]
    public async Task GET_Groups_WhenUnauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/groups");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GET_Groups_WhenAuthenticatedAndNoGroups_ReturnsOkWithEmptyList()
    {
        // Arrange
        var (_, token) = await CreateAndAuthenticateUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/groups");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetUserGroupsResponse>();
        Assert.NotNull(result);
        Assert.Empty(result.Groups);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task GET_Groups_WhenAuthenticated_ReturnsOkWithUserGroups()
    {
        // Arrange
        var (organizer, _) = await CreateAndAuthenticateUser("Organizer", "User");
        var (participant, participantToken) = await CreateAndAuthenticateUser("Participant", "User");

        var group1 = new Group { Name = "Test Group 1", OrganizerUserId = organizer.Id };
        var group2 = new Group { Name = "Test Group 2", OrganizerUserId = participant.Id };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.AddRange(group1, group2);
            context.GroupParticipants.AddRange(
                new GroupParticipant { Group = group1, UserId = organizer.Id },
                new GroupParticipant { Group = group1, UserId = participant.Id },
                new GroupParticipant { Group = group2, UserId = participant.Id }
            );
            await context.SaveChangesAsync();
        }

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", participantToken);

        // Act
        var response = await _client.GetAsync("/api/groups");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetUserGroupsResponse>();
        
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);

        var g1 = result.Groups.FirstOrDefault(g => g.GroupId == group1.Id);
        Assert.NotNull(g1);
        Assert.Equal("Test Group 1", g1.Name);
        Assert.Equal(organizer.Id, g1.OrganizerId);
        Assert.Equal("Organizer User", g1.OrganizerName);
        Assert.False(g1.IsOrganizer);
        Assert.Equal(2, g1.ParticipantCount);

        var g2 = result.Groups.FirstOrDefault(g => g.GroupId == group2.Id);
        Assert.NotNull(g2);
        Assert.Equal("Test Group 2", g2.Name);
        Assert.Equal(participant.Id, g2.OrganizerId);
        Assert.Equal("Participant User", g2.OrganizerName);
        Assert.True(g2.IsOrganizer);
        Assert.Equal(1, g2.ParticipantCount);
    }
    
    [Fact]
    public async Task GET_Groups_WithIncludeCompletedFalse_ReturnsOkWithOnlyActiveGroups()
    {
        // Arrange
        var (user, token) = await CreateAndAuthenticateUser();

        var activeGroup = new Group { Name = "Active Group", OrganizerUserId = user.Id };
        var completedGroup = new Group { Name = "Completed Group", OrganizerUserId = user.Id, DrawCompletedAt = DateTimeOffset.UtcNow };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Groups.AddRange(activeGroup, completedGroup);
            context.GroupParticipants.AddRange(
                new GroupParticipant { Group = activeGroup, UserId = user.Id },
                new GroupParticipant { Group = completedGroup, UserId = user.Id }
            );
            await context.SaveChangesAsync();
        }

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/groups?includeCompleted=false");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetUserGroupsResponse>();
        
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(activeGroup.Id, result.Groups.Single().GroupId);
    }
}
