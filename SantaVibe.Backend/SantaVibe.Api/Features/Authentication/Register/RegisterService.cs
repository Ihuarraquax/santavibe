using Microsoft.AspNetCore.Identity;
using SantaVibe.Api.Common;
using SantaVibe.Api.Data.Entities;

namespace SantaVibe.Api.Features.Authentication.Register;

/// <summary>
/// Service for user registration operations
/// </summary>
public interface IRegisterService
{
    Task<Result<RegisterResponse>> RegisterUserAsync(RegisterRequest request);
}

/// <summary>
/// Implementation of user registration service
/// </summary>
public class RegisterService(
    UserManager<ApplicationUser> userManager,
    IConfiguration configuration,
    ILogger<RegisterService> logger)
    : IRegisterService
{
    private readonly TokenHelper tokenHelper = new TokenHelper();

    public async Task<Result<RegisterResponse>> RegisterUserAsync(RegisterRequest request)
    {
        logger.LogInformation("Attempting to register user with email: {Email}", request.Email);

        // Check if email already exists
        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            logger.LogWarning("Registration failed: Email already exists - {Email}", request.Email);
            return Result<RegisterResponse>.Failure(
                "EmailAlreadyExists",
                "An account with this email address already exists");
        }

        // Create new user
        var user = new ApplicationUser
        {
            UserName = request.Email, // Use email as username
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            CreatedAt = DateTimeOffset.UtcNow,
            EmailConfirmed = false // No email verification for MVP, but track the flag
        };

        // Create user with password (Identity handles hashing)
        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = result.Errors
                .GroupBy(e => e.Code)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.Description).ToArray()
                );

            logger.LogWarning(
                "Registration failed for {Email}: {Errors}",
                request.Email,
                string.Join(", ", errors.SelectMany(e => e.Value)));

            return Result<RegisterResponse>.ValidationFailure(
                "User registration failed",
                errors);
        }

        logger.LogInformation("User created successfully: {UserId} - {Email}", user.Id, user.Email);

        // Generate JWT token
        var token = tokenHelper.GenerateJwtToken(user, configuration);
        var jwtSettings = configuration.GetSection("Jwt");
        var expirationDays = int.TryParse(jwtSettings["ExpirationInDays"], out var days) ? days : 7;
        var expiresAt = DateTimeOffset.UtcNow.AddDays(expirationDays);

        var response = new RegisterResponse
        {
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Token = token,
            ExpiresAt = expiresAt
        };

        logger.LogInformation("Registration completed successfully for user: {UserId}", user.Id);

        return Result<RegisterResponse>.Success(response);
    }
}
