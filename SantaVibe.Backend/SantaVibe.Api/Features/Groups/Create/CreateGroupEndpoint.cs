using MediatR;
using Microsoft.AspNetCore.Mvc;
using SantaVibe.Api.Common;

namespace SantaVibe.Api.Features.Groups.Create;

/// <summary>
/// Minimal API endpoint for creating a new Secret Santa group
/// </summary>
public static class CreateGroupEndpoint
{
    public static void MapCreateGroupEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/groups", async (
                [FromBody] CreateGroupRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateGroupCommand(request.Name);
                var result = await sender.Send(command, cancellationToken);

                if (result.IsSuccess)
                {
                    return Results.Created($"/api/groups/{result.Value!.GroupId}", result.Value);
                }

                return result.ToProblem();
            })
            .RequireAuthorization()
            .WithName("CreateGroup")
            .WithTags("Groups")
            .Produces<CreateGroupResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .AddEndpointFilter<ValidationFilter<CreateGroupRequest>>();
    }
}
