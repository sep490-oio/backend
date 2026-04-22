using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.OrderContext.Enums;

/// <summary>
/// Captures the moderator-decided refund intent at dispute-resolve time when the
/// resolution also opens a buyer ship-back return. The actual refund is deferred
/// until <c>ConfirmOrderReturnReceivedCommandHandler</c> fires it after the
/// seller receives the goods back. Populated by <see cref="Aggregates.Orders.Order.OpenReturnViaDispute"/>
/// from the moderator's ActionSet.
/// </summary>
public sealed class DeferredRefundIntent : EnumValueObject<DeferredRefundIntent>
{
    /// <summary>Full refund to buyer when the return is resolved.</summary>
    public static readonly DeferredRefundIntent Full    = new("full");

    /// <summary>Partial refund to buyer (amount stored on <c>OrderReturn.DeferredRefundAmount</c>).</summary>
    public static readonly DeferredRefundIntent Partial = new("partial");

    /// <summary>No deferred refund (e.g. open_return without refund clause, or non-return resolution).</summary>
    public static readonly DeferredRefundIntent None    = new("none");

    private DeferredRefundIntent(string id) : base(id) { }
}
