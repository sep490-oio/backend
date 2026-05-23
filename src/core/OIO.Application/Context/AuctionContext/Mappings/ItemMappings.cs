using OIO.Application.Abstractions.Sorting;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;

namespace OIO.Application.Context.AuctionContext.Mappings;

public static class ItemMappings {

    public static ItemDto ToDto(this Item item, bool hasInboundShipment = false, bool hasLiveAuction = false, ItemAuctionSummaryDto? auction = null)
    {
        return new ItemDto(
            Id: item.Id.Value,
            SellerId: item.SellerId.Value,
            CategoryId: item.CategoryId?.Value,
            Title: item.Title.Value,
            Description: item.Description,
            Condition: item.Condition.Id,
            Status: item.Status.Id,
            Quantity: item.Quantity,
            Images: item.Media.Select(i => i.ToDto()).ToList(),
            CreatedAt: item.CreatedAt,
            HasInboundShipment: hasInboundShipment,
            HasLiveAuction: hasLiveAuction,
            Auction: auction);
    }
    
    public static readonly SortMappingDefinition ItemDtoSortMapping = SortMappingBuilder<ItemDto, Item>
        .Create()
        .Map(x => x.Id, i => i.Id)
        .Map(x => x.SellerId, i => i.SellerId)
        .Map(x => x.CategoryId, i => i.CategoryId)
        .Map(x => x.Title, i => i.Title)
        .Map(x => x.Condition, i => i.Condition.Id)
        .Map(x => x.Status, i => i.Status.Id)
        .Map(x => x.Quantity, i => i.Quantity)
        .Map(x => x.CreatedAt, i => i.CreatedAt)
        .Build();
}