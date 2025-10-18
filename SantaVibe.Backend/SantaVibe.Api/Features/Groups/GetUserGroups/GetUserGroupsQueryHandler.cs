using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SantaVibe.Api.Common;
using SantaVibe.Api.Data;
using SantaVibe.Api.Services;
using SantaVibe.Api.Features.Groups.GetUserGroups;
using System.Collections.Generic;

namespace SantaVibe.Api.Features.Groups.GetUserGroups;

public class GetUserGroupsQueryHandler : IRequestHandler<GetUserGroupsQuery, Result<GetUserGroupsResponse>>
{
    private readonly ApplicationDbContext _context;
    private readonly IUserAccessor _userAccessor;

    public GetUserGroupsQueryHandler(ApplicationDbContext context, IUserAccessor userAccessor)
    {
        _context = context;
        _userAccessor = userAccessor;
    }

    public async Task<Result<GetUserGroupsResponse>> Handle(GetUserGroupsQuery request, CancellationToken cancellationToken)
    {
        var userId = _userAccessor.GetCurrentUserId();

        var query = _context.GroupParticipants
            .AsNoTracking()
            .Where(gp => gp.UserId == userId.ToString());

        if (request.IncludeCompleted is false)
        {
            query = query.Where(gp => gp.Group.DrawCompletedAt == null);
        }

        var groups = await query
            .Select(gp => new GroupDto
            {
                GroupId = gp.GroupId,
                Name = gp.Group.Name,
                OrganizerId = gp.Group.Organizer.Id.ToString(),
                OrganizerName = gp.Group.Organizer.FirstName + " " + gp.Group.Organizer.LastName,
                IsOrganizer = gp.Group.OrganizerUserId == userId.ToString(),
                ParticipantCount = gp.Group.GroupParticipants.Count(),
                Budget = gp.Group.Budget,
                DrawCompleted = gp.Group.DrawCompletedAt.HasValue,
                JoinedAt = gp.JoinedAt,
                DrawCompletedAt = gp.Group.DrawCompletedAt
            })
            .ToListAsync(cancellationToken);

        var response = new GetUserGroupsResponse
        {
            Groups = groups,
            TotalCount = groups.Count
        };

        return Result<GetUserGroupsResponse>.Success(response);
    }
}