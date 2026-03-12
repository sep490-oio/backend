using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.OrderContext.Enums;

public sealed class OrderStatus : EnumValueObject<OrderStatus>
{
    public static readonly OrderStatus PendingPayment = new("pending_payment");
    public static readonly OrderStatus Paid = new("paid");
    public static readonly OrderStatus Processing = new("processing");
    public static readonly OrderStatus Shipped = new("shipped");
    public static readonly OrderStatus Delivered = new("delivered");
    public static readonly OrderStatus Completed = new("completed");
    public static readonly OrderStatus Cancelled = new("cancelled");
    public static readonly OrderStatus Refunded = new("refunded");
    public static readonly OrderStatus Disputed = new("disputed");
    private OrderStatus(string id) : base(id) { }
}