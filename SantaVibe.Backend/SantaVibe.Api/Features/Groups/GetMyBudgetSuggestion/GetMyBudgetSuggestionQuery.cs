using MediatR;
using SantaVibe.Api.Common;

namespace SantaVibe.Api.Features.Groups.GetMyBudgetSuggestion;

public record GetMyBudgetSuggestionQuery(
    Guid GroupId
) : IRequest<Result<GetMyBudgetSuggestionResponse>>;
