using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.ModerationContext.Enums;

public sealed class DisputeMessageVisibility : EnumValueObject<DisputeMessageVisibility>
{
    public static readonly DisputeMessageVisibility External = new("external");
    public static readonly DisputeMessageVisibility Internal = new("internal");
    private DisputeMessageVisibility(string id) : base(id) { }
}
