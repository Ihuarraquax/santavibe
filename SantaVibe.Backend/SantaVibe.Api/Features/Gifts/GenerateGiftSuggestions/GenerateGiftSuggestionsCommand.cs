using MediatR;
using SantaVibe.Api.Common;

namespace SantaVibe.Api.Features.Gifts.GenerateGiftSuggestions;

public record GenerateGiftSuggestionsCommand(Guid GroupId) : IRequest<Result<GiftSuggestionsResponse>>;
