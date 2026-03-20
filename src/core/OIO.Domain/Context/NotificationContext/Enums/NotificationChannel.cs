using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.NotificationContext.Enums;

public sealed class NotificationChannel : EnumValueObject<NotificationChannel>
{
    public static readonly NotificationChannel InApp = new("In App");
    public static readonly NotificationChannel Email = new("Email");
    public static readonly NotificationChannel SignalR = new("SignalR");

    private NotificationChannel(string id) : base(id) { }
}
