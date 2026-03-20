using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Commands.BookInboundShipment;

public sealed record BookInboundShipmentCommand(
    Guid    ItemId,
    int     WeightGrams,
    decimal InsuranceValue,
    string  ItemName,
    decimal ItemPrice,
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
) : ICommand<InboundShipmentDto>, IHasValidate
{
    public ViolationsError Validate() =>
        BookInboundShipmentCommand.Check()
            .WithOwnerName("BookInboundShipment")
            .Field(ItemId).NotEmptyGuid()
            .Field(WeightGrams).GreaterThan(0)
            .Field(InsuranceValue).NonNegative()
            .Field(ItemName).NotWhiteSpace()
            .Field(ItemPrice).NonNegative();
}