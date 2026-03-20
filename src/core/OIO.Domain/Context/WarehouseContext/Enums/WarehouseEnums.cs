using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.WarehouseContext.Enums;

public sealed class ShippingProviderCode : EnumValueObject<ShippingProviderCode>
{
    public static readonly ShippingProviderCode Ghn      = new("ghn");
    public static readonly ShippingProviderCode Ghtk     = new("ghtk");
    public static readonly ShippingProviderCode External  = new("external");

    private ShippingProviderCode(string id) : base(id) { }
}
public sealed class InboundShipmentMode : EnumValueObject<InboundShipmentMode>
{
    public static readonly InboundShipmentMode PlatformManaged = new("platform_managed");
    public static readonly InboundShipmentMode ExternalCarrier  = new("external_carrier");

    private InboundShipmentMode(string id) : base(id) { }
}
public sealed class OutboundShipmentMode : EnumValueObject<OutboundShipmentMode>
{
    public static readonly OutboundShipmentMode PlatformManaged = new("platform_managed");
    public static readonly OutboundShipmentMode SellerSelfShip    = new("seller_self_ship");

    private OutboundShipmentMode(string id) : base(id) { }
}
public sealed class ShippingEnvironment : EnumValueObject<ShippingEnvironment>
{
    public static readonly ShippingEnvironment Sandbox    = new("sandbox");
    public static readonly ShippingEnvironment Production = new("production");

    private ShippingEnvironment(string id) : base(id) { }
}

public sealed class InboundShipmentStatus : EnumValueObject<InboundShipmentStatus>
{
    public static readonly InboundShipmentStatus AwaitingPickup = new("awaiting_pickup");
    public static readonly InboundShipmentStatus InTransit      = new("in_transit");
    public static readonly InboundShipmentStatus Arrived        = new("arrived");
    public static readonly InboundShipmentStatus Inspected      = new("inspected");
    public static readonly InboundShipmentStatus Completed      = new("completed");
    public static readonly InboundShipmentStatus Cancelled      = new("cancelled");
    public static readonly InboundShipmentStatus Failed         = new("failed");

    private InboundShipmentStatus(string id) : base(id) { }
}

public sealed class OutboundShipmentStatus : EnumValueObject<OutboundShipmentStatus>
{
    public static readonly OutboundShipmentStatus Pending           = new("pending");
    public static readonly OutboundShipmentStatus Booked            = new("booked");
    public static readonly OutboundShipmentStatus PickedUp          = new("picked_up");
    public static readonly OutboundShipmentStatus InTransit         = new("in_transit");
    public static readonly OutboundShipmentStatus Delivered         = new("delivered");
    public static readonly OutboundShipmentStatus Failed            = new("failed");
    public static readonly OutboundShipmentStatus Returning         = new("returning");
    public static readonly OutboundShipmentStatus Returned          = new("returned");
    public static readonly OutboundShipmentStatus Cancelled         = new("cancelled");

    private OutboundShipmentStatus(string id) : base(id) { }
}

public sealed class WarehouseItemStatus : EnumValueObject<WarehouseItemStatus>
{
    public static readonly WarehouseItemStatus Pending    = new("pending");
    public static readonly WarehouseItemStatus Received   = new("received");
    public static readonly WarehouseItemStatus Inspected  = new("inspected");
    public static readonly WarehouseItemStatus Stored     = new("stored");
    public static readonly WarehouseItemStatus Reserved   = new("reserved");
    public static readonly WarehouseItemStatus Dispatched = new("dispatched");

    private WarehouseItemStatus(string id) : base(id) { }
}

public sealed class WarehouseItemCondition : EnumValueObject<WarehouseItemCondition>
{
    public static readonly WarehouseItemCondition New        = new("new");
    public static readonly WarehouseItemCondition LikeNew    = new("like_new");
    public static readonly WarehouseItemCondition VeryGood   = new("very_good");
    public static readonly WarehouseItemCondition Good       = new("good");
    public static readonly WarehouseItemCondition Acceptable = new("acceptable");
    public static readonly WarehouseItemCondition Damaged    = new("damaged");

    private WarehouseItemCondition(string id) : base(id) { }
}

public sealed class WarehouseInspectionDecisionStatus : EnumValueObject<WarehouseInspectionDecisionStatus>
{
    public static readonly WarehouseInspectionDecisionStatus PendingReview = new("pending_review");
    public static readonly WarehouseInspectionDecisionStatus Approved = new("approved");
    public static readonly WarehouseInspectionDecisionStatus Rejected = new("rejected");
    public static readonly WarehouseInspectionDecisionStatus ConditionConfirmationRequired = new("condition_confirmation_required");
    public static readonly WarehouseInspectionDecisionStatus ConditionConfirmed = new("condition_confirmed");

    private WarehouseInspectionDecisionStatus(string id) : base(id) { }
}

public sealed class NormalizedTrackingStatus : EnumValueObject<NormalizedTrackingStatus>
{
    public static readonly NormalizedTrackingStatus Confirmed  = new("confirmed");
    public static readonly NormalizedTrackingStatus PickedUp   = new("picked_up");
    public static readonly NormalizedTrackingStatus InTransit  = new("in_transit");
    public static readonly NormalizedTrackingStatus Delivered  = new("delivered");
    public static readonly NormalizedTrackingStatus Failed     = new("failed");
    public static readonly NormalizedTrackingStatus Delayed    = new("delayed");
    public static readonly NormalizedTrackingStatus Returning  = new("returning");
    public static readonly NormalizedTrackingStatus Returned   = new("returned");
    public static readonly NormalizedTrackingStatus Cancelled  = new("cancelled");

    private NormalizedTrackingStatus(string id) : base(id) { }
}

/// <summary>
/// GHN-specific required_note field on create order.
/// Instructs the carrier how the buyer may interact with the package.
/// </summary>
public sealed class GhnHandlingNote : EnumValueObject<GhnHandlingNote>
{
    /// <summary>Allow buyer to inspect and try before accepting.</summary>
    public static readonly GhnHandlingNote AllowTry     = new("CHOTHUHANG");

    /// <summary>Allow buyer to see but not try.</summary>
    public static readonly GhnHandlingNote AllowSee     = new("CHOXEMHANGKHONGTHU");

    /// <summary>Do not allow buyer to inspect at all.</summary>
    public static readonly GhnHandlingNote NoInspection = new("KHONGCHOXEMHANG");

    private GhnHandlingNote(string id) : base(id) { }
}

/// <summary>
/// GHN-specific payment_type_id: who pays the shipping fee.
/// </summary>
public sealed class GhnPaymentType : EnumValueObject<GhnPaymentType>
{
    /// <summary>Warehouse/shop pays the shipping fee.</summary>
    public static readonly GhnPaymentType ShopPays  = new("1");

    /// <summary>Buyer pays the shipping fee (COD).</summary>
    public static readonly GhnPaymentType BuyerPays = new("2");

    private GhnPaymentType(string id) : base(id) { }
}
