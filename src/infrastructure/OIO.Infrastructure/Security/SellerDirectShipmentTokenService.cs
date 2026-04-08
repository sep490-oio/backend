using System.Text;
using System.Text.Json;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using OIO.Application.Context.OrderContext.Services;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Infrastructure.Security;

internal sealed class SellerDirectShipmentTokenService : ISellerDirectShipmentTokenService
{
    private const string Purpose = "seller-direct-shipment-qr-v1";

    private readonly IDataProtector _protector;

    public SellerDirectShipmentTokenService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector(Purpose);
    }

    public string Issue(
        SellerDirectShipmentId shipmentId,
        Guid orderId,
        Guid buyerId,
        int version,
        DateTime issuedAt)
    {
        var payload = new SellerDirectShipmentTokenPayload(
            ShipmentId: shipmentId.Value,
            OrderId: orderId,
            BuyerId: buyerId,
            Version: version,
            IssuedAt: DateTime.SpecifyKind(issuedAt, DateTimeKind.Utc));

        var json = JsonSerializer.SerializeToUtf8Bytes(payload);
        var protectedBytes = _protector.Protect(json);
        return WebEncoders.Base64UrlEncode(protectedBytes);
    }

    public Result<SellerDirectShipmentTokenPayload, Error> Validate(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Error.Validation(
                "token",
                "SellerDirectShipment.Token.Invalid",
                "Token is required.");

        try
        {
            var protectedBytes = WebEncoders.Base64UrlDecode(token);
            var json = _protector.Unprotect(protectedBytes);
            var payload = JsonSerializer.Deserialize<SellerDirectShipmentTokenPayload>(json);
            if (payload is null)
                return Error.Validation(
                    "token",
                    "SellerDirectShipment.Token.Invalid",
                    "Token payload could not be decoded.");

            return payload;
        }
        catch
        {
            return Error.Validation(
                "token",
                "SellerDirectShipment.Token.Invalid",
                "Token signature is invalid or has been tampered with.");
        }
    }
}
