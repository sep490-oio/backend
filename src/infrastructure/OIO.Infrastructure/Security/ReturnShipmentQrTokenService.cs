using System.Text.Json;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using OIO.Application.Abstractions.Security;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Infrastructure.Security;

internal sealed class ReturnShipmentQrTokenService : IReturnShipmentQrTokenService
{
    // Distinct purpose string from outbound — tokens cannot cross-validate even
    // if an attacker swaps the bearer context.
    private const string Purpose = "return-shipment-qr:v1";

    private readonly IDataProtector _protector;

    public ReturnShipmentQrTokenService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector(Purpose);
    }

    public string Issue(
        string kind,
        Guid shipmentOrReturnId,
        DateTime issuedAt,
        DateTime expiresAt)
    {
        var payload = new ReturnShipmentQrTokenPayload(
            Kind: kind,
            ShipmentOrReturnId: shipmentOrReturnId,
            IssuedAt: DateTime.SpecifyKind(issuedAt, DateTimeKind.Utc),
            ExpiresAt: DateTime.SpecifyKind(expiresAt, DateTimeKind.Utc));

        var json = JsonSerializer.SerializeToUtf8Bytes(payload);
        var protectedBytes = _protector.Protect(json);
        return WebEncoders.Base64UrlEncode(protectedBytes);
    }

    public Result<ReturnShipmentQrTokenPayload, Error> Validate(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Error.Validation(
                "token",
                "ReturnShipmentQr.InvalidToken",
                "Token is required.");

        try
        {
            var protectedBytes = WebEncoders.Base64UrlDecode(token);
            var json = _protector.Unprotect(protectedBytes);
            var payload = JsonSerializer.Deserialize<ReturnShipmentQrTokenPayload>(json);
            if (payload is null)
                return Error.Validation(
                    "token",
                    "ReturnShipmentQr.InvalidToken",
                    "Token payload could not be decoded.");

            return payload;
        }
        catch
        {
            return Error.Validation(
                "token",
                "ReturnShipmentQr.InvalidToken",
                "Token signature is invalid or has been tampered with.");
        }
    }
}
