using Microsoft.AspNetCore.Identity;
using SantaVibe.Api.Common;
using SantaVibe.Api.Data.Entities;
using SantaVibe.Api.Features.Profile.GetProfile;
using SantaVibe.Api.Features.Profile.UpdateProfile;

namespace SantaVibe.Api.Features.Profile;

/// <summary>
/// Implementation of profile management service
/// </summary>
public class ProfileService(
    UserManager<ApplicationUser> userManager,
    ILogger<ProfileService> logger)
    : IProfileService
{
    /// <summary>
    /// Retrieves a user's profile by user ID
    /// </summary>
    public async Task<Result<GetProfileResponse>> GetProfileAsync(Guid userId)
    {
        logger.LogInformation("Fetching profile for user: {UserId}", userId);

        try
        {
            // Find user by ID
            var user = await userManager.FindByIdAsync(userId.ToString());

            // Check if user exists
            if (user == null)
            {
                logger.LogWarning("User not found: {UserId}", userId);
                return Result<GetProfileResponse>.Failure(
                    "UserNotFound",
                    "User profile not found");
            }

            // Check if user is soft-deleted
            if (user.IsDeleted)
            {
                logger.LogWarning("User is soft-deleted: {UserId}", userId);
                return Result<GetProfileResponse>.Failure(
                    "UserNotFound",
                    "User profile not found");
            }

            // Map entity to response DTO
            var response = new GetProfileResponse
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };

            logger.LogInformation("Profile retrieved successfully for user: {UserId}", userId);

            return Result<GetProfileResponse>.Success(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving profile for user: {UserId}", userId);
            return Result<GetProfileResponse>.Failure(
                "InternalError",
                "An error occurred while retrieving the profile");
        }
    }

    /// <summary>
    /// Updates a user's profile information
    /// </summary>
    public async Task<Result<UpdateProfileResponse>> UpdateProfileAsync(UpdateProfileCommand command)
    {
        logger.LogInformation("Updating profile for user: {UserId}", command.UserId);

        try
        {
            // Find user by ID
            var user = await userManager.FindByIdAsync(command.UserId.ToString());

            // Check if user exists
            if (user == null)
            {
                logger.LogWarning("User not found: {UserId}", command.UserId);
                return Result<UpdateProfileResponse>.Failure(
                    "UserNotFound",
                    "User profile not found");
            }

            // Check if user is soft-deleted
            if (user.IsDeleted)
            {
                logger.LogWarning("User is soft-deleted: {UserId}", command.UserId);
                return Result<UpdateProfileResponse>.Failure(
                    "UserNotFound",
                    "User profile not found");
            }

            // Update user properties
            user.FirstName = command.FirstName;
            user.LastName = command.LastName;

            // Persist changes using UserManager
            var updateResult = await userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                logger.LogError(
                    "Failed to update profile for user {UserId}: {Errors}",
                    command.UserId,
                    errors);

                return Result<UpdateProfileResponse>.Failure(
                    "InternalError",
                    "Failed to update profile");
            }

            // Map updated entity to response DTO
            var response = new UpdateProfileResponse
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };

            logger.LogInformation("Profile updated successfully for user: {UserId}", command.UserId);

            return Result<UpdateProfileResponse>.Success(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating profile for user: {UserId}", command.UserId);
            return Result<UpdateProfileResponse>.Failure(
                "InternalError",
                "An error occurred while updating the profile");
        }
    }
}
