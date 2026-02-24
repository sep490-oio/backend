using System.Net;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.UserContext.Aggregates.Users;

public sealed class UserRefreshToken : SeedWork.Entities.Entity<UserRefreshTokenId>, ICreatedAtEntity
{
    
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private UserRefreshToken() {}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    
    public UserId UserId { get; private set; }

    public string TokenHash { get; private set; }

    public UserRefreshTokenFamilyId FamilyId { get; private set; }

    public UserRefreshTokenId? ParentTokenId { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime ExpiresAt { get; private set; }

    public DateTime? RevokedAt { get; private set; }

    public string? RevokedReason { get; private set; }

    public IPAddress CreatedByIp { get; private set; }

    public IPAddress? RevokedByIp { get; private set; }

    public int RotationCounter { get; private set; }

    public bool IsUsed { get; private set; }

    public DateTime? UsedAt { get; private set; }

    public UserRefreshTokenFamily? RefreshTokenFamily { get; private set; }
    
    internal UserRefreshToken(
        UserRefreshTokenId id,
        UserId userId,
        UserRefreshTokenFamilyId familyId,
        string tokenHash,
        UserRefreshTokenId? parentTokenId,
        IPAddress ipAddress,
        DateTime expiresAt,
        DateTime createdAt,
        int rotationCounter)
    {
        Id = id;
        UserId = userId;
        FamilyId = familyId;
        TokenHash = tokenHash;
        ParentTokenId = parentTokenId;
        CreatedByIp = ipAddress;
        ExpiresAt = expiresAt;
        RotationCounter = rotationCounter;
        CreatedAt = createdAt;
        IsUsed = false;
    }

    public static UserRefreshToken Create(
        UserId userId,
        UserRefreshTokenFamilyId familyId,
        string tokenHash,
        UserRefreshTokenId? parentTokenId,
        IPAddress ipAddress,
        TimeSpan timeRefreshTokenExpiration,
        DateTime createdAt,
        int rotationCounter)
    {
        return new UserRefreshToken(
            UserRefreshTokenId.Create(),
            userId,
            familyId,
            tokenHash,
            parentTokenId,
            ipAddress,
            createdAt.Add(timeRefreshTokenExpiration),
            createdAt,
            rotationCounter
        );
    }
    
    public bool IsExpired(DateTime now) => now >= ExpiresAt;
    public bool IsRevoked => RevokedAt is not null;
    public bool IsActive(DateTime now) => !IsRevoked && !IsExpired(now) && !IsUsed;

    internal void MarkAsUsed(DateTime now)
    {
        IsUsed = true;
        UsedAt = now;
    }

    internal void Revoke(
        string reason, 
        DateTime now,
        IPAddress? revokedByIp = null)
    {
        if (IsRevoked) return;

        RevokedAt = now;
        RevokedReason = reason;
        RevokedByIp = revokedByIp;
    }
}