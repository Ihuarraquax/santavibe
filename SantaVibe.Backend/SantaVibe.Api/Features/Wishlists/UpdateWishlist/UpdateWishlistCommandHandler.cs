using MediatR;
using Microsoft.EntityFrameworkCore;
using SantaVibe.Api.Common;
using SantaVibe.Api.Data;
using SantaVibe.Api.Services;

namespace SantaVibe.Api.Features.Wishlists.UpdateWishlist;

/// <summary>
/// Handles the update of a user's wishlist for a Secret Santa group
/// </summary>
public class UpdateWishlistCommandHandler : IRequestHandler<UpdateWishlistCommand, Result<UpdateWishlistResponse>>
{
    private readonly ApplicationDbContext _context;
    private readonly IUserAccessor _userAccessor;
    private readonly IPublisher _publisher;
    private readonly ILogger<UpdateWishlistCommandHandler> _logger;

    public UpdateWishlistCommandHandler(
        ApplicationDbContext context,
        IUserAccessor userAccessor,
        IPublisher publisher,
        ILogger<UpdateWishlistCommandHandler> logger)
    {
        _context = context;
        _userAccessor = userAccessor;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<Result<UpdateWishlistResponse>> Handle(
        UpdateWishlistCommand command,
        CancellationToken cancellationToken)
    {
        var userId = _userAccessor.GetCurrentUserId();

        // Fetch group with participant check in a single query
        var group = await _context.Groups
            .Include(g => g.GroupParticipants.Where(gp => gp.UserId == userId.ToString()))
            .FirstOrDefaultAsync(g => g.Id == command.GroupId, cancellationToken);

        if (group == null)
        {
            _logger.LogWarning(
                "User {UserId} attempted to update wishlist for non-existent group {GroupId}",
                userId,
                command.GroupId);
            return Result<UpdateWishlistResponse>.Failure(
                "GroupNotFound",
                "Group not found");
        }

        var participant = group.GroupParticipants.FirstOrDefault();
        if (participant == null)
        {
            _logger.LogWarning(
                "User {UserId} attempted to update wishlist for group {GroupId} without being a participant",
                userId,
                command.GroupId);
            return Result<UpdateWishlistResponse>.Failure(
                "NotParticipant",
                "You are not a participant in this group");
        }

        var lastModified = DateTimeOffset.UtcNow;

        // Update wishlist content and timestamp
        participant.WishlistContent = command.WishlistContent;
        participant.WishlistLastModified = lastModified;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "User {UserId} updated wishlist for group {GroupId}",
            userId,
            command.GroupId);

        // Publish domain event if draw is completed
        if (group.DrawCompletedAt.HasValue)
        {
            await _publisher.Publish(
                new WishlistUpdatedNotification(
                    command.GroupId,
                    userId.ToString()),
                cancellationToken);
        }

        return Result<UpdateWishlistResponse>.Success(
            new UpdateWishlistResponse
            {
                GroupId = command.GroupId,
                WishlistContent = command.WishlistContent,
                LastModified = lastModified
            });
    }
}
