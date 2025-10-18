using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using SantaVibe.Api.Features.Groups.Create;

namespace SantaVibe.Api.Common;

/// <summary>
/// Endpoint filter for validating request DTOs using Data Annotations
/// </summary>
/// <typeparam name="T">The type of request DTO to validate</typeparam>
public class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<T>().FirstOrDefault();
        if (request == null)
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "ValidationError",
                Detail = "Invalid request body",
                Status = StatusCodes.Status400BadRequest
            });
        }

        // Validate using Data Annotations
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(request);

        if (!Validator.TryValidateObject(request, validationContext, validationResults, validateAllProperties: true))
        {
            var errors = validationResults
                .GroupBy(e => e.MemberNames.FirstOrDefault() ?? "")
                .Where(g => !string.IsNullOrEmpty(g.Key))
                .ToDictionary(
                    g => char.ToLowerInvariant(g.Key[0]) + g.Key.Substring(1),
                    g => g.Select(e => e.ErrorMessage ?? "Validation error").ToArray()
                );

            var problemDetails = new ProblemDetails
            {
                Title = "Validation Error",
                Detail = validationResults.First().ErrorMessage ?? "Validation failed",
                Status = StatusCodes.Status400BadRequest
            };
            problemDetails.Extensions["errors"] = errors;

            return Results.BadRequest(problemDetails);
        }

        // Additional whitespace validation for CreateGroupRequest
        if (request is CreateGroupRequest createGroupRequest)
        {
            if (string.IsNullOrWhiteSpace(createGroupRequest.Name))
            {
                var problemDetails = new ProblemDetails
                {
                    Title = "Validation Error",
                    Detail = "Group name cannot be only whitespace",
                    Status = StatusCodes.Status400BadRequest
                };
                problemDetails.Extensions["errors"] = new Dictionary<string, string[]>
                {
                    { "name", new[] { "Group name cannot be only whitespace" } }
                };

                return Results.BadRequest(problemDetails);
            }
        }

        return await next(context);
    }
}
