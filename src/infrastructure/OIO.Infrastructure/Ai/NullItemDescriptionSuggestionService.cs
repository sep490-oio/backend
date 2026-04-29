using CSharpFunctionalExtensions;
using OIO.Application.Context.AuctionContext.Services.AiSuggestion;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Infrastructure.Ai;

/// <summary>
/// Safety-net implementation registered when <see cref="OIO.Application.Abstractions.Commons.AiOptions"/>
/// has <c>Enabled = false</c>. The handler should short-circuit on the same flag
/// before this service is invoked; this exists so DI resolution always succeeds.
/// </summary>
internal sealed class NullItemDescriptionSuggestionService : IItemDescriptionSuggestionService
{
    public Task<Result<RawSuggestion, Error>> SuggestAsync(
        SuggestItemDescriptionInput input,
        CancellationToken ct)
    {
        return Task.FromResult<Result<RawSuggestion, Error>>(AiSuggestionErrors.Disabled);
    }
}
