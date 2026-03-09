using OIO.Application.Abstractions.Commons;

namespace OIO.Application.Context.AuctionContext.Queries.GetPublicItemQuestions;

public record GetPublicItemQuestionsFilterParameters : PagedParameters, ISortByParameter
{
    public string? SortBy { get; init; }
}