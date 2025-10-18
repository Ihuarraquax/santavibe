using Microsoft.AspNetCore.Identity;
using SantaVibe.Api.Common;
using SantaVibe.Api.Data.Entities;
using SantaVibe.Api.Features.Authentication.Register;

namespace SantaVibe.Api.Features.Authentication.Login;

/// <summary>
/// Service for user login operations
/// </summary>
public interface ILoginService
{
    Task<Result<LoginResponse>> LoginUserAsync(LoginRequest request);
}

/// <summary>
/// Implementation of user login service
/// </summary>
public class LoginService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IConfiguration configuration,
    ILogger<LoginService> logger)
    : ILoginService
{
    private readonly TokenHelper tokenHelper = new();
    public async Task<Result<LoginResponse>> LoginUserAsync(LoginRequest request)
    {
        logger.LogInformation("Login attempt for email: {Email}", request.Email);

        // Find user by email
        var user = await userManager.FindByEmailAsync(request.Email);

        // Return generic error if user not found (prevent user enumeration)
        if (user == null)
        {
            logger.LogWarning("Login failed: User not found - {Email}", request.Email);
            return Result<LoginResponse>.Failure(
                "InvalidCredentials",
                "Invalid email or password");
        }

        // Check if user is soft-deleted (prevent deleted users from logging in)
        if (user.IsDeleted)
        {
            logger.LogWarning("Login failed: User is soft-deleted - {Email}", request.Email);
            return Result<LoginResponse>.Failure(
                "InvalidCredentials",
                "Invalid email or password");
        }

        // Verify password using SignInManager (constant-time comparison)
        var passwordCheck = await signInManager.CheckPasswordSignInAsync(
            user,
            request.Password,
            lockoutOnFailure: false);

        if (!passwordCheck.Succeeded)
        {
            logger.LogWarning("Login failed: Invalid password - {Email}", request.Email);
            return Result<LoginResponse>.Failure(
                "InvalidCredentials",
                "Invalid email or password");
        }

        // Update LastLoginAt timestamp
        user.LastLoginAt = DateTimeOffset.UtcNow;
        var updateResult = await userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            logger.LogError(
                "Failed to update LastLoginAt for user {UserId}: {Errors}",
                user.Id,
                string.Join(", ", updateResult.Errors.Select(e => e.Description)));

            // Continue with login even if timestamp update fails (non-critical)
        }

        logger.LogInformation("Login successful for user: {UserId} - {Email}", user.Id, user.Email);

        // Generate JWT token
        var token = tokenHelper.GenerateJwtToken(user, configuration);
        var jwtSettings = configuration.GetSection("Jwt");
        var expirationDays = int.TryParse(jwtSettings["ExpirationInDays"], out var days) ? days : 7;
        var expiresAt = DateTimeOffset.UtcNow.AddDays(expirationDays);

        var response = new LoginResponse
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Token = token,
            ExpiresAt = expiresAt
        };

        return Result<LoginResponse>.Success(response);
    }
}
