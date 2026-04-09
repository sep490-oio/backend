using System.Text.Json;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.CatalogContext.ValueObjects.Ids;
using OIO.Domain.Context.ModerationContext.Aggregates.Disputes;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
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
}

internal sealed class DisputeResolutionService : IDisputeResolutionService
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly EscrowSettlementService _escrowSettlementService;
    private readonly ILogger<DisputeResolutionService> _logger;

    public DisputeResolutionService(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock,
        EscrowSettlementService escrowSettlementService,
        ILogger<DisputeResolutionService> logger)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _escrowSettlementService = escrowSettlementService;
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

        // 1. Escrow action
        await ApplyEscrowActionAsync(actionSet, order, dispute, ct);

        // 2. Refund action
        await ApplyRefundActionAsync(actionSet, order, dispute, ct);

        // 3. Shipment action
        await ApplyShipmentActionAsync(actionSet, dispute, now, ct);

        // 4. Item action
        await ApplyItemActionAsync(actionSet, dispute, now, ct);

        // 5. Auction action
        await ApplyAuctionActionAsync(actionSet, dispute, now, ct);

        // 6. Penalty action (V1: flag only, no automated enforcement)
        ApplyPenaltyAction(actionSet, dispute);

        await _unitOfWork.SaveChangesAsync(ct);
        return UnitResult.Success<Error>();
    }

    private async Task ApplyEscrowActionAsync(
        DisputeResolutionActionSet actionSet,
        Order? order,
        Dispute dispute,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(actionSet.EscrowAction) || actionSet.EscrowAction == "no_action")
            return;

        if (order is null)
        {
            _logger.LogWarning(
                "Dispute {DisputeId}: escrow action '{Action}' skipped — no linked order",
                dispute.Id, actionSet.EscrowAction);
            return;
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
                        _logger.LogWarning(
                            "Dispute {DisputeId}: escrow release_to_seller failed — {Error}",
                            dispute.Id, result.Error.Message);
                    break;
                }

                case "refund_buyer":
                {
                    var result = await _escrowSettlementService.RefundBuyerAsync(
                        order, null, "Dispute resolution: full refund to buyer", null, ct);
                    if (result.IsFailure)
                        _logger.LogWarning(
                            "Dispute {DisputeId}: escrow refund_buyer failed — {Error}",
                            dispute.Id, result.Error.Message);
                    break;
                }

                case "partial_refund":
                {
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
                        _logger.LogWarning(
                            "Dispute {DisputeId}: escrow partial_refund failed — {Error}",
                            dispute.Id, result.Error.Message);
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
        }
    }

    private async Task ApplyRefundActionAsync(
        DisputeResolutionActionSet actionSet,
        Order? order,
        Dispute dispute,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(actionSet.RefundAction)
            || actionSet.RefundAction == "no_refund"
            || actionSet.RefundAction == "no_action")
            return;

        if (order is null)
        {
            _logger.LogWarning(
                "Dispute {DisputeId}: refund action '{Action}' skipped — no linked order",
                dispute.Id, actionSet.RefundAction);
            return;
        }

        try
        {
            switch (actionSet.RefundAction)
            {
                case "full_refund":
                {
                    var result = await _escrowSettlementService.RefundBuyerAsync(
                        order, null, "Dispute resolution: full refund", null, ct);
                    if (result.IsFailure)
                        _logger.LogWarning(
                            "Dispute {DisputeId}: refund full_refund failed — {Error}",
                            dispute.Id, result.Error.Message);
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
                        _logger.LogWarning(
                            "Dispute {DisputeId}: refund partial_refund failed — {Error}",
                            dispute.Id, result.Error.Message);
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
        }
    }

    private async Task ApplyShipmentActionAsync(
        DisputeResolutionActionSet actionSet,
        Dispute dispute,
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
                    // TODO: V1 stub — open_return requires return shipment orchestration
                    _logger.LogWarning(
                        "Dispute {DisputeId}: open_return is a V1 stub — manual intervention required",
                        dispute.Id);
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
                    // TODO: V1 stub — full item-return workflow is Phase 5+ territory
                    _logger.LogWarning(
                        "Dispute {DisputeId}: return_to_seller is a V1 stub — manual intervention required",
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
}
