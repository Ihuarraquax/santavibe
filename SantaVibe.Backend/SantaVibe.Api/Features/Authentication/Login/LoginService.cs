using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using SantaVibe.Api.Common;
using SantaVibe.Api.Data.Entities;

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
        var token = GenerateJwtToken(user);
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

    private string GenerateJwtToken(ApplicationUser user)
    {
        var jwtSettings = configuration.GetSection("Jwt");
        var secretKey = jwtSettings["Secret"]
            ?? throw new InvalidOperationException("JWT Secret not configured");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new Claim(JwtRegisteredClaimNames.FamilyName, user.LastName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
        };

        var expirationDays = int.TryParse(jwtSettings["ExpirationInDays"], out var days) ? days : 7;

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(expirationDays),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
