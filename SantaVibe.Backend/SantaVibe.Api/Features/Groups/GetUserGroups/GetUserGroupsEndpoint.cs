
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SantaVibe.Api.Common;

namespace SantaVibe.Api.Features.Groups.GetUserGroups;

public static class GetUserGroupsEndpoint
{
    public static void MapGetUserGroupsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/groups", async ([AsParameters] GetUserGroupsRequest request, ISender sender) =>
            {
                var query = new GetUserGroupsQuery(request.IncludeCompleted);
                var result = await sender.Send(query);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
            })
            .RequireAuthorization()
            .WithTags("Groups")
            .WithName("GetUserGroups")
            .Produces<GetUserGroupsResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}
