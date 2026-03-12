using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.ModerationContext.Enums;

public sealed class DesiredResolution : EnumValueObject<DesiredResolution>
{
    public static readonly DesiredResolution NoAction = new("no_action");
    public static readonly DesiredResolution Refund = new("refund");
    public static readonly DesiredResolution Replacement = new("replacement");
    public static readonly DesiredResolution PartialRefund = new("partial_refund");
    public static readonly DesiredResolution Other = new("other");
    private DesiredResolution(string id) : base(id) { }
}