using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using SantaVibe.Api.Common;
using SantaVibe.Api.Services;

namespace SantaVibe.Api.Features.Profile.UpdateProfile;

/// <summary>
/// Endpoint configuration for updating user profile
/// </summary>
public static class UpdateProfileEndpoint
{
    /// <summary>
    /// Maps the update profile endpoint to the application
    /// </summary>
    public static IEndpointRouteBuilder MapUpdateProfileEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/api/profile", HandleUpdateProfile)
            .WithName("UpdateProfile")
            .WithTags("Profile")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Update current user's profile";
                operation.Description = "Updates the authenticated user's first name and last name";
                return operation;
            })
            .RequireAuthorization()
            .Produces<UpdateProfileResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        return endpoints;
    }

    /// <summary>
    /// Handles update profile requests
    /// </summary>
    private static async Task<IResult> HandleUpdateProfile(
        [FromBody] UpdateProfileRequest? request,
        [FromServices] IUserAccessor userAccessor,
        [FromServices] IProfileService profileService,
        [FromServices] ILogger<UpdateProfileResponse> logger)
    {
        // Handle null request (malformed JSON or missing body)
        if (request == null)
        {
            var errorResponse = new ErrorResponse
            {
                Error = "ValidationError",
                Message = "Request body is required",
                Details = new Dictionary<string, string[]>
                {
                    ["$"] = new[] { "A valid JSON request body is required" }
                }
            };
            return Results.BadRequest(errorResponse);
        }

        // Validate model state manually (minimal APIs don't auto-validate)
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(request);

        if (!Validator.TryValidateObject(
            request, validationContext, validationResults, validateAllProperties: true))
        {
            var errors = validationResults
                .GroupBy(v => v.MemberNames.FirstOrDefault() ?? "General")
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(v => v.ErrorMessage ?? "Validation error").ToArray()
                );

            logger.LogWarning("Update profile validation failed: {Errors}",
                string.Join(", ", errors.SelectMany(e => e.Value)));

            var errorResponse = new ErrorResponse
            {
                Error = "ValidationError",
                Message = "One or more validation errors occurred",
                Details = errors
            };

            return Results.BadRequest(errorResponse);
        }

        try
        {
            // Extract user ID from JWT claims
            var userId = userAccessor.GetCurrentUserId();

            logger.LogInformation("Updating profile for user: {UserId}", userId);

            // Create command and call service
            var command = new UpdateProfileCommand(
                userId,
                request.FirstName,
                request.LastName
            );

            var result = await profileService.UpdateProfileAsync(command);

            if (!result.IsSuccess)
            {
                // Map error to appropriate HTTP status code
                var statusCode = result.Error switch
                {
                    "UserNotFound" => StatusCodes.Status404NotFound,
                    "ValidationError" => StatusCodes.Status400BadRequest,
                    "InternalError" => StatusCodes.Status500InternalServerError,
                    _ => StatusCodes.Status500InternalServerError
                };

                var errorResponse = new ErrorResponse
                {
                    Error = result.Error ?? "Error",
                    Message = result.Message ?? "An error occurred",
                    Details = result.ValidationErrors
                };

                logger.LogWarning(
                    "Update profile failed with status {StatusCode}: {Error} - {Message}",
                    statusCode, result.Error, result.Message);

                return Results.Json(errorResponse, statusCode: statusCode);
            }

            // Success - return 200 OK
            logger.LogInformation("Profile updated successfully for user: {UserId}", userId);

            return Results.Ok(result.Value);
        }
        catch (InvalidOperationException ex)
        {
            // This occurs when user ID cannot be extracted from JWT claims
            logger.LogError(ex, "Failed to extract user ID from JWT claims");

            var errorResponse = new ErrorResponse
            {
                Error = "Unauthorized",
                Message = "Invalid authentication token"
            };

            return Results.Json(errorResponse, statusCode: StatusCodes.Status401Unauthorized);
        }
    }
}
