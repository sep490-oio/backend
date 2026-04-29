namespace OIO.Application.Context.AuctionContext.Commands.SuggestItemDescription;

public sealed record CategorySuggestion(Guid Id, string Name, string Path, double Confidence);
