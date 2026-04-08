using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Commands.SelfShipOrder;

/// <summary>
/// Used for "Not-verify" flow where seller ships directly to buyer.
/// </summary>
public sealed record SelfShipOrderCommand(
    Guid   OrderId,
    string ExternalCarrierName,
    string CarrierTrackingNumber,
    // Weight is required and must be positive — PackageDimensions rejects 0/negative.
    int     WeightGrams,
    decimal InsuranceValue = 0,
    string? ShippingMethod = null
) : ICommand<OutboundShipmentDto>, IHasValidate
{
    public ViolationsError Validate() =>
        SelfShipOrderCommand.Check()
            .WithOwnerName("SelfShipOrder")
            .Field(OrderId).NotEmptyGuid()
            .Field(ExternalCarrierName).NotWhiteSpace()
            .Field(CarrierTrackingNumber).NotWhiteSpace()
            .Field(WeightGrams).GreaterThan(0);
}
