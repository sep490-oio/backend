using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.NotificationContext.Enums;

public sealed class NotificationPriority : EnumValueObject<NotificationPriority>
{
    public static readonly NotificationPriority Low = new("low");
    public static readonly NotificationPriority Normal = new("normal");
    public static readonly NotificationPriority High = new("high");
    public static readonly NotificationPriority Urgent = new("urgent");
    private NotificationPriority(string id) : base(id) { }
}