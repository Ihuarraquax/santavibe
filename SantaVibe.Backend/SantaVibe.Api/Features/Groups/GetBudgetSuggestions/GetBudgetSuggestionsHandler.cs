using MediatR;
using Microsoft.EntityFrameworkCore;
using SantaVibe.Api.Common;
using SantaVibe.Api.Data;
using SantaVibe.Api.Services;

namespace SantaVibe.Api.Features.Groups.GetBudgetSuggestions;

/// <summary>
/// Handler for GetBudgetSuggestionsQuery
/// Retrieves anonymous budget suggestions from group participants
/// Only accessible by the group organizer
/// </summary>
public class GetBudgetSuggestionsHandler(
    ApplicationDbContext context,
    IUserAccessor userAccessor)
    : IRequestHandler<GetBudgetSuggestionsQuery, Result<BudgetSuggestionsResponse>>
{
    public async Task<Result<BudgetSuggestionsResponse>> Handle(
        GetBudgetSuggestionsQuery request,
        CancellationToken cancellationToken)
    {
        // Get current user ID from JWT token
        var currentUserId = userAccessor.GetCurrentUserId();

        // Query group entity with optimized single query including participants
        var group = await context.Groups
            .AsNoTracking()
            .Include(g => g.GroupParticipants)
            .FirstOrDefaultAsync(g => g.Id == request.GroupId, cancellationToken);

        // Check if group exists
        if (group == null)
        {
            return Result<BudgetSuggestionsResponse>.Failure(
                "GroupNotFound",
                "Group does not exist"
            );
        }

        // Verify user is the organizer
        if (group.OrganizerUserId != currentUserId.ToString())
        {
            return Result<BudgetSuggestionsResponse>.Failure(
                "Forbidden",
                "Only the group organizer can view budget suggestions"
            );
        }

        // Extract and process budget suggestions
        var allBudgetSuggestions = group.GroupParticipants
            .Select(gp => gp.BudgetSuggestion)
            .ToList();

        // Filter non-null suggestions and sort in ascending order
        var suggestions = allBudgetSuggestions
            .Where(bs => bs.HasValue)
            .Select(bs => bs!.Value)
            .OrderBy(amount => amount)
            .ToList();

        // Calculate counts
        var participantCount = group.GroupParticipants.Count;
        var suggestionsReceived = suggestions.Count;

        // Build response
        var response = new BudgetSuggestionsResponse
        {
            GroupId = group.Id,
            Suggestions = suggestions,
            Count = suggestions.Count,
            ParticipantCount = participantCount,
            SuggestionsReceived = suggestionsReceived,
            CurrentBudget = group.Budget
        };

        return Result<BudgetSuggestionsResponse>.Success(response);
    }
}
