using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.ModerationContext.Enums;

public sealed class DisputeType : EnumValueObject<DisputeType>
{
    public static readonly DisputeType ItemNotReceived = new("item_not_received");
    public static readonly DisputeType ItemNotAsDescribed = new("item_not_as_described");
    public static readonly DisputeType DamagedItem = new("damaged_item");
    public static readonly DisputeType Counterfeit = new("counterfeit");
    public static readonly DisputeType PaymentIssue = new("payment_issue");
    public static readonly DisputeType ShippingIssue = new("shipping_issue");
    public static readonly DisputeType SellerUnresponsive = new("seller_unresponsive");
    public static readonly DisputeType VerificationCorrection = new("verification_correction");
    public static readonly DisputeType Other = new("other");
    private DisputeType(string id) : base(id) { }
}
