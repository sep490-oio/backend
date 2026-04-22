using MediatR;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Context.WarehouseContext.Services;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems.Events;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;

namespace OIO.Application.Context.WarehouseContext.EventHandlers;

/// <summary>
/// Handles <see cref="WarehouseInspectionRejectedEvent"/> by delegating to
/// <see cref="IWarehouseReturnShipmentFactory"/>. Keeps Tier-1 hard-fail
/// semantics (plan H3): address-missing / resolution failures surface as thrown
/// exceptions so the outbox retries up to MaxAttempts, then dead-letters.
/// Admin recovers via the <c>RetryPendingInspectionRejectCommand</c> endpoint.
/// </summary>
internal sealed class CreateWarehouseToSellerShipmentOnRejectionHandler(
    IWarehouseReturnShipmentFactory factory,
    IClock clock,
    ILogger<CreateWarehouseToSellerShipmentOnRejectionHandler> logger)
    : INotificationHandler<WarehouseInspectionRejectedEvent>
{
    public async Task Handle(WarehouseInspectionRejectedEvent notification, CancellationToken cancellationToken)
    {
        var inspectionId    = WarehouseInspectionId.From(notification.WarehouseInspectionId);
        var warehouseItemId = WarehouseItemId.From(notification.WarehouseItemId);
        var now             = clock.UtcNow;

        var result = await factory.EnsureShipmentExistsAsync(
            inspectionId:    inspectionId,
            warehouseItemId: warehouseItemId,
            rejectionReason: notification.Reason,
            nowUtc:          now,
            cancellationToken: cancellationToken);

        switch (result.Outcome)
        {
            case EnsureShipmentOutcome.Created:
            case EnsureShipmentOutcome.AlreadyExists:
            case EnsureShipmentOutcome.CreatedViaDbDedup:
                return;

            case EnsureShipmentOutcome.WarehouseItemNotFound:
                // Log-only: the warehouse item is gone (soft-deleted?) — outbox
                // retry won't help here, so we don't throw.
                logger.LogWarning(
                    "Inspection-reject handler: WarehouseItem missing for inspection {InspectionId}. Giving up without throw.",
                    notification.WarehouseInspectionId);
                return;

            case EnsureShipmentOutcome.SellerAddressMissing:
            case EnsureShipmentOutcome.SellerNotResolved:
                // Tier-1 hard-fail (plan H3). Throw so the outbox retries until
                // dead-letter. Admin intervenes via RetryPendingInspectionReject.
                throw new InvalidOperationException(
                    result.Error?.Message
                        ?? $"Cannot create warehouse→seller shipment for inspection {notification.WarehouseInspectionId}.");

            case EnsureShipmentOutcome.CreateFailed:
            case EnsureShipmentOutcome.TransitionFailed:
                throw new InvalidOperationException(
                    $"Failed to create warehouse→seller shipment for inspection {notification.WarehouseInspectionId}: " +
                    $"{result.Error?.Message ?? result.Outcome.ToString()}");

            default:
                throw new InvalidOperationException(
                    $"Unexpected EnsureShipmentOutcome '{result.Outcome}' for inspection {notification.WarehouseInspectionId}.");
        }
    }
}
