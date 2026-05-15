using OIO.Application.Abstractions.Commons;
using OIO.Domain.Context.CatalogContext.Enums;

namespace OIO.Application.Context.AuctionContext.Queries.Admins.GetAdminItems;

public record AdminItemFilterParameters : PagedParameters
{
    public Guid? CategoryId { get; init; }
    public string? Condition { get; init; }
    public string? Status { get; init; }
    public string? SearchTerm { get; init; }
    public string? PhysicalLocation { get; init; }
}
