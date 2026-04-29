namespace OIO.Application.Context.AuctionContext.Commands.SuggestItemDescription;

public sealed record SuggestItemDescriptionResult(
    string Description,
    string DescriptionFormat,
    CategorySuggestion? SuggestedCategory,
    IReadOnlyList<CategorySuggestion> Alternatives,
    IReadOnlyList<string> Warnings);
