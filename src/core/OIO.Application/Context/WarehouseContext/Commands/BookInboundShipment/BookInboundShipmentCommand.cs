using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Commands.BookInboundShipment;

/// <summary>
/// Represents a single item to be included in a batch inbound shipment booking.
/// ItemName is auto-resolved from the Items table (Item.Title).
/// </summary>
public sealed record BookInboundShipmentItem(
    Guid     ItemId,
    decimal? ItemPrice,   // Optional: per-item custom price declared to carrier
    int      WeightGrams  // per-item weight reported to carrier in Items[] metadata
);

/// <summary>
/// Books a batch inbound shipment: creates ONE carrier order (GHN) for all items,
/// but creates ONE InboundShipment record per ItemId so staff can process each item independently.
/// All records in the batch share the same ClientOrderCode and CarrierTrackingNumber.
/// </summary>
public sealed record BookInboundShipmentCommand(
    List<BookInboundShipmentItem> Items,
    int     WeightGrams,       // total package weight sent to carrier
    decimal InsuranceValue,    // total insurance value for the batch
    string? SenderName                   = null,
    string? SenderPhone                  = null,
    string? SenderAddress                = null,
    string? SenderWard                   = null,
    string? SenderDistrict               = null,
    string? SenderProvince               = null,
    string? ShipmentMode                   = null,   // null = platform_managed
    string? ExternalCarrierName            = null,   // required when ShipmentMode = external_carrier
    int?    LengthCm                       = null,
    int?    WidthCm                        = null,
    int?    HeightCm                       = null,
    string? SenderCarrierAddressDataJson   = null,
    string? ProviderCode                   = null,   // null = use default active provider
    string? Notes                          = null,
    string? GhnHandlingNote                = null    // CHOTHUHANG | CHOXEMHANGKHONGTHU | KHONGCHOXEMHANG
) : ICommand<List<InboundShipmentDto>>, IHasValidate
{
    public ViolationsError Validate()
    {
        var check = BookInboundShipmentCommand.Check()
            .WithOwnerName("BookInboundShipment")
            .Field(Items.Count, nameof(Items)).GreaterThan(0)
            .Field(WeightGrams).GreaterThan(0)
            .Field(InsuranceValue).NonNegative();

        return check;
    }
}