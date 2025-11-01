using MediatR;
using SantaVibe.Api.Common;

namespace SantaVibe.Api.Features.Groups.GetBudgetSuggestions;

/// <summary>
/// Query to retrieve anonymous budget suggestions for a group
/// </summary>
/// <param name="GroupId">The unique identifier of the group</param>
public record GetBudgetSuggestionsQuery(
    Guid GroupId
) : IRequest<Result<BudgetSuggestionsResponse>>;
