using CSharpFunctionalExtensions;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.OrderContext.Services;

/// <summary>
/// Issues and validates opaque, signed tokens embedded in the seller-direct
/// shipment QR deep-link. Signing uses ASP.NET Core Data Protection with a
/// dedicated purpose string so tokens are bound to this use case only.
/// </summary>
public interface ISellerDirectShipmentTokenService
{
    string Issue(
        SellerDirectShipmentId shipmentId,
        Guid orderId,
        Guid buyerId,
        int version,
        DateTime issuedAt);

    Result<SellerDirectShipmentTokenPayload, Error> Validate(string token);
}

public sealed record SellerDirectShipmentTokenPayload(
    Guid ShipmentId,
    Guid OrderId,
    Guid BuyerId,
    int Version,
    DateTime IssuedAt);
