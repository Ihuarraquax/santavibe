using MediatR;
using Microsoft.EntityFrameworkCore;
using SantaVibe.Api.Common;
using SantaVibe.Api.Data;
using SantaVibe.Api.Services;

namespace SantaVibe.Api.Features.Wishlists.GetMyWishlist;

/// <summary>
/// Handles the retrieval of a user's wishlist for a Secret Santa group
/// </summary>
public class GetMyWishlistQueryHandler : IRequestHandler<GetMyWishlistQuery, Result<GetMyWishlistResponse>>
{
    private readonly ApplicationDbContext _context;
    private readonly IUserAccessor _userAccessor;
    private readonly ILogger<GetMyWishlistQueryHandler> _logger;

    public GetMyWishlistQueryHandler(
        ApplicationDbContext context,
        IUserAccessor userAccessor,
        ILogger<GetMyWishlistQueryHandler> logger)
    {
        _context = context;
        _userAccessor = userAccessor;
        _logger = logger;
    }

    public async Task<Result<GetMyWishlistResponse>> Handle(
        GetMyWishlistQuery query,
        CancellationToken cancellationToken)
    {
        // Extract current user ID from JWT token
        var userId = _userAccessor.GetCurrentUserId();

        // Query database with filtered include (only current user's participant record)
        var group = await _context.Groups
            .AsNoTracking()
            .Include(g => g.GroupParticipants.Where(gp => gp.UserId == userId.ToString()))
            .FirstOrDefaultAsync(g => g.Id == query.GroupId, cancellationToken);

        // Validate group exists
        if (group == null)
        {
            _logger.LogWarning(
                "User {UserId} attempted to access wishlist for non-existent group {GroupId}",
                userId,
                query.GroupId);
            return Result<GetMyWishlistResponse>.Failure(
                "GroupNotFound",
                "Group not found");
        }

        // Validate user is participant
        var participant = group.GroupParticipants.FirstOrDefault();
        if (participant == null)
        {
            _logger.LogWarning(
                "User {UserId} attempted to access wishlist for group {GroupId} but is not a participant",
                userId,
                query.GroupId);
            return Result<GetMyWishlistResponse>.Failure(
                "NotParticipant",
                "You are not a participant in this group");
        }

        // Map to response DTO
        var response = new GetMyWishlistResponse
        {
            GroupId = group.Id,
            WishlistContent = participant.WishlistContent,
            LastModified = participant.WishlistLastModified
        };

        return Result<GetMyWishlistResponse>.Success(response);
    }
}
