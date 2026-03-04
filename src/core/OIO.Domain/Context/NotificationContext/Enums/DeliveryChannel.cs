using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.NotificationContext.Enums;

public sealed class DeliveryChannel : EnumValueObject<DeliveryChannel>
{
    public static readonly DeliveryChannel Email = new("email");
    public static readonly DeliveryChannel Push = new("push");
    public static readonly DeliveryChannel Sms = new("sms");

    private DeliveryChannel(string id) : base(id) { }
}
