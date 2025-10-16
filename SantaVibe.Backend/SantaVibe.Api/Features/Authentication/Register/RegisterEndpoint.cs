using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SantaVibe.Api.Common;

namespace SantaVibe.Api.Features.Authentication.Register;

/// <summary>
/// Endpoint configuration for user registration
/// </summary>
public static class RegisterEndpoint
{
    /// <summary>
    /// Maps the registration endpoint to the application
    /// </summary>
    public static IEndpointRouteBuilder MapRegisterEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/auth/register", HandleRegister)
            .WithName("Register")
            .WithTags("Authentication")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Register a new user";
                operation.Description = "Creates a new user account and returns a JWT token";
                return operation;
            })
            .RequireRateLimiting("register")
            .Produces<RegisterResponse>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
            .Produces<ErrorResponse>(StatusCodes.Status429TooManyRequests)
            .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        return endpoints;
    }

    /// <summary>
    /// Handles user registration requests
    /// </summary>
    private static async Task<IResult> HandleRegister(
        [FromBody] RegisterRequest? request,
        [FromServices] IRegisterService registerService,
        [FromServices] ILogger<RegisterRequest> logger,
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

            logger.LogWarning("Registration validation failed: {Errors}",
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
        var result = await registerService.RegisterUserAsync(request);

        if (!result.IsSuccess)
        {
            // Map error to appropriate HTTP status code
            var statusCode = result.Error switch
            {
                "EmailAlreadyExists" => StatusCodes.Status409Conflict,
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
                "Registration failed with status {StatusCode}: {Error} - {Message}",
                statusCode, result.Error, result.Message);

            return Results.Json(errorResponse, statusCode: statusCode);
        }

        // Success
        logger.LogInformation("User registered successfully: {UserId}", result.Value!.UserId);

        return Results.Created(
            $"/api/users/{result.Value.UserId}",
            result.Value);
    }
}
