using System.Text.Json;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using OIO.Application.Context.WarehouseContext.Services;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Infrastructure.Security;

internal sealed class OutboundShipmentQrTokenService : IOutboundShipmentQrTokenService
{
    private const string Purpose = "outbound-shipment-qr:v1";

    private readonly IDataProtector _protector;

    public OutboundShipmentQrTokenService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector(Purpose);
    }

    public string Issue(
        OutboundShipmentId shipmentId,
        Guid orderId,
        Guid buyerId,
        int version,
        DateTime issuedAt)
    {
        var payload = new OutboundShipmentQrTokenPayload(
            ShipmentId: shipmentId.Value,
            OrderId: orderId,
            BuyerId: buyerId,
            Version: version,
            IssuedAt: DateTime.SpecifyKind(issuedAt, DateTimeKind.Utc));

        var json = JsonSerializer.SerializeToUtf8Bytes(payload);
        var protectedBytes = _protector.Protect(json);
        return WebEncoders.Base64UrlEncode(protectedBytes);
    }

    public Result<OutboundShipmentQrTokenPayload, Error> Validate(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Error.Validation(
                "token",
                "OutboundShipmentQr.InvalidToken",
                "Token is required.");

        try
        {
            var protectedBytes = WebEncoders.Base64UrlDecode(token);
            var json = _protector.Unprotect(protectedBytes);
            var payload = JsonSerializer.Deserialize<OutboundShipmentQrTokenPayload>(json);
            if (payload is null)
                return Error.Validation(
                    "token",
                    "OutboundShipmentQr.InvalidToken",
                    "Token payload could not be decoded.");

            return payload;
        }
        catch
        {
            return Error.Validation(
                "token",
                "OutboundShipmentQr.InvalidToken",
                "Token signature is invalid or has been tampered with.");
        }
    }
}
