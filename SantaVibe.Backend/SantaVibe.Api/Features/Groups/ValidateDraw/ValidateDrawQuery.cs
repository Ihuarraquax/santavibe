using MediatR;
using SantaVibe.Api.Common;

namespace SantaVibe.Api.Features.Groups.ValidateDraw;

/// <summary>
/// Query to validate draw feasibility for a group
/// </summary>
public sealed record ValidateDrawQuery(
    Guid GroupId,
    string UserId) : IRequest<Result<ValidateDrawResponse>>;
