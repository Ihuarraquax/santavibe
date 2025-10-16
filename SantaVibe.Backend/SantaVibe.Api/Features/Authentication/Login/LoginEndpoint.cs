using Microsoft.AspNetCore.Mvc;
using SantaVibe.Api.Common;

namespace SantaVibe.Api.Features.Authentication.Login;

/// <summary>
/// Endpoint configuration for user login
/// </summary>
public static class LoginEndpoint
{
    /// <summary>
    /// Maps the login endpoint to the application
    /// </summary>
    public static IEndpointRouteBuilder MapLoginEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/auth/login", HandleLogin)
            .WithName("Login")
            .WithTags("Authentication")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Login with email and password";
                operation.Description = "Authenticates a user and returns a JWT token";
                return operation;
            })
            .RequireRateLimiting("login")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ErrorResponse>(StatusCodes.Status429TooManyRequests)
            .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        return endpoints;
    }

    /// <summary>
    /// Handles user login requests
    /// </summary>
    private static async Task<IResult> HandleLogin(
        [FromBody] LoginRequest? request,
        [FromServices] ILoginService loginService,
        [FromServices] ILogger<LoginRequest> logger,
        HttpContext context)
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
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(request);

        if (!System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
            request, validationContext, validationResults, validateAllProperties: true))
        {
            var errors = validationResults
                .GroupBy(v => v.MemberNames.FirstOrDefault() ?? "General")
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(v => v.ErrorMessage ?? "Validation error").ToArray()
                );

            logger.LogWarning("Login validation failed: {Errors}",
                string.Join(", ", errors.SelectMany(e => e.Value)));

            var errorResponse = new ErrorResponse
            {
                Error = "ValidationError",
                Message = "One or more validation errors occurred",
                Details = errors
            };

            return Results.BadRequest(errorResponse);
        }

        // Call service
        var result = await loginService.LoginUserAsync(request);

        if (!result.IsSuccess)
        {
            // Map error to appropriate HTTP status code
            var statusCode = result.Error switch
            {
                "InvalidCredentials" => StatusCodes.Status401Unauthorized,
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
                "Login failed with status {StatusCode}: {Error} - {Message}",
                statusCode, result.Error, result.Message);

            return Results.Json(errorResponse, statusCode: statusCode);
        }

        // Success - return 200 OK (not 201 Created, since login doesn't create a resource)
        logger.LogInformation("User logged in successfully: {UserId}", result.Value!.UserId);

        return Results.Ok(result.Value);
    }
}
