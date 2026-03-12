using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.AuctionContext.Enums;

public sealed class EmergencyStatus : EnumValueObject<EmergencyStatus>
{
    public static readonly EmergencyStatus Triggered = new("triggered");
    public static readonly EmergencyStatus Investigating = new("investigating");
    public static readonly EmergencyStatus Mitigated = new("mitigated");
    public static readonly EmergencyStatus Resolved = new("resolved");
    public static readonly EmergencyStatus Dismissed = new("dismissed");
    private EmergencyStatus(string id) : base(id) { }
}