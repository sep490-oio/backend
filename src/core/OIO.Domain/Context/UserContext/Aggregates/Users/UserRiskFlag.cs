using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.UserContext.Aggregates.Users;

public sealed class UserRiskFlag : BaseEntity<UserRiskFlagId>, ICreatedAtEntity
{
    public UserId UserId { get; private set; }
    public string FlagType { get; private set; }
    public string? Reason { get; private set; }
    public RiskFlagSeverity Severity { get; private set; }
    public UserId? CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private UserRiskFlag() { }

    public static UserRiskFlag Create(
        UserId userId,
        string flagType,
        string? reason,
        RiskFlagSeverity severity,
        UserId? createdBy,
        DateTime nowUtc)
    {
        return new UserRiskFlag
        {
            Id = UserRiskFlagId.From(Guid.CreateVersion7()),
            UserId = userId,
            FlagType = flagType,
            Reason = reason,
            Severity = severity,
            CreatedBy = createdBy,
            CreatedAt = nowUtc
        };
    }
}
