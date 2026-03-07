using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Domain.Context.AuctionContext.Aggregates.Items;

namespace OIO.Application.Context.AuctionContext.Mappings;

public static class ItemMappings {

    public static ItemDto ToDto(this Item item)
    {
        return new ItemDto(
            Id: item.Id.Value,
            SellerId: item.SellerId.Value,
            CategoryId: item.CategoryId?.Value,
            Title: item.Title,
            Description: item.Description,
            Condition: item.Condition.Id,
            Status: item.Status.Id,
            Quantity: item.Quantity,
            Images: item.Media.Select(i => i.ToDto()).ToList(),
            CreatedAt: item.CreatedAt);
    }
}