using MediatR;
using SantaVibe.Api.Common;

namespace SantaVibe.Api.Features.Groups.GetUserGroups;

public record GetUserGroupsQuery(bool? IncludeCompleted) : IRequest<Result<GetUserGroupsResponse>>;