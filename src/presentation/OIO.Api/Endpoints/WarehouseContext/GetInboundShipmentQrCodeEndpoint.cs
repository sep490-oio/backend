using OIO.Api.Common;
using OIO.Domain.AppDefinitions;
using QRCoder;

namespace OIO.Api.Endpoints.WarehouseContext;

/// <summary>
/// Returns a QR code PNG image for the given inbound shipment.
///
/// Optional query parameters:
///   pixelSize   — pixels per module (default: 10, range: 5–20)
///   darkColor   — foreground hex color without # (default: 000000)
///   lightColor  — background hex color without # (default: ffffff)
///   drawQuietZones — include quiet-zone border (default: true)
/// </summary>
public sealed class GetInboundShipmentQrCodeEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Warehouse.QrCode, (
                Guid    shipmentId,
                int?    pixelSize      = null,
                string? darkColor      = null,
                string? lightColor     = null,
                bool    drawQuietZones = true) =>
            {
                // Clamp pixel size
                var moduleSize = Math.Clamp(pixelSize ?? 10, 5, 20);

                // Parse hex colors — fall back to black on white if invalid
                var dark  = ParseHex(darkColor,  0x00, 0x00, 0x00);
                var light = ParseHex(lightColor, 0xFF, 0xFF, 0xFF);

                var content = shipmentId.ToString();

                using var qrGenerator = new QRCodeGenerator();
                using var qrData      = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
                using var qrCode      = new PngByteQRCode(qrData);

                var pngBytes = qrCode.GetGraphic(
                    pixelsPerModule: moduleSize,
                    darkColorRgba:   dark,
                    lightColorRgba:  light,
                    drawQuietZones:  drawQuietZones);

                return Results.File(pngBytes, contentType: "image/png",
                    fileDownloadName: $"shipment-{shipmentId}.png");
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.ReadShipments)
            .WithName(ApiEndpoint.Names.Warehouse.GetInboundShipmentQrCode)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces<byte[]>(StatusCodes.Status200OK, contentType: "image/png")
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    /// <summary>
    /// Parses a 6-character hex string (e.g. "1A2B3C") into an RGBA byte array.
    /// Falls back to the supplied default RGB values if parsing fails.
    /// </summary>
    private static byte[] ParseHex(string? hex, byte defaultR, byte defaultG, byte defaultB)
    {
        if (!string.IsNullOrWhiteSpace(hex))
        {
            hex = hex.TrimStart('#');
            if (hex.Length == 6 &&
                byte.TryParse(hex[0..2], System.Globalization.NumberStyles.HexNumber, null, out var r) &&
                byte.TryParse(hex[2..4], System.Globalization.NumberStyles.HexNumber, null, out var g) &&
                byte.TryParse(hex[4..6], System.Globalization.NumberStyles.HexNumber, null, out var b))
            {
                return [r, g, b, 255]; // fully opaque
            }
        }

        return [defaultR, defaultG, defaultB, 255];
    }
}
