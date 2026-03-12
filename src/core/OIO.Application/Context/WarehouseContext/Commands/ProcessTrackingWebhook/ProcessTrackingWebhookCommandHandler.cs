using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.Context.WarehouseContext.ValueObjects;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Commands.ProcessTrackingWebhook;

internal sealed class ProcessTrackingWebhookCommandHandler
    : ICommandHandler<ProcessTrackingWebhookCommand>
{
    private readonly IDbContext  _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock      _clock;

    public ProcessTrackingWebhookCommandHandler(
        IDbContext  dbContext,
        IUnitOfWork unitOfWork,
        IClock      clock)
    {
        _dbContext  = dbContext;
        _unitOfWork = unitOfWork;
        _clock      = clock;
    }

    public async Task<UnitResult<Error>> Handle(
        ProcessTrackingWebhookCommand request,
        CancellationToken             cancellationToken)
    {
        var now = _clock.UtcNow;

        // ── 1. Resolve domain enums ───────────────────────────────────────────
        var providerCode = ShippingProviderCode.FromId(request.ProviderCode);
        if (providerCode.HasNoValue)
            return WarehouseErrors.ShippingProvider.NotFound(request.ProviderCode);

        var normalizedStatus = NormalizedTrackingStatus.FromId(request.NormalizedStatusId);
        if (normalizedStatus.HasNoValue)
            return Error.Unexpected(
                code: "Webhook.NormalizedStatus.Unknown",
                description: $"Unknown normalized status '{request.NormalizedStatusId}'.");

        var rawPayload = WebhookRawPayload.From(request.RawPayloadJson);

        // ── 2. Determine shipment type from ClientOrderCode prefix ────────────
        // INB-xxx → inbound, OUT-xxx → outbound.
        // If ClientOrderCode is absent, fall back to CarrierTrackingNumber search.
        var isInbound  = request.ClientOrderCode?.StartsWith("INB-", StringComparison.OrdinalIgnoreCase) == true;
        var isOutbound = request.ClientOrderCode?.StartsWith("OUT-", StringComparison.OrdinalIgnoreCase) == true;

        if (!isInbound && !isOutbound)
        {
            // No recognizable prefix — try CarrierTrackingNumber across both tables
            return await ProcessByCarrierTrackingNumberAsync(
                request, providerCode.Value, normalizedStatus.Value, rawPayload, now, cancellationToken);
        }

        if (isInbound)
            return await ProcessInboundAsync(
                request, providerCode.Value, normalizedStatus.Value, rawPayload, now, cancellationToken);

        return await ProcessOutboundAsync(
            request, providerCode.Value, normalizedStatus.Value, rawPayload, now, cancellationToken);
    }

    // ── Inbound ───────────────────────────────────────────────────────────────

    private async Task<UnitResult<Error>> ProcessInboundAsync(
        ProcessTrackingWebhookCommand request,
        ShippingProviderCode          providerCode,
        NormalizedTrackingStatus      normalizedStatus,
        WebhookRawPayload             rawPayload,
        DateTime                      now,
        CancellationToken             ct)
    {
        var shipment = await _dbContext.Set<InboundShipment>()
            .FirstOrDefaultAsync(
                s => s.ClientOrderCode == request.ClientOrderCode,
                ct);

        if (shipment is null)
            return WarehouseErrors.InboundShipment.NotFound(request.ClientOrderCode ?? "");

        shipment.RecordTrackingEvent(
            providerCode,
            request.CarrierStatusRaw,
            request.CarrierStatusDesc,
            normalizedStatus,
            request.Location,
            request.ReasonCode,
            request.ReasonDescription,
            request.EventTime,
            rawPayload,
            now);

        _dbContext.Update(shipment);
        await _unitOfWork.SaveChangesAsync(ct);

        return UnitResult.Success<Error>();
    }

    // ── Outbound ──────────────────────────────────────────────────────────────

    private async Task<UnitResult<Error>> ProcessOutboundAsync(
        ProcessTrackingWebhookCommand request,
        ShippingProviderCode          providerCode,
        NormalizedTrackingStatus      normalizedStatus,
        WebhookRawPayload             rawPayload,
        DateTime                      now,
        CancellationToken             ct)
    {
        var shipment = await _dbContext.Set<OutboundShipment>()
            .FirstOrDefaultAsync(
                s => s.ClientOrderCode == request.ClientOrderCode,
                ct);

        if (shipment is null)
            return WarehouseErrors.OutboundShipment.NotFound(request.ClientOrderCode ?? "");

        shipment.RecordTrackingEvent(
            providerCode,
            request.CarrierStatusRaw,
            request.CarrierStatusDesc,
            normalizedStatus,
            request.Location,
            request.ReasonCode,
            request.ReasonDescription,
            request.EventTime,
            rawPayload,
            now);

        _dbContext.Update(shipment);
        await _unitOfWork.SaveChangesAsync(ct);

        return UnitResult.Success<Error>();
    }

    // ── Fallback: search by CarrierTrackingNumber ─────────────────────────────

    private async Task<UnitResult<Error>> ProcessByCarrierTrackingNumberAsync(
        ProcessTrackingWebhookCommand request,
        ShippingProviderCode          providerCode,
        NormalizedTrackingStatus      normalizedStatus,
        WebhookRawPayload             rawPayload,
        DateTime                      now,
        CancellationToken             ct)
    {
        if (string.IsNullOrWhiteSpace(request.CarrierTrackingNumber))
            return Error.Unexpected(
                code: "Webhook.NoReference",
                description: "Webhook contains neither a recognizable ClientOrderCode nor a CarrierTrackingNumber.");

        // Try inbound first
        var inbound = await _dbContext.Set<InboundShipment>()
            .FirstOrDefaultAsync(
                s => s.CarrierTrackingNumber == request.CarrierTrackingNumber,
                ct);

        if (inbound is not null)
        {
            inbound.RecordTrackingEvent(
                providerCode, request.CarrierStatusRaw, request.CarrierStatusDesc,
                normalizedStatus, request.Location, request.ReasonCode,
                request.ReasonDescription, request.EventTime, rawPayload, now);

            _dbContext.Update(inbound);
            await _unitOfWork.SaveChangesAsync(ct);
            return UnitResult.Success<Error>();
        }

        // Try outbound
        var outbound = await _dbContext.Set<OutboundShipment>()
            .FirstOrDefaultAsync(
                s => s.CarrierTrackingNumber == request.CarrierTrackingNumber,
                ct);

        if (outbound is not null)
        {
            outbound.RecordTrackingEvent(
                providerCode, request.CarrierStatusRaw, request.CarrierStatusDesc,
                normalizedStatus, request.Location, request.ReasonCode,
                request.ReasonDescription, request.EventTime, rawPayload, now);

            _dbContext.Update(outbound);
            await _unitOfWork.SaveChangesAsync(ct);
            return UnitResult.Success<Error>();
        }

        return Error.NotFound(
            code: "Webhook.Shipment.NotFound",
            description: $"No shipment found for CarrierTrackingNumber '{request.CarrierTrackingNumber}'.");
    }
}