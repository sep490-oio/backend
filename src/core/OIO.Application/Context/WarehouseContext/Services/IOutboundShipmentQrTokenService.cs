using CSharpFunctionalExtensions;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Services;

/// <summary>
/// Issues and validates opaque, signed tokens embedded in the
/// outbound-shipment QR deep-link used by the external-carrier flow. Signing
/// uses ASP.NET Core Data Protection with a dedicated purpose string so tokens
/// are bound to this use case only.
/// </summary>
public interface IOutboundShipmentQrTokenService
{
    string Issue(
        OutboundShipmentId shipmentId,
        Guid orderId,
        Guid buyerId,
        int version,
        DateTime issuedAt);

    Result<OutboundShipmentQrTokenPayload, Error> Validate(string token);
}

public sealed record OutboundShipmentQrTokenPayload(
    Guid ShipmentId,
    Guid OrderId,
    Guid BuyerId,
    int Version,
    DateTime IssuedAt);
