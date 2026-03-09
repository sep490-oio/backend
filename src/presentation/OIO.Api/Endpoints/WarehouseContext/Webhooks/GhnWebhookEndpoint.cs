using MediatR;
using Microsoft.EntityFrameworkCore;
using OIO.Api.Common;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.WarehouseContext.Commands.ProcessTrackingWebhook;
using OIO.Domain.Context.WarehouseContext.Aggregates.ShippingProviders;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Infrastructure.Shipping;
using Microsoft.Extensions.Logging;

namespace OIO.Api.Endpoints.WarehouseContext.Webhooks;

/// <summary>
/// Receives carrier status-update webhooks from GHN.
///
/// GHN authenticates by embedding its ShopId in the JSON body — the adapter
/// verifies it matches our configured ShopId.  There is no HMAC / bearer token.
///
/// Convention: ALWAYS return 200 OK, even when parsing fails.
/// A non-200 response causes GHN to retry indefinitely.
/// Parse errors are logged and silently swallowed here.
/// </summary>
public sealed class GhnWebhookEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Warehouse.GhnWebhook, async (
                HttpRequest                httpRequest,
                ISender                    sender,
                IShippingProviderSelector  providerSelector,
                IDbContext                 dbContext,
                ILogger<GhnWebhookEndpoint> logger,
                CancellationToken          ct) =>
            {
                // ── 1. Read raw body ──────────────────────────────────────────
                string body;
                using (var reader = new StreamReader(httpRequest.Body, leaveOpen: true))
                {
                    body = await reader.ReadToEndAsync(ct);
                }

                if (string.IsNullOrWhiteSpace(body))
                {
                    logger.LogWarning("GHN webhook received empty body — ignoring.");
                    return Results.Ok();
                }

                // ── 2. Resolve GHN provider ───────────────────────────────────
                var providerResult = providerSelector.Select("ghn");
                if (providerResult.IsFailure)
                {
                    logger.LogError(
                        "GHN webhook: provider 'ghn' not registered — {Error}",
                        providerResult.Error.Message);
                    return Results.Ok(); // still 200
                }

                // ── 3. Load active GHN config from DB ────────────────────────
                var ghnCode = ShippingProviderCode.FromId("ghn");
                if (ghnCode.HasNoValue)
                {
                    logger.LogError("GHN webhook: 'ghn' is not a valid ShippingProviderCode.");
                    return Results.Ok();
                }

                var config = await dbContext.Set<ShippingProviderConfig>()
                    .FirstOrDefaultAsync(
                        c => c.ProviderCode == ghnCode.Value && c.IsActive,
                        ct);

                if (config is null)
                {
                    logger.LogError("GHN webhook: no active GHN ShippingProviderConfig found — ignoring.");
                    return Results.Ok();
                }

                // ── 4. Parse + verify webhook ─────────────────────────────────
                var parseResult = providerResult.Value.ParseWebhook(
                    body,
                    httpRequest.Headers,
                    httpRequest.Query,
                    config);

                if (parseResult.IsFailure)
                {
                    logger.LogWarning(
                        "GHN webhook parse/verification failed: {Code} — {Message}",
                        parseResult.Error.Code,
                        parseResult.Error.Message);
                    return Results.Ok(); // still 200 — not our error
                }

                var evt = parseResult.Value;

                // ── 5. Dispatch application command ───────────────────────────
                var command = new ProcessTrackingWebhookCommand(
                    ProviderCode:         evt.ProviderCode,
                    ClientOrderCode:      evt.ClientOrderCode,
                    CarrierTrackingNumber: evt.CarrierTrackingNumber,
                    CarrierStatusRaw:     evt.CarrierStatusRaw,
                    CarrierStatusDesc:    evt.CarrierStatusDesc,
                    NormalizedStatusId:   evt.NormalizedStatus.Id,
                    Location:             evt.Location,
                    ReasonCode:           evt.ReasonCode,
                    ReasonDescription:    evt.ReasonDescription,
                    EventTime:            evt.EventTime,
                    RawPayloadJson:       evt.RawPayloadJson);

                var result = await sender.Send(command, ct);

                if (result.IsFailure)
                {
                    logger.LogWarning(
                        "GHN webhook: ProcessTrackingWebhookCommand failed: {Code} — {Message}. " +
                        "ClientOrderCode={ClientOrderCode}, CarrierTracking={CarrierTracking}",
                        result.Error.Code,
                        result.Error.Message,
                        evt.ClientOrderCode,
                        evt.CarrierTrackingNumber);
                }

                // ── 6. Always 200 ─────────────────────────────────────────────
                return Results.Ok();
            })
            .AllowAnonymous()
            .WithName(ApiEndpoint.Names.Warehouse.GhnWebhook)
            .WithTags(ApiEndpoint.Tags.Webhooks)
            .Produces(StatusCodes.Status200OK)
            .ExcludeFromDescription(); // don't expose internals in Scalar/Swagger
    }
}