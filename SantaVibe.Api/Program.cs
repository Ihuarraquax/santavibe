using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SantaVibe.Api.Data;
using SantaVibe.Api.DTOs;
using SantaVibe.Api.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddOpenApi();

// Configure CORS for Angular frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Configure PostgreSQL database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors();

// POST /group - Create a new Secret Santa group
app.MapPost("/group", async (CreateGroupRequest request, AppDbContext db) =>
{
    // For MVP, we'll use a mock user ID since authentication is not yet implemented
    // TODO: Replace with actual authenticated user ID from JWT token
    var mockUserId = Guid.NewGuid();

    // Check if mock user exists, if not create one
    var user = await db.Users.FirstOrDefaultAsync(u => u.Id == mockUserId);
    if (user == null)
    {
        user = new User
        {
            Id = mockUserId,
            Email = "organizer@example.com",
            FirstName = "John",
            LastName = "Doe",
            PasswordHash = "mock_hash",
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Users.Add(user);
    }

    // Generate unique invitation code
    var invitationCode = GenerateInvitationCode();

    // Create the group
    var group = new Group
    {
        Id = Guid.NewGuid(),
        Name = request.Name,
        OrganizerId = user.Id,
        Organizer = user,
        InvitationCode = invitationCode,
        CreatedAt = DateTimeOffset.UtcNow,
        IsDrawPerformed = false
    };

    db.Groups.Add(group);

    // Add organizer as a participant
    var participant = new Participant
    {
        Id = Guid.NewGuid(),
        GroupId = group.Id,
        Group = group,
        UserId = user.Id,
        User = user,
        JoinedAt = DateTimeOffset.UtcNow
    };

    db.Participants.Add(participant);

    await db.SaveChangesAsync();

    // Return group response
    var response = new GroupResponse
    {
        Id = group.Id,
        Name = group.Name,
        OrganizerId = group.OrganizerId,
        OrganizerName = $"{user.FirstName} {user.LastName}",
        InvitationCode = invitationCode,
        Budget = group.Budget,
        IsDrawPerformed = group.IsDrawPerformed,
        CreatedAt = group.CreatedAt,
        DrawPerformedAt = group.DrawPerformedAt,
        ParticipantCount = 1
    };

    return Results.Created($"/group/{group.Id}", response);
})
.WithName("CreateGroup")
.WithOpenApi();

app.Run();

// Helper method to generate cryptographically secure invitation code
static string GenerateInvitationCode()
{
    const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    var bytes = new byte[8];
    using (var rng = RandomNumberGenerator.Create())
    {
        rng.GetBytes(bytes);
    }

    return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
}