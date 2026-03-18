using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.NotificationContext.Enums;

public sealed class NotificationStatus : EnumValueObject<NotificationStatus>
{
    public static readonly NotificationStatus Unread = new("unread");
    public static readonly NotificationStatus Read = new("read");
    public static readonly NotificationStatus Archived = new("archived");
    public static readonly NotificationStatus Deleted = new("deleted");
    private NotificationStatus(string id) : base(id) { }
}