using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SantaVibe.Api.Data;
using SantaVibe.Api.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add DbContext with PostgreSQL
builder.Services.AddDbContext<SantaVibeDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add CORS for Angular frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast",
        () =>
        {
            var forecast = Enumerable.Range(1, 5).Select(index =>
                    new WeatherForecast(
                        DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                        Random.Shared.Next(-20, 55),
                        summaries[Random.Shared.Next(summaries.Length)]
                    ))
                .ToArray();
            return forecast;
        })
    .WithName("GetWeatherForecast");

// POST /group endpoint
app.MapPost("/group", async (CreateGroupRequest request, SantaVibeDbContext db) =>
    {
        // Validate the request
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(request);

        if (!System.ComponentModel.DataAnnotations.Validator.TryValidateObject(request, validationContext, validationResults, true))
        {
            var errors = validationResults.Select(vr => vr.ErrorMessage).ToList();
            return Results.ValidationProblem(errors.ToDictionary(e => e ?? "Error", e => new[] { e ?? "Validation error" }));
        }

        // Validate DrawDate is in the future if provided
        if (request.DrawDate.HasValue && request.DrawDate.Value < DateTime.UtcNow)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                { "DrawDate", new[] { "Draw date must be in the future" } }
            });
        }

        // Generate unique invitation code
        var invitationCode = GenerateInvitationCode();
        while (await db.Groups.AnyAsync(g => g.InvitationCode == invitationCode))
        {
            invitationCode = GenerateInvitationCode();
        }

        // Create the group
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Budget = request.Budget,
            DrawDate = request.DrawDate,
            OrganizerId = "temp-user-id", // TODO: Replace with actual user ID from authentication
            IsDrawCompleted = false,
            InvitationCode = invitationCode,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Groups.Add(group);
        await db.SaveChangesAsync();

        return Results.Created($"/group/{group.Id}", group);
    })
    .WithName("CreateGroup")
    .WithOpenApi();

static string GenerateInvitationCode()
{
    const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Removed similar-looking characters
    var random = new Random();
    return new string(Enumerable.Repeat(chars, 8)
        .Select(s => s[random.Next(s.Length)]).ToArray());
}

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}