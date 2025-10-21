using Microsoft.AspNetCore.Mvc;
using SantaVibe.Api.Common;

namespace SantaVibe.Api.Features.Invitations.GetInvitationDetails;

/// <summary>
/// Endpoint configuration for getting invitation details
/// </summary>
public static class GetInvitationDetailsEndpoint
{
    /// <summary>
    /// Maps the get invitation details endpoint to the application
    /// </summary>
    public static IEndpointRouteBuilder MapGetInvitationDetailsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/invitations/{token:guid}", HandleGetInvitationDetails)
            .WithName("GetInvitationDetails")
            .WithTags("Invitations")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Get invitation details by token";
                operation.Description = "Retrieves group information for a given invitation token. " +
                    "This is a public endpoint used to display group details to potential participants " +
                    "before they decide to join. Returns 404 if the token is invalid, or 410 if the " +
                    "group has already completed the draw.";
                return operation;
            })
            .AllowAnonymous()
            .Produces<GetInvitationDetailsResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status410Gone)
            .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        return endpoints;
    }

    /// <summary>
    /// Handles get invitation details requests
    /// </summary>
    private static async Task<IResult> HandleGetInvitationDetails(
        [FromRoute] Guid token,
        [FromServices] IInvitationService invitationService,
        [FromServices] ILogger<GetInvitationDetailsResponse> logger,
        CancellationToken cancellationToken)
    {
        // Call service to retrieve invitation details
        var result = await invitationService.GetInvitationDetailsAsync(token, cancellationToken);

        // Handle not found case
        if (result is null)
        {
            logger.LogWarning("Invitation token {Token} not found", token);

            var errorResponse = new ErrorResponse
            {
                Error = "InvalidInvitation",
                Message = "This invitation link is invalid or has expired"
            };

            return Results.NotFound(errorResponse);
        }

        // Handle draw completed case (410 Gone)
        if (result.DrawCompleted)
        {
            logger.LogWarning(
                "Invitation token {Token} accessed but group {GroupId} draw already completed",
                token,
                result.GroupId);

            var errorResponse = new ErrorResponse
            {
                Error = "InvitationExpired",
                Message = "This group has already completed the draw and is no longer accepting participants"
            };

            return Results.Json(errorResponse, statusCode: StatusCodes.Status410Gone);
        }

        // Success - return 200 OK with invitation details
        logger.LogInformation(
            "Invitation details retrieved successfully for token {Token}, Group: {GroupName}",
            token,
            result.GroupName);

        return Results.Ok(result);
    }
}
