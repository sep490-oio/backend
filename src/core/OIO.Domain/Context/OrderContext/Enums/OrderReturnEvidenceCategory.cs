using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.OrderContext.Enums;

/// <summary>
/// Category of evidence photo attached to an <see cref="Aggregates.Orders.OrderReturn"/>.
/// Enforced on both the aggregate-level guard (which transition requires which category)
/// and on the upload command handler (which actor can add which category).
/// </summary>
public sealed class OrderReturnEvidenceCategory : EnumValueObject<OrderReturnEvidenceCategory>
{
    /// <summary>Photo captured by the buyer at parcel hand-off to the carrier. Required before <c>MarkReturnShipped</c>.</summary>
    public static readonly OrderReturnEvidenceCategory PickupByBuyer   = new("pickup_by_buyer");

    /// <summary>Photo captured by the seller when the parcel arrives. Required before <c>Resolve</c>.</summary>
    public static readonly OrderReturnEvidenceCategory ReceiptBySeller = new("receipt_by_seller");

    private OrderReturnEvidenceCategory(string id) : base(id) { }
}
