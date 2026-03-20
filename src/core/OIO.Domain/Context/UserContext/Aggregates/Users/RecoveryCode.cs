using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.UserContext.Aggregates.Users;

public sealed class RecoveryCode : BaseEntity<RecoveryCodeId>, ICreatedAtEntity
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private RecoveryCode() {}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    public UserId UserId { get; private set; }

    public string CodeHash { get; private set; } = null!;

    public bool IsUsed { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UsedAt { get; private set; }

    public static RecoveryCode Create(UserId userId, string codeHash, DateTime nowUtc)
    {
        return new RecoveryCode
        {
            Id = RecoveryCodeId.From(Guid.CreateVersion7()),
            UserId = userId,
            CodeHash = codeHash,
            IsUsed = false,
            CreatedAt = nowUtc
        };
    }

    public void MarkAsUsed(DateTime nowUtc)
    {
        IsUsed = true;
        UsedAt = nowUtc;
    }
}
