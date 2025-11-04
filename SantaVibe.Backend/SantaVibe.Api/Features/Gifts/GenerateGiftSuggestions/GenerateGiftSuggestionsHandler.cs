using MediatR;
using Microsoft.EntityFrameworkCore;
using SantaVibe.Api.Common;
using SantaVibe.Api.Data;
using SantaVibe.Api.Services;
using SantaVibe.Api.Services.AI;

namespace SantaVibe.Api.Features.Gifts.GenerateGiftSuggestions;

public class GenerateGiftSuggestionsHandler(
    ApplicationDbContext context,
    IGiftSuggestionService giftSuggestionService,
    IUserAccessor userAccessor,
    ILogger<GenerateGiftSuggestionsHandler> logger) : IRequestHandler<GenerateGiftSuggestionsCommand, Result<GiftSuggestionsResponse>>
{
    public async Task<Result<GiftSuggestionsResponse>> Handle(
        GenerateGiftSuggestionsCommand request,
        CancellationToken cancellationToken)
    {
        var userId = userAccessor.GetCurrentUserId();

        // Step 1: Load assignment with all necessary context (optimized single query)
        var assignmentData = await context.Assignments
            .AsNoTracking()
            .Where(a => a.GroupId == request.GroupId && a.SantaUserId == userId.ToString())
            .Select(a => new
            {
                a.GroupId,
                Budget = a.Group.Budget,
                DrawCompleted = a.Group.DrawCompletedAt != null,
                IsParticipant = a.Group.GroupParticipants.Any(p => p.UserId == userId.ToString()),
                RecipientFirstName = a.Recipient.FirstName,
                RecipientUserId = a.RecipientUserId,
                GroupExists = true
            })
            .FirstOrDefaultAsync(cancellationToken);

        // Step 2: Validate business rules with clear error messages
        if (assignmentData == null)
        {
            // Check if group exists
            var groupExists = await context.Groups
                .AsNoTracking()
                .AnyAsync(g => g.Id == request.GroupId, cancellationToken);

            if (!groupExists)
            {
                return Result<GiftSuggestionsResponse>.Failure(
                    "GroupNotFound",
                    "Group not found");
            }

            // Check if user is a participant
            var isParticipant = await context.GroupParticipants
                .AsNoTracking()
                .AnyAsync(gp => gp.GroupId == request.GroupId && gp.UserId == userId.ToString(), cancellationToken);

            if (!isParticipant)
            {
                return Result<GiftSuggestionsResponse>.Failure(
                    "NotParticipant",
                    "You are not a participant in this group");
            }

            // Check if draw is completed
            var drawCompleted = await context.Groups
                .AsNoTracking()
                .Where(g => g.Id == request.GroupId)
                .Select(g => g.DrawCompletedAt != null)
                .FirstOrDefaultAsync(cancellationToken);

            if (!drawCompleted)
            {
                return Result<GiftSuggestionsResponse>.Failure(
                    "DrawNotCompleted",
                    "Draw has not been completed yet. You cannot view recipient information.");
            }

            // If we reached here, the user has no assignment
            return Result<GiftSuggestionsResponse>.Failure(
                "AssignmentNotFound",
                "You do not have an assignment in this group");
        }

        if (!assignmentData.IsParticipant)
        {
            return Result<GiftSuggestionsResponse>.Failure(
                "NotParticipant",
                "You are not a participant in this group");
        }

        if (!assignmentData.DrawCompleted)
        {
            return Result<GiftSuggestionsResponse>.Failure(
                "DrawNotCompleted",
                "Draw has not been completed yet. You cannot view recipient information.");
        }

        if (!assignmentData.Budget.HasValue)
        {
            logger.LogError(
                "Group {GroupId} has no budget set but draw is completed",
                request.GroupId);

            return Result<GiftSuggestionsResponse>.Failure(
                "InternalServerError",
                "Group budget is not configured. Please contact the organizer.");
        }

        // Step 3: Load recipient's wishlist
        var wishlist = await context.GroupParticipants
            .AsNoTracking()
            .Where(gp => gp.GroupId == request.GroupId && gp.UserId == assignmentData.RecipientUserId)
            .Select(gp => gp.WishlistContent)
            .FirstOrDefaultAsync(cancellationToken);

        // Step 4: Build context and call AI service
        var suggestionContext = new GiftSuggestionContext(
            RecipientFirstName: assignmentData.RecipientFirstName,
            WishlistContent: wishlist,
            Budget: assignmentData.Budget.Value
        );

        logger.LogInformation(
            "Generating gift suggestions for user {UserId} in group {GroupId}, recipient: {RecipientName}",
            userId, request.GroupId, assignmentData.RecipientFirstName);

        GiftSuggestionsResult aiResult;
        try
        {
            aiResult = await giftSuggestionService.GenerateGiftSuggestionsAsync(
                suggestionContext,
                cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex,
                "AI service error while generating gift suggestions for user {UserId} in group {GroupId}",
                userId, request.GroupId);

            return Result<GiftSuggestionsResponse>.Failure(
                "InternalServerError",
                "Gift suggestion service is temporarily unavailable. Please try again later.");
        }

        logger.LogInformation(
            "Successfully generated gift suggestions markdown for user {UserId} in group {GroupId}",
            userId, request.GroupId);

        // Step 5: Return response with correct GroupId
        var response = new GiftSuggestionsResponse(
            GroupId: request.GroupId,
            RecipientFirstName: aiResult.RecipientFirstName,
            Budget: aiResult.Budget,
            SuggestionsMarkdown: aiResult.SuggestionsMarkdown,
            GeneratedAt: aiResult.GeneratedAt,
            AiModel: aiResult.AiModel
        );

        return Result<GiftSuggestionsResponse>.Success(response);
    }
}
