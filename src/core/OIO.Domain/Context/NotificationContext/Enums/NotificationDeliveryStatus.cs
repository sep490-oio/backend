using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.NotificationContext.Enums;

public sealed class NotificationDeliveryStatus : EnumValueObject<NotificationDeliveryStatus>
{
    public static readonly NotificationDeliveryStatus Pending = new("Pending");
    public static readonly NotificationDeliveryStatus Sent = new("Sent");
    public static readonly NotificationDeliveryStatus Failed = new("Failed");

    private NotificationDeliveryStatus(string id) : base(id) { }
}
