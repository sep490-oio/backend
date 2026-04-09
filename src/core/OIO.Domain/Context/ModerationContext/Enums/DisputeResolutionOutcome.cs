using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.ModerationContext.Enums;

public sealed class DisputeResolutionOutcome : EnumValueObject<DisputeResolutionOutcome>
{
    public static readonly DisputeResolutionOutcome FavorBuyer = new("favor_buyer");
    public static readonly DisputeResolutionOutcome FavorSeller = new("favor_seller");
    public static readonly DisputeResolutionOutcome FavorPlatform = new("favor_platform");
    public static readonly DisputeResolutionOutcome PartialSplit = new("partial_split");
    public static readonly DisputeResolutionOutcome VoidClaim = new("void_claim");
    public static readonly DisputeResolutionOutcome OperationalRework = new("operational_rework");
    private DisputeResolutionOutcome(string id) : base(id) { }
}
