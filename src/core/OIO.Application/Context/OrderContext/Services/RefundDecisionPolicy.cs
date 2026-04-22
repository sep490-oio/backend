using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Enums;

namespace OIO.Application.Context.OrderContext.Services;

/// <summary>
/// Single decision site for refund timing at <see cref="OrderReturn"/> resolve.
/// Principle 1 rewrite — all refund-firing logic lives in one class so the
/// admin UI / command handler / auto-confirm job all agree on what to fire.
/// </summary>
/// <remarks>
/// Non-dispute historic path (<see cref="OrderReturn.DeferredRefundIntent"/> is null
/// for returns opened via <c>Order.RequestReturn</c>, not <c>OpenReturnViaDispute</c>):
/// fire FULL refund as today. Dispute path defers to the moderator's intent.
/// </remarks>
public static class RefundDecisionPolicy
{
    public static RefundDecision DecideFor(OrderReturn orderReturn)
    {
        // Non-dispute path: historic behavior — always full refund on seller-confirm.
        if (orderReturn.DeferredRefundIntent is null)
            return new RefundDecision.FireFull();

        if (orderReturn.DeferredRefundIntent == DeferredRefundIntent.Full)
            return new RefundDecision.FireFull();

        if (orderReturn.DeferredRefundIntent == DeferredRefundIntent.Partial)
        {
            var amount = orderReturn.DeferredRefundAmount
                ?? throw new InvalidOperationException(
                    $"Partial deferred refund on OrderReturn {orderReturn.Id.Value} has no amount.");
            return new RefundDecision.FirePartial(amount);
        }

        // DeferredRefundIntent.None — no refund (open_return without refund clause).
        return new RefundDecision.Skip();
    }
}

/// <summary>Discriminated union describing the refund action to dispatch at seller-confirm.</summary>
public abstract record RefundDecision
{
    public sealed record FireFull : RefundDecision;
    public sealed record FirePartial(decimal Amount) : RefundDecision;
    public sealed record Skip : RefundDecision;

    private RefundDecision() { }
}
