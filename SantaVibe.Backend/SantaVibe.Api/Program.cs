using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SantaVibe.Api.Data;
using SantaVibe.Api.Data.Entities;
using SantaVibe.Api.Features.Authentication.Register;
using SantaVibe.Api.Features.Authentication.Login;
using SantaVibe.Api.Middleware;
using Serilog;

// Configure Serilog (basic configuration, will be enhanced with app configuration later)
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/santavibe-.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting SantaVibe API");

    var builder = WebApplication.CreateBuilder(args);

    // Use Serilog for logging
    builder.Host.UseSerilog();

    // Add database context
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null
            )
        )
    );

    // Add Identity services
    builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Password settings
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 8;
        options.Password.RequiredUniqueChars = 1;

        // User settings
        options.User.RequireUniqueEmail = true;
        options.User.AllowedUserNameCharacters =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

        // Sign-in settings (not used for registration, but configure anyway)
        options.SignIn.RequireConfirmedEmail = false; // No email verification for MVP
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

    // Add JWT authentication
    var jwtSettings = builder.Configuration.GetSection("Jwt");
    var secretKey = jwtSettings["Secret"]
        ?? throw new InvalidOperationException("JWT Secret not configured");

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(secretKey)
            ),
            ClockSkew = TimeSpan.Zero // No tolerance for token expiration
        };
    });

    builder.Services.AddAuthorization();

    // Add CORS services
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("DevelopmentPolicy", policy =>
        {
            policy.WithOrigins("http://localhost:4200")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });
    });

    // Add rate limiting services
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.AddPolicy("register", context =>
            System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5, // 5 attempts
                    Window = TimeSpan.FromMinutes(15), // per 15 minutes
                    QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0 // No queueing
                }));

        options.AddPolicy("login", context =>
            System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5, // 5 attempts
                    Window = TimeSpan.FromMinutes(15), // per 15 minutes
                    QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0 // No queueing
                }));
    });

    // Register application services (Vertical Slice Architecture)
    builder.Services.AddScoped<IRegisterService, RegisterService>();
    builder.Services.AddScoped<ILoginService, LoginService>();

    builder.Services.AddOpenApi();

    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "SantaVibe API",
            Version = "v1",
            Description = "Secret Santa gift exchange API"
        });

        // Configure JWT authentication in Swagger
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme (Example: 'Bearer {token}')",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    var app = builder.Build();

    // Apply database migrations automatically on startup
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            Log.Information("Applying database migrations...");
            await context.Database.MigrateAsync();
            Log.Information("Database migrations applied successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while migrating the database");
            throw;
        }
    }

    // Configure global exception handling (must be early in pipeline)
    app.UseGlobalExceptionHandler();

    // Configure development tools
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "SantaVibe API v1");
        });
    }

    app.UseHttpsRedirection();

    // Apply CORS middleware (must be before authentication and authorization)
    if (app.Environment.IsDevelopment())
    {
        app.UseCors("DevelopmentPolicy");
    }

    // Apply rate limiting middleware (skip in testing environment)
    if (!app.Configuration.GetValue<bool>("DisableRateLimiting"))
    {
        app.UseRateLimiter();
    }

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseSerilogRequestLogging();
    // Map endpoints (Vertical Slice Architecture)
    app.MapRegisterEndpoint();
    app.MapLoginEndpoint();

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

// Make Program accessible for integration testing
namespace SantaVibe.Api
{
    public partial class Program { }
}
