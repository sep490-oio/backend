using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.ModerationContext.Enums;

public sealed class AlertSeverity : EnumValueObject<AlertSeverity>
{
    public static readonly AlertSeverity Low = new("low");
    public static readonly AlertSeverity Medium = new("medium");
    public static readonly AlertSeverity High = new("high");
    public static readonly AlertSeverity Critical = new("critical");
    private AlertSeverity(string id) : base(id) { }
}