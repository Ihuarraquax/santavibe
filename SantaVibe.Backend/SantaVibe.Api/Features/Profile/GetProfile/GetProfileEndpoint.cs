using Microsoft.AspNetCore.Mvc;
using SantaVibe.Api.Common;
using SantaVibe.Api.Services;

namespace SantaVibe.Api.Features.Profile.GetProfile;

/// <summary>
/// Endpoint configuration for retrieving user profile
/// </summary>
public static class GetProfileEndpoint
{
    /// <summary>
    /// Maps the get profile endpoint to the application
    /// </summary>
    public static IEndpointRouteBuilder MapGetProfileEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/profile", HandleGetProfile)
            .WithName("GetProfile")
            .WithTags("Profile")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Get current user's profile";
                operation.Description = "Retrieves the authenticated user's profile information";
                return operation;
            })
            .RequireAuthorization()
            .Produces<GetProfileResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        return endpoints;
    }

    /// <summary>
    /// Handles get profile requests
    /// </summary>
    private static async Task<IResult> HandleGetProfile(
        [FromServices] IUserAccessor userAccessor,
        [FromServices] IProfileService profileService,
        [FromServices] ILogger<GetProfileResponse> logger)
    {
        try
        {
            // Extract user ID from JWT claims
            var userId = userAccessor.GetCurrentUserId();

            logger.LogInformation("Retrieving profile for user: {UserId}", userId);

            // Call service to get profile
            var result = await profileService.GetProfileAsync(userId);

            if (!result.IsSuccess)
            {
                // Map error to appropriate HTTP status code
                var statusCode = result.Error switch
                {
                    "UserNotFound" => StatusCodes.Status404NotFound,
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
                    "Get profile failed with status {StatusCode}: {Error} - {Message}",
                    statusCode, result.Error, result.Message);

                return Results.Json(errorResponse, statusCode: statusCode);
            }

            // Success - return 200 OK
            logger.LogInformation("Profile retrieved successfully for user: {UserId}", userId);

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
