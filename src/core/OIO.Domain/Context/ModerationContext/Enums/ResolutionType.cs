using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.ModerationContext.Enums;

public sealed class ResolutionType : EnumValueObject<ResolutionType>
{
    public static readonly ResolutionType NoResolution = new("no_resolution");
    public static readonly ResolutionType RefundFull = new("refund_full");
    public static readonly ResolutionType RefundPartial = new("refund_partial");
    public static readonly ResolutionType Replacement = new("replacement");
    public static readonly ResolutionType FavorBuyer = new("favor_buyer");
    public static readonly ResolutionType FavorSeller = new("favor_seller");
    public static readonly ResolutionType MutualAgreement = new("mutual_agreement");
    public static readonly ResolutionType NoAction = new("no_action");
    public static readonly ResolutionType Cancelled = new("cancelled");
    private ResolutionType(string id) : base(id) { }
}