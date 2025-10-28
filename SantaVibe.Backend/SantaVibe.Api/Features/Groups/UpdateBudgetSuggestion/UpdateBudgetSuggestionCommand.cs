using MediatR;
using SantaVibe.Api.Common;

namespace SantaVibe.Api.Features.Groups.UpdateBudgetSuggestion;

public record UpdateBudgetSuggestionCommand(
    Guid GroupId,
    decimal? BudgetSuggestion
) : IRequest<Result<UpdateBudgetSuggestionResponse>>;
