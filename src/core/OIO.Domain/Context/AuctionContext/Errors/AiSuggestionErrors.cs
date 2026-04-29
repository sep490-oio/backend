using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.AuctionContext.Errors;

public static class AiSuggestionErrors
{
    public static readonly Error Disabled = Error.Conflict(
        "AiSuggestion.Disabled",
        "AI description suggestion is disabled.");

    public static readonly Error ProviderUnavailable = Error.Unavailable(
        "AiSuggestion.ProviderUnavailable",
        "AI provider is unavailable. Please try again later.");

    public static readonly Error RateLimited = Error.Conflict(
        "AiSuggestion.RateLimited",
        "Too many suggestion requests. Please wait and try again.");

    public static readonly Error InvalidModelOutput = Error.Unexpected(
        "AiSuggestion.InvalidModelOutput",
        "AI model returned an unusable description. Please try again or write the description manually.");
}
