using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.OrderContext.Services;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Enums;
using OIO.Domain.Context.OrderContext.Errors;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.OrderContext.Commands.RetryDeferredRefund;

/// <summary>
/// Admin-initiated retry of a deferred refund that previously failed (D1
/// compensating-path follow-up). Verifies the return is <see cref="OrderReturnStatus.Resolved"/>
/// with a non-None intent, re-runs <see cref="RefundDecisionPolicy.DecideFor"/>, and
/// fires <c>RefundBuyerAsync</c> again. Admin ticket (structured log or table row)
/// closes implicitly on success — caller is expected to mark the ticket resolved.
/// </summary>
public sealed record RetryDeferredRefundCommand(
    Guid OrderId,
    Guid OrderReturnId) : ICommand, IHasValidate
{
    public ViolationsError Validate() =>
        RetryDeferredRefundCommand.Check()
            .WithOwnerName("RetryDeferredRefund")
            .Field(OrderId).NotEmptyGuid()
            .Field(OrderReturnId).NotEmptyGuid();
}

internal sealed class RetryDeferredRefundCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    EscrowSettlementService settlementService,
    ILogger<RetryDeferredRefundCommandHandler> logger)
    : ICommandHandler<RetryDeferredRefundCommand>
{
    public async Task<UnitResult<Error>> Handle(
        RetryDeferredRefundCommand request,
        CancellationToken cancellationToken)
    {
        var order = await dbContext.Set<Order>()
            .Include(x => x.Return)
            .Include(x => x.Escrows)
            .FirstOrDefaultAsync(x => x.Id == OrderId.From(request.OrderId), cancellationToken);

        if (order is null)
            return OrderErrors.Order.NotFound(OrderId.From(request.OrderId));

        if (order.Return is null || order.Return.Id != OrderReturnId.From(request.OrderReturnId))
            return OrderErrors.Order.ReturnNotFound(order.Id);

        // Guards — only retry a resolved return with a non-None deferred intent.
        if (order.Return.Status != OrderReturnStatus.Resolved)
            return Error.Conflict(
                "OrderReturn.InvalidState",
                $"Retry requires Resolved status, but return is '{order.Return.Status.Id}'.");

        if (order.Return.DeferredRefundIntent is null
            || order.Return.DeferredRefundIntent == DeferredRefundIntent.None)
            return Error.Conflict(
                "OrderReturn.NoDeferredRefund",
                "Return has no deferred-refund intent to retry.");

        var decision = RefundDecisionPolicy.DecideFor(order.Return);

        Result<RefundSettlementResult, Error> refundResult;
        switch (decision)
        {
            case RefundDecision.FireFull:
                refundResult = await settlementService.RefundBuyerAsync(
                    order,
                    partialAmount: null,
                    reason: "Deferred refund retry (full) — admin-initiated",
                    actorId: null,
                    cancellationToken: cancellationToken);
                break;

            case RefundDecision.FirePartial partial:
                refundResult = await settlementService.RefundBuyerAsync(
                    order,
                    partialAmount: partial.Amount,
                    reason: "Deferred refund retry (partial) — admin-initiated",
                    actorId: null,
                    cancellationToken: cancellationToken);
                break;

            case RefundDecision.Skip:
                // Should not happen — None was guarded above.
                return Error.Conflict(
                    "OrderReturn.NoDeferredRefund",
                    "RefundDecisionPolicy returned Skip unexpectedly.");

            default:
                return Error.Conflict(
                    "OrderReturn.UnknownDecision",
                    $"Unknown RefundDecision variant '{decision.GetType().Name}'.");
        }

        if (refundResult.IsFailure)
        {
            logger.LogError(
                "RetryDeferredRefund FAILED again: OrderId={OrderId} OrderReturnId={OrderReturnId} Error={Error}",
                order.Id.Value, order.Return.Id.Value, refundResult.Error.Message);
            return refundResult.Error;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "RetryDeferredRefund succeeded: OrderId={OrderId} OrderReturnId={OrderReturnId} Decision={Decision}",
            order.Id.Value, order.Return.Id.Value, decision.GetType().Name);

        return UnitResult.Success<Error>();
    }
}
