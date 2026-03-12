using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.ModerationContext.Enums;

public sealed class DisputePriority : EnumValueObject<DisputePriority>
{
    public static readonly DisputePriority None = new("none");
    public static readonly DisputePriority Low = new("low");
    public static readonly DisputePriority Medium = new("medium");
    public static readonly DisputePriority High = new("high");
    public static readonly DisputePriority Urgent = new("urgent");
    private DisputePriority(string id) : base(id) { }
}