using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.OrderContext.Enums;

public sealed class OrderReturnStatus : EnumValueObject<OrderReturnStatus>
{
    public static readonly OrderReturnStatus Requested = new("requested");
    public static readonly OrderReturnStatus Approved = new("approved");
    public static readonly OrderReturnStatus Rejected = new("rejected");
    public static readonly OrderReturnStatus ReturnInTransit = new("return_in_transit");
    public static readonly OrderReturnStatus SellerReceived = new("seller_received");
    public static readonly OrderReturnStatus BuyerFollowup = new("buyer_followup");
    public static readonly OrderReturnStatus Resolved = new("resolved");
    public static readonly OrderReturnStatus Cancelled = new("cancelled");
    private OrderReturnStatus(string id) : base(id) { }
}