using MediatR;
using SantaVibe.Api.Common;

namespace SantaVibe.Api.Features.Assignments.GetMyAssignment;

/// <summary>
/// Query to retrieve the authenticated user's Secret Santa assignment for a specific group
/// </summary>
public sealed record GetMyAssignmentQuery(
    Guid GroupId,
    string UserId) : IRequest<Result<GetMyAssignmentResponse>>;
