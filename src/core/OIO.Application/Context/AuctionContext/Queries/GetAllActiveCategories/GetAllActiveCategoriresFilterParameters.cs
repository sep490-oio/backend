using OIO.Application.Abstractions.Commons;

namespace OIO.Application.Context.AuctionContext.Queries.GetAllActiveCategories;

public record GetAllActiveCategoriresFilterParameters 
    : PagedParameters, ISortByParameter
{
    public string? SortBy { get; init; }
}