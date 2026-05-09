using System.Text.Json;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Security;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.CatalogContext.ValueObjects.Ids;
using OIO.Domain.Context.ModerationContext.Aggregates.Disputes;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Enums;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;
using OIO.Application.Context.OrderContext.Services;

namespace OIO.Application.Context.ModerationContext.Services;

public sealed record DisputeResolutionActionSet
{
    public string? EscrowAction { get; init; }
    public string? RefundAction { get; init; }
    public decimal? RefundAmount { get; init; }
    public string? ShipmentAction { get; init; }
    public string? ItemAction { get; init; }
    public string? AuctionAction { get; init; }
    public string? PenaltyAction { get; init; }

    /// <summary>
    /// Optional fee-payer selector for <c>open_return</c>. Accepts <c>"buyer"</c>,
    /// <c>"seller"</c>, or <c>"platform"</c>; parsed via
    /// <see cref="ShippingFeePayer.FromId"/>. Defaults to Buyer when null / invalid.
    /// </summary>
    public string? ReturnShippingFeePayer { get; init; }

    /// <summary>
    /// Optional override for the buyer's ship-back decision window. Null ⇒ 7 days.
    /// </summary>
    public int? BuyerDecisionDueDays { get; init; }
}

internal sealed class DisputeResolutionService : IDisputeResolutionService
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly EscrowSettlementService _escrowSettlementService;
    private readonly IReturnShipmentQrTokenService _qrTokenService;
    private readonly ILogger<DisputeResolutionService> _logger;

    public DisputeResolutionService(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock,
        EscrowSettlementService escrowSettlementService,
        IReturnShipmentQrTokenService qrTokenService,
        ILogger<DisputeResolutionService> logger)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _escrowSettlementService = escrowSettlementService;
        _qrTokenService = qrTokenService;
        _logger = logger;
    }

    public async Task<UnitResult<Error>> ApplyResolutionAsync(Dispute dispute, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dispute.ResolutionActionSetJson))
        {
            _logger.LogDebug(
                "Dispute {DisputeId} has no resolution action set — skipping side-effects",
                dispute.Id);
            return UnitResult.Success<Error>();
        }

        DisputeResolutionActionSet? actionSet;
        try
        {
            actionSet = JsonSerializer.Deserialize<DisputeResolutionActionSet>(
                dispute.ResolutionActionSetJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex,
                "Failed to deserialize ResolutionActionSetJson for Dispute {DisputeId}",
                dispute.Id);
            return Error.Validation("ResolutionActionSetJson", "Dispute.InvalidActionSet",
                "Resolution action set JSON is malformed.");
        }

        if (actionSet is null)
            return UnitResult.Success<Error>();

        // Load the order if the dispute has an OrderId
        Order? order = dispute.OrderId.Value != Guid.Empty
            ? await _dbContext.GetByIdAsync<Order, OrderId>(dispute.OrderId, cancellationToken: ct)
            : null;

        var now = _clock.UtcNow;

        // D8: compute deferred-refund intent BEFORE escrow/refund actions so both
        // call sites can short-circuit when the resolution also opens a return.
        // OR-composition — handles EscrowAction=refund_buyer AND RefundAction=full_refund
        // simultaneously (moderator ActionSet has no mutual exclusion).
        var (deferredIntent, deferredAmount) = ComputeDeferredRefundIntent(actionSet);
        var skipRefund = IsOpenReturn(actionSet)
            && deferredIntent != DeferredRefundIntent.None;

        // 1. Escrow action (skip refund sub-cases when deferring)
        var escrowActionResult = await ApplyEscrowActionAsync(actionSet, order, dispute, skipRefund, ct);
        if (escrowActionResult.IsFailure)
            return escrowActionResult.Error;

        // 2. Refund action (skip refund sub-cases when deferring)
        var refundActionResult = await ApplyRefundActionAsync(actionSet, order, dispute, skipRefund, ct);
        if (refundActionResult.IsFailure)
            return refundActionResult.Error;

        // 3. Shipment action — carries the computed deferredIntent + amount into
        //    Order.OpenReturnViaDispute when ShipmentAction == "open_return".
        await ApplyShipmentActionAsync(actionSet, dispute, deferredIntent, deferredAmount, now, ct);

        // 4. Item action
        await ApplyItemActionAsync(actionSet, dispute, now, ct);

        // 5. Auction action
        await ApplyAuctionActionAsync(actionSet, dispute, now, ct);

        // 6. Penalty action (V1: flag only, no automated enforcement)
        ApplyPenaltyAction(actionSet, dispute);

        await _unitOfWork.SaveChangesAsync(ct);
        return UnitResult.Success<Error>();
    }

    private async Task<UnitResult<Error>> ApplyEscrowActionAsync(
        DisputeResolutionActionSet actionSet,
        Order? order,
        Dispute dispute,
        bool skipRefund,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(actionSet.EscrowAction) || actionSet.EscrowAction == "no_action")
            return UnitResult.Success<Error>();

        if (order is null)
        {
            _logger.LogWarning(
                "Dispute {DisputeId}: escrow action '{Action}' skipped — no linked order",
                dispute.Id, actionSet.EscrowAction);
            return UnitResult.Success<Error>();
        }

        try
        {
            switch (actionSet.EscrowAction)
            {
                case "release_to_seller":
                {
                    var result = await _escrowSettlementService.ReleaseToSellerAsync(
                        order, "Dispute resolution: release to seller", null, ct);
                    if (result.IsFailure)
                    {
                        _logger.LogWarning(
                            "Dispute {DisputeId}: escrow release_to_seller failed — {Error}",
                            dispute.Id, result.Error.Message);
                        return result.Error;
                    }
                    break;
                }

                case "refund_buyer":
                {
                    if (skipRefund)
                    {
                        _logger.LogInformation(
                            "Dispute {DisputeId}: escrow refund_buyer DEFERRED — resolution also opens a return; refund fires at seller-confirm.",
                            dispute.Id);
                        break;
                    }
                    var shouldChargeInspectionFee = ShouldChargeBuyerWinInspectionFee(dispute, order);
                    var result = await _escrowSettlementService.RefundBuyerAsync(
                        order, null, "Dispute resolution: full refund to buyer", null, ct);
                    if (result.IsFailure)
                    {
                        _logger.LogWarning(
                            "Dispute {DisputeId}: escrow refund_buyer failed — {Error}",
                            dispute.Id, result.Error.Message);
                        return result.Error;
                    }
                    if (shouldChargeInspectionFee)
                        await ChargeBuyerWinInspectionFeeForResolvedCaseAsync(order, dispute, ct);
                    break;
                }

                case "partial_refund":
                {
                    if (skipRefund)
                    {
                        _logger.LogInformation(
                            "Dispute {DisputeId}: escrow partial_refund DEFERRED — resolution also opens a return; refund fires at seller-confirm.",
                            dispute.Id);
                        break;
                    }
                    if (actionSet.RefundAmount is null or <= 0)
                    {
                        _logger.LogWarning(
                            "Dispute {DisputeId}: escrow partial_refund skipped — no valid RefundAmount",
                            dispute.Id);
                        break;
                    }

                    var result = await _escrowSettlementService.RefundBuyerAsync(
                        order, actionSet.RefundAmount, "Dispute resolution: partial refund to buyer", null, ct);
                    if (result.IsFailure)
                    {
                        _logger.LogWarning(
                            "Dispute {DisputeId}: escrow partial_refund failed — {Error}",
                            dispute.Id, result.Error.Message);
                        return result.Error;
                    }
                    break;
                }

                case "hold":
                    _logger.LogInformation(
                        "Dispute {DisputeId}: escrow hold — no action needed (escrow remains held)",
                        dispute.Id);
                    break;

                default:
                    _logger.LogWarning(
                        "Dispute {DisputeId}: unknown escrow action '{Action}'",
                        dispute.Id, actionSet.EscrowAction);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Dispute {DisputeId}: escrow action '{Action}' threw an exception",
                dispute.Id, actionSet.EscrowAction);
            return Error.Unexpected(
                "DisputeResolution.EscrowActionFailed",
                $"Escrow action '{actionSet.EscrowAction}' failed unexpectedly.");
        }

        return UnitResult.Success<Error>();
    }

    private async Task<UnitResult<Error>> ApplyRefundActionAsync(
        DisputeResolutionActionSet actionSet,
        Order? order,
        Dispute dispute,
        bool skipRefund,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(actionSet.RefundAction)
            || actionSet.RefundAction == "no_refund"
            || actionSet.RefundAction == "no_action")
            return UnitResult.Success<Error>();

        if (order is null)
        {
            _logger.LogWarning(
                "Dispute {DisputeId}: refund action '{Action}' skipped — no linked order",
                dispute.Id, actionSet.RefundAction);
            return UnitResult.Success<Error>();
        }

        if (skipRefund)
        {
            _logger.LogInformation(
                "Dispute {DisputeId}: refund action '{Action}' DEFERRED — resolution also opens a return; refund fires at seller-confirm.",
                dispute.Id, actionSet.RefundAction);
            return UnitResult.Success<Error>();
        }

        try
        {
            switch (actionSet.RefundAction)
            {
                case "full_refund":
                {
                    var shouldChargeInspectionFee = ShouldChargeBuyerWinInspectionFee(dispute, order);
                    var result = await _escrowSettlementService.RefundBuyerAsync(
                        order, null, "Dispute resolution: full refund", null, ct);
                    if (result.IsFailure)
                    {
                        _logger.LogWarning(
                            "Dispute {DisputeId}: refund full_refund failed — {Error}",
                            dispute.Id, result.Error.Message);
                        return result.Error;
                    }
                    if (shouldChargeInspectionFee)
                        await ChargeBuyerWinInspectionFeeForResolvedCaseAsync(order, dispute, ct);
                    break;
                }

                case "partial_refund":
                {
                    if (actionSet.RefundAmount is null or <= 0)
                    {
                        _logger.LogWarning(
                            "Dispute {DisputeId}: refund partial_refund skipped — no valid RefundAmount",
                            dispute.Id);
                        break;
                    }

                    var result = await _escrowSettlementService.RefundBuyerAsync(
                        order, actionSet.RefundAmount, "Dispute resolution: partial refund", null, ct);
                    if (result.IsFailure)
                    {
                        _logger.LogWarning(
                            "Dispute {DisputeId}: refund partial_refund failed — {Error}",
                            dispute.Id, result.Error.Message);
                        return result.Error;
                    }
                    break;
                }

                default:
                    _logger.LogWarning(
                        "Dispute {DisputeId}: unknown refund action '{Action}'",
                        dispute.Id, actionSet.RefundAction);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Dispute {DisputeId}: refund action '{Action}' threw an exception",
                dispute.Id, actionSet.RefundAction);
            return Error.Unexpected(
                "DisputeResolution.RefundActionFailed",
                $"Refund action '{actionSet.RefundAction}' failed unexpectedly.");
        }

        return UnitResult.Success<Error>();
    }

    private static bool ShouldChargeBuyerWinInspectionFee(Dispute dispute, Order order)
    {
        var favorsBuyer = string.Equals(
            dispute.ResolutionOutcome,
            "favor_buyer",
            StringComparison.OrdinalIgnoreCase);

        var isPostDeliveryCase = order.Status == OrderStatus.Delivered
                                 || order.Status == OrderStatus.Disputed
                                 || order.Status == OrderStatus.Completed;

        return favorsBuyer && isPostDeliveryCase && order.IsPlatformVerifiedItem;
    }

    private async Task ChargeBuyerWinInspectionFeeForResolvedCaseAsync(
        Order order,
        Dispute dispute,
        CancellationToken ct)
    {
        var result = await _escrowSettlementService.ChargeVerifiedInspectionFeeForBuyerWinAsync(
            order,
            dispute.Id.Value,
            dispute.ResolutionReason ?? "Buyer-win dispute resolution",
            null,
            ct);

        if (result.IsFailure)
        {
            _logger.LogWarning(
                "Dispute {DisputeId}: buyer-win inspection fee charge failed - {Error}",
                dispute.Id,
                result.Error.Message);
        }
        else if (result.Value.Pending)
        {
            _logger.LogWarning(
                "Dispute {DisputeId}: buyer-win inspection fee charge is pending. Amount={Amount} Currency={Currency}",
                dispute.Id,
                result.Value.FeeAmount,
                result.Value.Currency);
        }
    }

    private async Task ChargeBuyerWinInspectionFeeAsync(
        Order order,
        Dispute dispute,
        CancellationToken ct)
    {
        var result = await _escrowSettlementService.ChargeVerifiedInspectionFeeForBuyerWinAsync(
            order,
            dispute.Id.Value,
            dispute.ResolutionReason ?? "Buyer-win dispute resolution",
            null,
            ct);

        if (result.IsFailure)
        {
            _logger.LogWarning(
                "Dispute {DisputeId}: buyer-win inspection fee charge failed â€” {Error}",
                dispute.Id,
                result.Error.Message);
        }
        else if (result.Value.Pending)
        {
            _logger.LogWarning(
                "Dispute {DisputeId}: buyer-win inspection fee charge is pending. Amount={Amount} Currency={Currency}",
                dispute.Id,
                result.Value.FeeAmount,
                result.Value.Currency);
        }
    }

    private async Task ApplyShipmentActionAsync(
        DisputeResolutionActionSet actionSet,
        Dispute dispute,
        DeferredRefundIntent deferredIntent,
        decimal? deferredAmount,
        DateTime now,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(actionSet.ShipmentAction) || actionSet.ShipmentAction == "no_action")
            return;

        try
        {
            switch (actionSet.ShipmentAction)
            {
                case "cancel_shipment":
                {
                    var shipment = await LoadShipmentAsync(dispute, ct);
                    if (shipment is null)
                    {
                        _logger.LogWarning(
                            "Dispute {DisputeId}: cancel_shipment skipped — shipment not found",
                            dispute.Id);
                        break;
                    }

                    var result = shipment.Cancel("Cancelled via dispute resolution", now);
                    if (result.IsFailure)
                        _logger.LogWarning(
                            "Dispute {DisputeId}: cancel_shipment failed — {Error}",
                            dispute.Id, result.Error.Message);
                    break;
                }

                case "mark_lost":
                {
                    var shipment = await LoadShipmentAsync(dispute, ct);
                    if (shipment is null)
                    {
                        _logger.LogWarning(
                            "Dispute {DisputeId}: mark_lost skipped — shipment not found",
                            dispute.Id);
                        break;
                    }

                    var result = shipment.RecordFailed("Marked lost via dispute resolution", now);
                    if (result.IsFailure)
                        _logger.LogWarning(
                            "Dispute {DisputeId}: mark_lost failed — {Error}",
                            dispute.Id, result.Error.Message);
                    break;
                }

                case "rebook_shipment":
                    // TODO: V1 stub — rebook requires complex carrier orchestration
                    _logger.LogWarning(
                        "Dispute {DisputeId}: rebook_shipment is a V1 stub — manual intervention required",
                        dispute.Id);
                    break;

                case "open_return":
                    await ApplyOpenReturnAsync(actionSet, dispute, deferredIntent, deferredAmount, now, ct);
                    break;

                default:
                    _logger.LogWarning(
                        "Dispute {DisputeId}: unknown shipment action '{Action}'",
                        dispute.Id, actionSet.ShipmentAction);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Dispute {DisputeId}: shipment action '{Action}' threw an exception",
                dispute.Id, actionSet.ShipmentAction);
        }
    }

    private async Task ApplyItemActionAsync(
        DisputeResolutionActionSet actionSet,
        Dispute dispute,
        DateTime now,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(actionSet.ItemAction) || actionSet.ItemAction == "no_action")
            return;

        try
        {
            switch (actionSet.ItemAction)
            {
                case "return_to_active":
                {
                    var item = await LoadItemFromDisputeAsync(dispute, ct);
                    if (item is null)
                    {
                        _logger.LogWarning(
                            "Dispute {DisputeId}: return_to_active skipped — item not found",
                            dispute.Id);
                        break;
                    }

                    var result = item.ReturnToActive(now);
                    if (result.IsFailure)
                        _logger.LogWarning(
                            "Dispute {DisputeId}: return_to_active failed — {Error}",
                            dispute.Id, result.Error.Message);
                    break;
                }

                case "hold_in_warehouse":
                    _logger.LogInformation(
                        "Dispute {DisputeId}: hold_in_warehouse — no action needed (item remains in current warehouse status)",
                        dispute.Id);
                    break;

                case "return_to_seller":
                    // return_to_seller is now handled automatically by
                    // CreateWarehouseToSellerShipmentOnRejectionHandler on
                    // WarehouseInspectionRejectedEvent — no action needed in
                    // ApplyItemActionAsync. Kept as an explicit case so the
                    // "unknown item action" warning doesn't fire. V2 follow-up:
                    // add a dispute-level explicit return-to-seller for rare
                    // scenarios where the inspector has NOT rejected the item
                    // but the moderator decides it should go back anyway.
                    _logger.LogInformation(
                        "Dispute {DisputeId}: return_to_seller is a no-op at dispute level — warehouse-inspection-reject handler covers this scenario.",
                        dispute.Id);
                    break;

                case "reject_listing":
                    // TODO: V1 stub — reject_listing requires moderation workflow
                    _logger.LogWarning(
                        "Dispute {DisputeId}: reject_listing is a V1 stub — manual intervention required",
                        dispute.Id);
                    break;

                default:
                    _logger.LogWarning(
                        "Dispute {DisputeId}: unknown item action '{Action}'",
                        dispute.Id, actionSet.ItemAction);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Dispute {DisputeId}: item action '{Action}' threw an exception",
                dispute.Id, actionSet.ItemAction);
        }
    }

    private async Task ApplyAuctionActionAsync(
        DisputeResolutionActionSet actionSet,
        Dispute dispute,
        DateTime now,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(actionSet.AuctionAction) || actionSet.AuctionAction == "no_action")
            return;

        try
        {
            switch (actionSet.AuctionAction)
            {
                case "relist":
                {
                    // Return the item to active so it's available for a new auction.
                    // The actual re-auction creation is manual (seller does it).
                    var item = await LoadItemFromDisputeAsync(dispute, ct);
                    if (item is null)
                    {
                        _logger.LogWarning(
                            "Dispute {DisputeId}: relist skipped — item not found",
                            dispute.Id);
                        break;
                    }

                    var result = item.ReturnToActive(now);
                    if (result.IsFailure)
                        _logger.LogWarning(
                            "Dispute {DisputeId}: relist (return_to_active) failed — {Error}",
                            dispute.Id, result.Error.Message);
                    else
                        _logger.LogInformation(
                            "Dispute {DisputeId}: item returned to active for relisting",
                            dispute.Id);
                    break;
                }

                case "cancel":
                {
                    var auction = await LoadAuctionAsync(dispute, ct);
                    if (auction is null)
                    {
                        _logger.LogWarning(
                            "Dispute {DisputeId}: auction cancel skipped — auction not found",
                            dispute.Id);
                        break;
                    }

                    var result = auction.CancelAuction("Cancelled via dispute resolution", now);
                    if (result.IsFailure)
                        _logger.LogWarning(
                            "Dispute {DisputeId}: auction cancel failed — {Error}",
                            dispute.Id, result.Error.Message);
                    break;
                }

                default:
                    _logger.LogWarning(
                        "Dispute {DisputeId}: unknown auction action '{Action}'",
                        dispute.Id, actionSet.AuctionAction);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Dispute {DisputeId}: auction action '{Action}' threw an exception",
                dispute.Id, actionSet.AuctionAction);
        }
    }

    private void ApplyPenaltyAction(DisputeResolutionActionSet actionSet, Dispute dispute)
    {
        if (string.IsNullOrWhiteSpace(actionSet.PenaltyAction)
            || actionSet.PenaltyAction == "no_penalty"
            || actionSet.PenaltyAction == "no_action")
            return;

        // V1: log an audit-level warning flagging the user for manual review.
        // Automated bans or restrictions are not yet implemented.
        switch (actionSet.PenaltyAction)
        {
            case "flag_buyer":
                _logger.LogWarning(
                    "Dispute {DisputeId}: PENALTY — buyer {BuyerId} flagged for review",
                    dispute.Id, dispute.ComplainantId);
                break;

            case "flag_seller":
                _logger.LogWarning(
                    "Dispute {DisputeId}: PENALTY — seller {SellerId} flagged for review",
                    dispute.Id, dispute.RespondentId);
                break;

            default:
                _logger.LogWarning(
                    "Dispute {DisputeId}: unknown penalty action '{Action}'",
                    dispute.Id, actionSet.PenaltyAction);
                break;
        }
    }

    // ── Helper methods to load related entities ──

    private async Task<OutboundShipment?> LoadShipmentAsync(Dispute dispute, CancellationToken ct)
    {
        if (dispute.ShipmentId.HasValue)
        {
            return await _dbContext.GetByIdAsync<OutboundShipment, OutboundShipmentId>(
                OutboundShipmentId.From(dispute.ShipmentId.Value), cancellationToken: ct);
        }

        // Fallback: try to find shipment via the order
        if (dispute.OrderId.Value != Guid.Empty)
        {
            return await _dbContext.Set<OutboundShipment>()
                .FirstOrDefaultAsync(s => s.OrderId == dispute.OrderId, ct);
        }

        return null;
    }

    private async Task<Item?> LoadItemFromDisputeAsync(Dispute dispute, CancellationToken ct)
    {
        // Try to derive the item from the auction link
        if (dispute.AuctionId.HasValue)
        {
            var auction = await _dbContext.GetByIdAsync<Auction, AuctionId>(
                dispute.AuctionId.Value, cancellationToken: ct);
            if (auction is not null)
            {
                return await _dbContext.GetByIdAsync<Item, ItemId>(
                    auction.ItemId, cancellationToken: ct);
            }
        }

        // Fallback: try CaseAuctionId
        if (dispute.CaseAuctionId.HasValue)
        {
            var auction = await _dbContext.GetByIdAsync<Auction, AuctionId>(
                AuctionId.From(dispute.CaseAuctionId.Value), cancellationToken: ct);
            if (auction is not null)
            {
                return await _dbContext.GetByIdAsync<Item, ItemId>(
                    auction.ItemId, cancellationToken: ct);
            }
        }

        return null;
    }

    private async Task ApplyOpenReturnAsync(
        DisputeResolutionActionSet actionSet,
        Dispute dispute,
        DeferredRefundIntent deferredRefundIntent,
        decimal? deferredRefundAmount,
        DateTime now,
        CancellationToken ct)
    {
        if (dispute.OrderId.Value == Guid.Empty)
        {
            _logger.LogWarning(
                "Dispute {DisputeId}: open_return skipped — dispute has no linked OrderId.",
                dispute.Id);
            return;
        }

        // Load Order with its 1:1 Return nav so the aggregate's ReturnAlreadyExists
        // guard sees an existing non-terminal row. GetByIdAsync exposes a query-
        // builder hook for includes.
        var order = await _dbContext.GetByIdAsync<Order, OrderId>(
            dispute.OrderId,
            queryBuilder: q => q.Include(o => o.Return),
            cancellationToken: ct);

        if (order is null)
        {
            _logger.LogWarning(
                "Dispute {DisputeId}: open_return skipped — Order {OrderId} not found.",
                dispute.Id, dispute.OrderId);
            return;
        }

        // Fee-payer resolution: prefer explicit moderator choice, fall back to Buyer.
        var feePayer = ShippingFeePayer.Buyer;
        if (!string.IsNullOrWhiteSpace(actionSet.ReturnShippingFeePayer))
        {
            var parsed = ShippingFeePayer.FromId(actionSet.ReturnShippingFeePayer);
            if (parsed.HasValue)
            {
                feePayer = parsed.Value;
            }
            else
            {
                _logger.LogWarning(
                    "Dispute {DisputeId}: open_return — unknown ReturnShippingFeePayer '{Value}'. Defaulting to Buyer.",
                    dispute.Id, actionSet.ReturnShippingFeePayer);
            }
        }

        var dueDays = actionSet.BuyerDecisionDueDays is > 0
            ? actionSet.BuyerDecisionDueDays.Value
            : 7;

        var openResult = order.OpenReturnViaDispute(
            reasonCode: "dispute_resolution",
            description: "Opened via dispute resolution",
            feePayer: feePayer,
            deferredRefundIntent: deferredRefundIntent,
            deferredRefundAmount: deferredRefundAmount,
            buyerDecisionDueAt: now.AddDays(dueDays),
            nowUtc: now);

        if (openResult.IsFailure)
        {
            if (openResult.Error.Code == "Order.ReturnAlreadyExists")
            {
                // Idempotent re-apply — a prior apply already opened the return.
                _logger.LogInformation(
                    "Dispute {DisputeId}: open_return skipped — Order {OrderId} already has an active OrderReturn.",
                    dispute.Id, dispute.OrderId);
            }
            else
            {
                _logger.LogWarning(
                    "Dispute {DisputeId}: open_return failed for Order {OrderId} — {Error}",
                    dispute.Id, dispute.OrderId, openResult.Error.Message);
            }
            return;
        }

        // Mint a signed return-scoped QR token and stamp it onto the freshly
        // opened OrderReturn so the buyer sees the shipping label IMMEDIATELY
        // when the return is approved — before they book a courier or hand
        // the parcel off. Bound to the real OrderReturnId (aggregate-minted).
        var orderReturn = openResult.Value;
        var qrToken = _qrTokenService.Issue(
            kind:              "order_return",
            shipmentOrReturnId: orderReturn.Id.Value,
            issuedAt:          now,
            expiresAt:         now.AddDays(30));
        var qrResult = orderReturn.IssueReturnQr(qrToken, now);
        if (qrResult.IsFailure)
        {
            _logger.LogWarning(
                "Dispute {DisputeId}: open_return — QR token stamp failed for OrderReturn {OrderReturnId}: {Error}. Return remains usable; QR will be re-issued on MarkReturnShipped.",
                dispute.Id, orderReturn.Id.Value, qrResult.Error.Message);
        }

        // Aggregate owns the lifecycle — update Order; the cascade persists the
        // new OrderReturn. Do NOT call _dbContext.Insert(orderReturn). The
        // OrderReturnOpenedByDisputeEvent is raised inside OpenReturnViaDispute.
        _dbContext.Update(order);

        _logger.LogInformation(
            "Dispute {DisputeId}: open_return applied — OrderReturn {OrderReturnId} pre-approved for Order {OrderId}. FeePayer={FeePayer}, DueAt={DueAt:o}",
            dispute.Id, openResult.Value.Id.Value, dispute.OrderId, feePayer.Id, now.AddDays(dueDays));
    }

    private async Task<Auction?> LoadAuctionAsync(Dispute dispute, CancellationToken ct)
    {
        if (dispute.AuctionId.HasValue)
        {
            return await _dbContext.GetByIdAsync<Auction, AuctionId>(
                dispute.AuctionId.Value, cancellationToken: ct);
        }

        if (dispute.CaseAuctionId.HasValue)
        {
            return await _dbContext.GetByIdAsync<Auction, AuctionId>(
                AuctionId.From(dispute.CaseAuctionId.Value), cancellationToken: ct);
        }

        return null;
    }

    // ── Deferred-refund helpers (B2) ──

    private static bool IsOpenReturn(DisputeResolutionActionSet actionSet) =>
        string.Equals(actionSet.ShipmentAction, "open_return", StringComparison.Ordinal);

    /// <summary>
    /// Computes the <see cref="DeferredRefundIntent"/> from the moderator's ActionSet.
    /// D8 — OR-composition: when both <c>EscrowAction</c> and <c>RefundAction</c> carry
    /// a refund verb simultaneously, the stronger intent wins (Full over Partial) and
    /// only one intent is recorded; both skip-refund call sites suppress their
    /// <c>RefundBuyerAsync</c> calls so the refund fires exactly once at seller-confirm.
    /// </summary>
    internal static (DeferredRefundIntent Intent, decimal? Amount) ComputeDeferredRefundIntent(
        DisputeResolutionActionSet actionSet)
    {
        // Intent only applies when the resolution opens a return; otherwise refund
        // fires immediately via the existing path and no deferral is recorded.
        if (!IsOpenReturn(actionSet))
            return (DeferredRefundIntent.None, null);

        var escrowIsFullRefund    = actionSet.EscrowAction == "refund_buyer";
        var refundIsFullRefund    = actionSet.RefundAction == "full_refund";
        var escrowIsPartialRefund = actionSet.EscrowAction == "partial_refund";
        var refundIsPartialRefund = actionSet.RefundAction == "partial_refund";

        // Full wins over Partial if both are present (OR-composition).
        if (escrowIsFullRefund || refundIsFullRefund)
            return (DeferredRefundIntent.Full, null);

        if (escrowIsPartialRefund || refundIsPartialRefund)
        {
            // Partial requires a positive amount — fall back to None if missing.
            if (actionSet.RefundAmount is null or <= 0)
                return (DeferredRefundIntent.None, null);
            return (DeferredRefundIntent.Partial, actionSet.RefundAmount);
        }

        // open_return without a refund verb — pure return, no refund deferred.
        return (DeferredRefundIntent.None, null);
    }
}
