using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.ModerationContext.Enums;

public sealed class AlertStatus : EnumValueObject<AlertStatus>
{
    public static readonly AlertStatus Open = new("open");
    public static readonly AlertStatus Acknowledged = new("acknowledged");
    public static readonly AlertStatus Resolved = new("resolved");
    public static readonly AlertStatus Ignored = new("ignored");
    private AlertStatus(string id) : base(id) { }
}