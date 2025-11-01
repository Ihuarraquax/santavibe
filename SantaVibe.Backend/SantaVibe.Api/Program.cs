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
using SantaVibe.Api.Features.Groups.GetUserGroups;
using SantaVibe.Api.Features.Groups.GetGroupDetails;
using SantaVibe.Api.Features.Groups.Create;
using SantaVibe.Api.Features.Invitations;
using SantaVibe.Api.Features.Invitations.GetInvitationDetails;
using SantaVibe.Api.Features.Invitations.AcceptInvitation;
using SantaVibe.Api.Features.Wishlists.UpdateWishlist;
using SantaVibe.Api.Features.Wishlists.GetMyWishlist;
using SantaVibe.Api.Features.Groups.UpdateBudgetSuggestion;
using SantaVibe.Api.Features.Groups.GetBudgetSuggestions;
using SantaVibe.Api.Features.ExclusionRules.GetExclusionRules;
using SantaVibe.Api.Features.ExclusionRules.CreateExclusionRule;
using SantaVibe.Api.Features.ExclusionRules.DeleteExclusionRule;
using SantaVibe.Api.Middleware;
using SantaVibe.Api.Services;
using SantaVibe.Api.Common;
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

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration.GetSection("Jwt");
        var secretKey = jwtSettings["Secret"]
                        ?? throw new InvalidOperationException("JWT Secret not configured");
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        {
            KeyId = "my-app-signing-key-id" // <-- ADD THIS
        };
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            ClockSkew = TimeSpan.Zero, // No tolerance for token expiration
            IssuerSigningKey = securityKey
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
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<IUserAccessor, UserAccessor>();
    builder.Services.AddScoped<IRegisterService, RegisterService>();
    builder.Services.AddScoped<ILoginService, LoginService>();
    builder.Services.AddScoped<IInvitationService, InvitationService>();
    builder.Services.AddScoped<SantaVibe.Api.Services.DrawValidation.IDrawValidationService, SantaVibe.Api.Services.DrawValidation.DrawValidationService>();

    // Register validation filter
    builder.Services.AddScoped(typeof(ValidationFilter<>));

    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
        // Register transaction behavior for all commands implementing ITransactionalCommand
        cfg.AddOpenBehavior(typeof(SantaVibe.Api.Common.Behaviors.TransactionBehavior<,>));
    });
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
    Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;
    app.UseSerilogRequestLogging();
    // Map endpoints (Vertical Slice Architecture)
    app.MapRegisterEndpoint();
    app.MapLoginEndpoint();
    app.MapGetUserGroupsEndpoint();
    app.MapGetGroupDetailsEndpoint();
    app.MapCreateGroupEndpoint();
    app.MapGetInvitationDetailsEndpoint();
    app.MapAcceptInvitationEndpoint();
    app.MapUpdateWishlistEndpoint();
    app.MapGetMyWishlistEndpoint();
    app.MapUpdateBudgetSuggestionEndpoint();
    app.MapGetBudgetSuggestionsEndpoint();
    app.MapGetExclusionRulesEndpoint();
    app.MapCreateExclusionRuleEndpoint();
    app.MapDeleteExclusionRuleEndpoint();

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
