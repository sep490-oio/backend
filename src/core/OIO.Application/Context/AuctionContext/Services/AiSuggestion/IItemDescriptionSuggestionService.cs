using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Services.AiSuggestion;

public interface IItemDescriptionSuggestionService
{
    Task<Result<RawSuggestion, Error>> SuggestAsync(
        SuggestItemDescriptionInput input,
        CancellationToken ct);
}
