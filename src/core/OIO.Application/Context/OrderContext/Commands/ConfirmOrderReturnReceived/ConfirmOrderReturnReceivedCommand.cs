using System.Text.Json;
using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.NotificationContext;
using OIO.Application.Context.NotificationContext.Commands.CreateNotification;
using OIO.Application.Context.OrderContext.Commands.CreateAdminRefundRetryTicket;
using OIO.Application.Context.OrderContext.DTOs;
using OIO.Application.Context.OrderContext.Mappings;
using OIO.Application.Context.OrderContext.Services;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.CatalogContext.Enums;
using OIO.Domain.Context.CatalogContext.ValueObjects.Ids;
using OIO.Domain.Context.NotificationContext.Enums;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Enums;
using OIO.Domain.Context.OrderContext.Errors;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.OrderContext.Commands.ConfirmOrderReturnReceived;

public sealed record ConfirmOrderReturnReceivedCommand(
    Guid OrderId,
    Guid ReturnId) : ICommand<OrderReturnDto>, IHasValidate
{
    public ViolationsError Validate() =>
        ConfirmOrderReturnReceivedCommand.Check()
            .WithOwnerName("ConfirmOrderReturnReceived")
            .Field(OrderId).NotEmptyGuid()
            .Field(ReturnId).NotEmptyGuid();
}

internal sealed class ConfirmOrderReturnReceivedCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock,
    EscrowSettlementService settlementService,
    ISender sender,
    ILogger<ConfirmOrderReturnReceivedCommandHandler> logger)
    : ICommandHandler<ConfirmOrderReturnReceivedCommand, OrderReturnDto>
{
    public async Task<Result<OrderReturnDto, Error>> Handle(
        ConfirmOrderReturnReceivedCommand request,
        CancellationToken cancellationToken)
    {
        var order = await dbContext.Set<Order>()
            .Include(x => x.Return)
                .ThenInclude(r => r!.Evidence)
            .Include(x => x.Escrows)
            .FirstOrDefaultAsync(x => x.Id == OrderId.From(request.OrderId), cancellationToken);

        if (order is null)
            return OrderErrors.Order.NotFound(OrderId.From(request.OrderId));

        if (order.SellerId != currentUser.UserId)
            return Error.Forbidden("Order.ReturnForbidden", "Only the seller can confirm returned goods.");

        if (order.Return is null || order.Return.Id != OrderReturnId.From(request.ReturnId))
            return OrderErrors.Order.ReturnNotFound(order.Id);

        // D3: MarkSellerReceived does NOT require evidence — scan is identity-proof only.
        // Idempotent: if the seller already scanned the QR (status == SellerReceived) or
        // has already resolved the return, skip the transition and proceed to evidence+refund.
        // Only fire MarkSellerReceived when status is still Approved or ReturnInTransit.
        if (order.Return.Status == OrderReturnStatus.Approved
            || order.Return.Status == OrderReturnStatus.ReturnInTransit)
        {
            var receiveResult = order.Return.MarkSellerReceived(DateTime.UtcNow);
            if (receiveResult.IsFailure)
                return receiveResult.Error;
        }

        // D3: Resolve carries the HasReceiptEvidence guard. Moved UP so the terminal
        // status commit acts as the concurrency lock: a concurrent caller finds
        // Status == Resolved and short-circuits with Success (idempotent skip).
        var resolveResult = order.Return.Resolve(DateTime.UtcNow);
        if (resolveResult.IsFailure)
            return resolveResult.Error;

        // Commit the state transition BEFORE firing the refund so concurrent callers
        // (seller click + auto-confirm-job race) reload the aggregate, find it
        // already Resolved, and the second Resolve() returns InvalidState -> success.
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Concurrent caller beat us to Resolved — idempotent skip.
            logger.LogInformation(
                "ConfirmOrderReturnReceived: short-circuit — OrderReturn {OrderReturnId} already Resolved. Idempotent skip.",
                order.Return.Id.Value);
            return order.Return.ToDto();
        }

        // D6: dispatch refund via RefundDecisionPolicy — single decision site.
        var decision = RefundDecisionPolicy.DecideFor(order.Return);

        Result<RefundSettlementResult, Error> refundResult = default;
        var refundFired = false;

        switch (decision)
        {
            case RefundDecision.FireFull:
                refundResult = await settlementService.RefundBuyerAsync(
                    order, null, "Deferred refund at return-received (full)", currentUser.UserId, cancellationToken);
                refundFired = true;
                break;

            case RefundDecision.FirePartial partial:
                refundResult = await settlementService.RefundBuyerAsync(
                    order, partial.Amount, "Deferred refund at return-received (partial)", currentUser.UserId, cancellationToken);
                refundFired = true;
                break;
        }

        if (refundFired && refundResult.IsFailure)
        {
            // D1 compensating path — V1 scope. Resolve() already committed, so we do
            // NOT propagate the error to the caller; instead log + raise event +
            // create admin ticket so the stuck refund surfaces for manual retry.
            await EmitDeferredRefundFailedAsync(order, decision, refundResult.Error, cancellationToken);
        }

        // Charge seller commission when the deferred refund succeeds.
        // DisputeResolutionService skips ChargeSellerCommissionOnBuyerRefundAsync
        // when it defers the refund via open_return (the `break` inside the
        // refund_buyer / full_refund case exits before the commission call).
        // Fire it here so the seller's platform fee is always collected.
        if (refundFired && refundResult.IsSuccess
            && order.Return.DeferredRefundIntent is not null
            && order.Return.DeferredRefundIntent != DeferredRefundIntent.None)
        {
            var commissionResult = await settlementService.ChargeSellerCommissionOnBuyerRefundAsync(
                order,
                disputeId: order.Return.Id.Value,  // use return ID as reference
                reason: "Platform commission for buyer-win dispute return",
                cancellationToken);

            if (commissionResult.IsFailure)
            {
                logger.LogWarning(
                    "ConfirmOrderReturnReceived: seller commission charge failed for Order {OrderId}: {Error}. " +
                    "Refund already committed — commission will need manual collection.",
                    order.Id.Value, commissionResult.Error.Message);
            }
            else if (commissionResult.Value.Pending)
            {
                logger.LogWarning(
                    "ConfirmOrderReturnReceived: seller commission pending (insufficient funds) for Order {OrderId}. " +
                    "Amount={Amount} Currency={Currency}",
                    order.Id.Value, commissionResult.Value.FeeAmount, commissionResult.Value.Currency);
            }
            else if (commissionResult.Value.Collected)
            {
                logger.LogInformation(
                    "ConfirmOrderReturnReceived: seller commission charged for Order {OrderId}. " +
                    "Amount={Amount} Currency={Currency}",
                    order.Id.Value, commissionResult.Value.FeeAmount, commissionResult.Value.Currency);
            }
        }

        // Save the refund transactions (or admin-ticket side effects) now.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Bug 4 — flip the Item status back to Active so the marketplace stops
        // showing it as "Sold". Best-effort: the return is the primary action,
        // the item flip is secondary. Any failure here is logged but does NOT
        // propagate to the caller.
        await TryReturnItemToActiveAsync(order, cancellationToken);

        await NotificationDispatch.DispatchAsync(
            sender,
            logger,
            new CreateNotificationCommand(
                UserId: order.BuyerId.Value,
                NotificationType: "order",
                EventType: "refund_completed",
                Title: "Hoan tien da hoan tat",
                Message: $"Khoan tien giu cho don {order.OrderNumber.Value} da duoc hoan cho ban.",
                Priority: NotificationPriority.High,
                EntityType: "Order",
                EntityId: order.Id.Value),
            cancellationToken);

        return order.Return.ToDto();
    }

    /// <summary>
    /// Bug 4 — secondary item-state flip. After the return is Resolved and the
    /// refund has fired, re-activate the underlying Item so the marketplace
    /// stops showing it as "Sold". Runs as a best-effort side-effect so any
    /// failure (wrong state, item already re-listed on a new auction) does not
    /// roll back the primary return transition. Fires for both manual
    /// seller-click and the OrderReturnAutoConfirmJob (handler-level).
    /// </summary>
    /// <remarks>
    /// Guard: only flip when the item is currently <see cref="ItemStatus.Sold"/>.
    /// If the seller has already re-listed on a new auction (InAuction, Active,
    /// Approved, etc.), the flip is skipped — we must not clobber an active
    /// listing. Concurrent returns race note: two parallel confirm calls would
    /// both hit this path, but <see cref="Item.ReturnToActive"/> is idempotent
    /// (no-op when already Active/Approved) so the second call is harmless.
    /// </remarks>
    private async Task TryReturnItemToActiveAsync(Order order, CancellationToken ct)
    {
        try
        {
            var auction = await dbContext.GetByIdAsync<Auction, AuctionId>(
                order.AuctionId, cancellationToken: ct);
            if (auction is null)
            {
                logger.LogInformation(
                    "ConfirmOrderReturnReceived: item flip skipped — auction {AuctionId} not found for Order {OrderId}.",
                    order.AuctionId.Value, order.Id.Value);
                return;
            }

            var item = await dbContext.GetByIdAsync<Item, ItemId>(
                auction.ItemId, cancellationToken: ct);
            if (item is null)
            {
                logger.LogInformation(
                    "ConfirmOrderReturnReceived: item flip skipped — item {ItemId} not found for Auction {AuctionId}.",
                    auction.ItemId.Value, auction.Id.Value);
                return;
            }

            // Guard: only flip when the item is still "locked after fulfillment".
            // Sold is the canonical post-sale state; we deliberately skip InAuction
            // / Active / Approved / Removed etc. so we don't clobber a re-listing.
            if (item.Status != ItemStatus.Sold)
            {
                logger.LogInformation(
                    "ConfirmOrderReturnReceived: item flip skipped — Item {ItemId} status is '{Status}', not 'sold'. Leaving as-is.",
                    item.Id.Value, item.Status.Id);
                return;
            }

            var flipResult = item.ReturnToActive(clock.UtcNow);
            if (flipResult.IsFailure)
            {
                logger.LogWarning(
                    "ConfirmOrderReturnReceived: Item.ReturnToActive failed for Item {ItemId}: {Error}. Return remains Resolved; item status unchanged.",
                    item.Id.Value, flipResult.Error.Message);
                return;
            }

            dbContext.Update(item);
            await unitOfWork.SaveChangesAsync(ct);

            logger.LogInformation(
                "ConfirmOrderReturnReceived: Item {ItemId} flipped back to Active after return resolution for Order {OrderId}.",
                item.Id.Value, order.Id.Value);
        }
        catch (Exception ex)
        {
            // Secondary side-effect — never propagate.
            logger.LogWarning(ex,
                "ConfirmOrderReturnReceived: TryReturnItemToActive threw for Order {OrderId}. Return remains Resolved.",
                order.Id.Value);
        }
    }

    /// <summary>
    /// D1 compensating path. Emits the structured error log, raises the
    /// <see cref="Domain.Context.OrderContext.Aggregates.Orders.Events.DeferredRefundFailedEvent"/>
    /// on the Order aggregate (OrderReturn is BaseEntity), and creates an admin
    /// retry ticket via <see cref="CreateAdminRefundRetryTicketCommand"/>.
    /// </summary>
    private async Task EmitDeferredRefundFailedAsync(
        Order order,
        RefundDecision decision,
        Error error,
        CancellationToken ct)
    {
        var orderReturn = order.Return!;

        logger.LogError(
            "ConfirmOrderReturnReceived: RefundBuyerAsync FAILED after Resolve() committed. " +
            "OrderReturnId={OrderReturnId}, OrderId={OrderId}, Intent={Intent}, Amount={Amount}, Error={Error}. " +
            "Creating admin retry ticket.",
            orderReturn.Id.Value, order.Id.Value,
            orderReturn.DeferredRefundIntent?.Id, orderReturn.DeferredRefundAmount,
            error.Message);

        var intentJson = JsonSerializer.Serialize(new
        {
            intent = orderReturn.DeferredRefundIntent?.Id,
            amount = orderReturn.DeferredRefundAmount,
            decision = decision.GetType().Name
        });

        order.RaiseDeferredRefundFailed(
            orderReturn:  orderReturn,
            intentJson:   intentJson,
            errorMessage: error.Message,
            nowUtc:       DateTime.UtcNow);

        await sender.Send(new CreateAdminRefundRetryTicketCommand(
            OrderId:       order.Id.Value,
            OrderReturnId: orderReturn.Id.Value,
            Intent:        orderReturn.DeferredRefundIntent?.Id ?? "unknown",
            Amount:        orderReturn.DeferredRefundAmount,
            FailureReason: error.Message), ct);
    }
}
