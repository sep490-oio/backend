using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.WarehouseContext.Enums;

/// <summary>
/// Category of evidence photo attached to a <c>WarehouseToSellerShipment</c>.
/// Mirror of <c>OrderReturnEvidenceCategory</c> but scoped to the warehouse-to-seller
/// return flow (warehouse staff at pickup, seller at receipt).
/// </summary>
public sealed class WarehouseReturnEvidenceCategory : EnumValueObject<WarehouseReturnEvidenceCategory>
{
    /// <summary>Photo captured by warehouse staff at parcel hand-off to the carrier. Required before <c>MarkShipped</c>.</summary>
    public static readonly WarehouseReturnEvidenceCategory PickupByWarehouseStaff = new("pickup_by_warehouse_staff");

    /// <summary>Photo captured by the seller when the parcel arrives. Required before <c>ConfirmBySeller</c>.</summary>
    public static readonly WarehouseReturnEvidenceCategory ReceiptBySeller        = new("receipt_by_seller");

    private WarehouseReturnEvidenceCategory(string id) : base(id) { }
}
