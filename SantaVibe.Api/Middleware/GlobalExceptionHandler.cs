using System.Net;
using System.Text.Json;
using SantaVibe.Api.DTOs.Common;

namespace SantaVibe.Api.Middleware;

/// <summary>
/// Global exception handling middleware for consistent error responses
/// </summary>
public class GlobalExceptionHandler(
    RequestDelegate next,
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Log the exception
        logger.LogError(
            exception,
            "Unhandled exception occurred. Path: {Path}, Method: {Method}, TraceId: {TraceId}",
            context.Request.Path,
            context.Request.Method,
            context.TraceIdentifier);

        // Determine status code and error message
        var (statusCode, error, message) = exception switch
        {
            UnauthorizedAccessException => (
                HttpStatusCode.Forbidden,
                "Forbidden",
                "You do not have permission to access this resource"),

            KeyNotFoundException => (
                HttpStatusCode.NotFound,
                "NotFound",
                "The requested resource was not found"),

            ArgumentException or ArgumentNullException => (
                HttpStatusCode.BadRequest,
                "BadRequest",
                "Invalid request parameters"),

            InvalidOperationException => (
                HttpStatusCode.BadRequest,
                "InvalidOperation",
                "The requested operation is not valid"),

            _ => (
                HttpStatusCode.InternalServerError,
                "InternalError",
                "An unexpected error occurred while processing your request")
        };

        // Create error response
        var errorResponse = new ErrorResponse
        {
            Error = error,
            Message = message,
            Details = environment.IsDevelopment()
                ? new Dictionary<string, string[]>
                {
                    ["Exception"] = new[] { exception.Message },
                    ["StackTrace"] = new[] { exception.StackTrace ?? "No stack trace available" }
                }
                : null
        };

        // Set response
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = environment.IsDevelopment()
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(errorResponse, options));
    }
}

/// <summary>
/// Extension method for registering the global exception handler
/// </summary>
public static class GlobalExceptionHandlerExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionHandler>();
    }
}
