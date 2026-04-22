using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Abstractions.Security;

/// <summary>
/// Issues and validates opaque, signed tokens embedded in the return-shipment
/// QR deep-link. Mirrors <c>IOutboundShipmentQrTokenService</c> but scoped to
/// the return flows (buyer-ship-back + warehouse-to-seller) with a distinct
/// purpose string so tokens cannot cross-validate between outbound and return.
/// Signing uses ASP.NET Core Data Protection. 30-day TTL enforced in payload.
/// </summary>
public interface IReturnShipmentQrTokenService
{
    /// <param name="kind">"order_return" or "warehouse_to_seller".</param>
    /// <param name="shipmentOrReturnId">The OrderReturnId or WarehouseToSellerShipmentId the token is bound to.</param>
    /// <param name="issuedAt">UTC issue timestamp stamped into the payload.</param>
    /// <param name="expiresAt">UTC expiry timestamp — <see cref="Validate"/> self-rejects after this.</param>
    string Issue(
        string kind,
        Guid shipmentOrReturnId,
        DateTime issuedAt,
        DateTime expiresAt);

    /// <summary>
    /// Verifies the token signature + decodes the payload. Does NOT check
    /// <see cref="ReturnShipmentQrTokenPayload.ExpiresAt"/> — caller must.
    /// </summary>
    Result<ReturnShipmentQrTokenPayload, Error> Validate(string token);
}

public sealed record ReturnShipmentQrTokenPayload(
    string Kind,
    Guid ShipmentOrReturnId,
    DateTime IssuedAt,
    DateTime ExpiresAt);
