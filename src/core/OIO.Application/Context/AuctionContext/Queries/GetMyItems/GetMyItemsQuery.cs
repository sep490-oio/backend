using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Domain.Context.CatalogContext.Enums;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Queries.GetMyItems;

public sealed record GetMyItemsQuery(GetMyItemsFilterParameters Parameters) : IQuery<PagedList<ItemDto>>, IHasValidate
{
    public ViolationsError Validate()
    {
        return GetMyItemsQuery.Check()
            .WithOwnerName("GetMyItems")
            .Field(Parameters.SortBy)
            .WhenHasValue(x => x.Must(ItemMappings.ItemDtoSortMapping.ValidateMappings))
            .Field(Parameters.Status)
            .WhenHasValue(x => x.InSet(ItemStatus.All.Select(status => status.Id)));
    }
}