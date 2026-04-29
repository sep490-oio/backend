namespace OIO.Application.Context.AuctionContext.Services.AiSuggestion;

public sealed record RawSuggestion(
    string Description,
    Guid? SuggestedCategoryId,
    double SuggestedCategoryConfidence,
    IReadOnlyList<RawAlternative> Alternatives,
    IReadOnlyList<string> Warnings);
