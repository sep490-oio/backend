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
using Microsoft.Extensions.Logging;
using OIO.Domain.SeedWork.Errors;
using e = OIO.Domain.SeedWork.Errors.Error;

namespace OIO.Application.Context.WarehouseContext.Commands.ProcessTrackingWebhook;

internal sealed class ProcessTrackingWebhookCommandHandler
    : ICommandHandler<ProcessTrackingWebhookCommand>
{
    private readonly IDbContext  _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock      _clock;
    private readonly ILogger<ProcessTrackingWebhookCommandHandler> _logger;

    public ProcessTrackingWebhookCommandHandler(
        IDbContext  dbContext,
        IUnitOfWork unitOfWork,
        IClock      clock,
        ILogger<ProcessTrackingWebhookCommandHandler> logger)
    {
        _dbContext  = dbContext;
        _unitOfWork = unitOfWork;
        _clock      = clock;
        _logger     = logger;
    }

    public async Task<UnitResult<e>> Handle(
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
        // INB-xxx → inbound, OUT-xxx → outbound, EXT-xxx → external (skip).
        var isInbound  = request.ClientOrderCode?.StartsWith("INB-", StringComparison.OrdinalIgnoreCase) == true;
        var isOutbound = request.ClientOrderCode?.StartsWith("OUT-", StringComparison.OrdinalIgnoreCase) == true;
        var isExternal = request.ClientOrderCode?.StartsWith("EXT-", StringComparison.OrdinalIgnoreCase) == true;

        if (isExternal)
            return UnitResult.Success<e>();

        if (!isInbound && !isOutbound)
        {
            return await ProcessByCarrierTrackingNumberAsync(
                request, providerCode.Value, normalizedStatus.Value, rawPayload, now, cancellationToken);
        }

        if (isInbound)
            return await ProcessInboundAsync(
                request, providerCode.Value, normalizedStatus.Value, rawPayload, now, cancellationToken);

        return await ProcessOutboundAsync(
            request, providerCode.Value, normalizedStatus.Value, rawPayload, now, cancellationToken);
    }

    private async Task<UnitResult<e>> ProcessInboundAsync(
        ProcessTrackingWebhookCommand request,
        ShippingProviderCode          providerCode,
        NormalizedTrackingStatus      normalizedStatus,
        WebhookRawPayload             rawPayload,
        DateTime                      now,
        CancellationToken             ct)
    {
        var shipment = await _dbContext.Set<InboundShipment>()
            .FirstOrDefaultAsync(s => s.ClientOrderCode == request.ClientOrderCode, ct);

        if (shipment is null)
            return WarehouseErrors.InboundShipment.NotFound(request.ClientOrderCode ?? "");

        var result = shipment.RecordTrackingEvent(
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

        if (result.IsFailure) return result.Error;

        // If normalized status indicates arrival, also record arrived to trigger domain event
        if (normalizedStatus == NormalizedTrackingStatus.Arrived &&
            shipment.Status != InboundShipmentStatus.Arrived)
        {
            var arrivedResult = shipment.RecordArrived(now);
            if (arrivedResult.IsFailure)
            {
                _logger.LogWarning(
                    "RecordArrived failed for inbound shipment {ShipmentId}: {Error}",
                    shipment.Id, arrivedResult.Error);
            }
        }

        _dbContext.Update(shipment);
        await _unitOfWork.SaveChangesAsync(ct);

        return UnitResult.Success<e>();
    }

    private async Task<UnitResult<e>> ProcessOutboundAsync(
        ProcessTrackingWebhookCommand request,
        ShippingProviderCode          providerCode,
        NormalizedTrackingStatus      normalizedStatus,
        WebhookRawPayload             rawPayload,
        DateTime                      now,
        CancellationToken             ct)
    {
        var shipment = await _dbContext.Set<OutboundShipment>()
            .FirstOrDefaultAsync(s => s.ClientOrderCode == request.ClientOrderCode, ct);

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

        return UnitResult.Success<e>();
    }

    private async Task<UnitResult<e>> ProcessByCarrierTrackingNumberAsync(
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

        var inbound = await _dbContext.Set<InboundShipment>()
            .FirstOrDefaultAsync(s => s.CarrierTrackingNumber == request.CarrierTrackingNumber, ct);

        if (inbound is not null)
        {
            var result = inbound.RecordTrackingEvent(
                providerCode, request.CarrierStatusRaw, request.CarrierStatusDesc,
                normalizedStatus, request.Location, request.ReasonCode,
                request.ReasonDescription, request.EventTime, rawPayload, now);

            if (result.IsFailure) return result.Error;

            // If arrived, trigger domain event (matching primary path behavior)
            if (normalizedStatus == NormalizedTrackingStatus.Arrived &&
                inbound.Status != InboundShipmentStatus.Arrived)
            {
                var arrivedResult = inbound.RecordArrived(now);
                if (arrivedResult.IsFailure)
                {
                    _logger.LogWarning(
                        "RecordArrived failed for inbound shipment {ShipmentId} (carrier fallback): {Error}",
                        inbound.Id, arrivedResult.Error);
                }
            }

            _dbContext.Update(inbound);
            await _unitOfWork.SaveChangesAsync(ct);
            return UnitResult.Success<e>();
        }

        var outbound = await _dbContext.Set<OutboundShipment>()
            .FirstOrDefaultAsync(s => s.CarrierTrackingNumber == request.CarrierTrackingNumber, ct);

        if (outbound is not null)
        {
            outbound.RecordTrackingEvent(
                providerCode, request.CarrierStatusRaw, request.CarrierStatusDesc,
                normalizedStatus, request.Location, request.ReasonCode,
                request.ReasonDescription, request.EventTime, rawPayload, now);

            _dbContext.Update(outbound);
            await _unitOfWork.SaveChangesAsync(ct);
            return UnitResult.Success<e>();
        }

        return Error.NotFound(
            code: "Webhook.Shipment.NotFound",
            description: $"No shipment found for CarrierTrackingNumber '{request.CarrierTrackingNumber}'.");
    }
}