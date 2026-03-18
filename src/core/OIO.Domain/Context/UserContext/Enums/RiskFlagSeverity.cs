using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.UserContext.Enums;

public sealed class RiskFlagSeverity : EnumValueObject<RiskFlagSeverity>
{
    public static readonly RiskFlagSeverity Low = new("low");
    public static readonly RiskFlagSeverity Medium = new("medium");
    public static readonly RiskFlagSeverity High = new("high");
    public static readonly RiskFlagSeverity Critical = new("critical");
    private RiskFlagSeverity(string id) : base(id) { }
}