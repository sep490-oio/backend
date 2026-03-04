using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.NotificationContext.Enums;

public sealed class DeliveryStatus : EnumValueObject<DeliveryStatus>
{
    public static readonly DeliveryStatus Pending = new("pending");
    public static readonly DeliveryStatus Sent = new("sent");
    public static readonly DeliveryStatus Delivered = new("delivered");
    public static readonly DeliveryStatus Failed = new("failed");
    public static readonly DeliveryStatus Cancelled = new("cancelled");

    private DeliveryStatus(string id) : base(id) { }
}
