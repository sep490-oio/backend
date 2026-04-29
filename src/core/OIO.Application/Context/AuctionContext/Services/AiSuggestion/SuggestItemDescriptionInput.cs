namespace OIO.Application.Context.AuctionContext.Services.AiSuggestion;

public sealed record SuggestItemDescriptionInput(
    string Title,
    string Condition,
    IReadOnlyList<ImageRef> Images,
    IReadOnlyList<CategoryReference> ActiveCategories,
    string Locale);
