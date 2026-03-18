using System.Net;
using CSharpFunctionalExtensions;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.UserContext.Aggregates.Users;

public sealed class UserSession : BaseEntity<UserSessionId>, ICreatedAtEntity
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private UserSession() {}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    
    private readonly List<UserRefreshToken> _tokens = [];
    
    public UserId UserId { get; private set; }

    public Guid DeviceId { get; private set; }

    public string UserAgent { get; private set; } = null!;

    public IPAddress IpAddress { get; private set; } = null!;

    public bool IsActive { get; private set; }

    public DateTime ExpiresAt { get; private set; }
    public DateTime AbsoluteExpiresAt { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime LastRotatedAt { get; private set; }

    public DateTime? RevokedAt { get; private set; }

    public string? RevokedReason { get; private set; }
    
    public IReadOnlyList<UserRefreshToken> Tokens => _tokens;
    
    public bool IsExpired(DateTime now) => now >= ExpiresAt;
    
    internal UserSession(
        UserSessionId id,
        UserId userId,
        Guid deviceId,
        string userAgent,
        IPAddress ipAddress,
        DateTime expiresAt,
        DateTime absoluteExpiresAt,
        DateTime createdAt,
        DateTime lastRotatedAt)
    {
        Id = id;
        UserId = userId;
        DeviceId = deviceId;
        UserAgent = userAgent;
        IpAddress = ipAddress;
        IsActive = true;
        ExpiresAt = expiresAt;
        AbsoluteExpiresAt = absoluteExpiresAt;
        CreatedAt = createdAt;
        LastRotatedAt = lastRotatedAt;
        if (ExpiresAt > AbsoluteExpiresAt)
            ExpiresAt = AbsoluteExpiresAt;
    }
    
    public bool IsAbsoluteExpired(DateTime now) => now >= AbsoluteExpiresAt;

   
    public TimeSpan RemainingAbsoluteTime(DateTime now) =>
        AbsoluteExpiresAt > now
            ? AbsoluteExpiresAt - now
            : TimeSpan.Zero;

  
    public bool IsNearingAbsoluteExpiration(DateTime now) =>
        RemainingAbsoluteTime(now) < TimeSpan.FromDays(1) && !IsAbsoluteExpired(now);

    public static Result<UserSession, Error> Create(
        UserId userId,
        Guid deviceId,
        string userAgent,
        IPAddress ipAddress,
        TimeSpan slidingExpiration,
        TimeSpan absoluteExpiration,
        DateTime now)
    {
        var session = new UserSession
        {
            Id = UserSessionId.From(Guid.CreateVersion7()),
            UserId = userId,
            DeviceId = deviceId,
            UserAgent = userAgent,
            IpAddress = ipAddress,
            IsActive = true,
            ExpiresAt = now.Add(slidingExpiration),
            AbsoluteExpiresAt = now.Add(absoluteExpiration),
            CreatedAt = now,
            LastRotatedAt = now
        };

        return session;
    }

    internal Result<UserRefreshToken, Error> CreateToken(
        string tokenHash,
        IPAddress ipAddress,
        TimeSpan timeRefreshTokenExpiration,
        DateTime now)
    {
        var activeR = EnsureActive(now);
        
        if (activeR.IsFailure) 
            return Result.Failure<UserRefreshToken, Error>(activeR.Error);

        var token = UserRefreshToken.Create(
            userId: UserId,
            sessionId: Id,
            tokenHash: tokenHash,
            parentTokenId: null,
            ipAddress: ipAddress,
            timeRefreshTokenExpiration: timeRefreshTokenExpiration,
            createdAt: now,
            rotationCounter: 0);

        _tokens.Add(token);
        return Result.Success<UserRefreshToken, Error>(token);
    }

    internal Result<UserRefreshToken, Error> RotateToken(
        UserRefreshToken currentToken,
        string newTokenHash,
        IPAddress ipAddress,
        TimeSpan timeRefreshTokenExpiration,
        TimeSpan slidingExpiration,
        DateTime now)
    {
        var activeR = EnsureActive(now);
        
        if (activeR.IsFailure) 
            return Result.Failure<UserRefreshToken, Error>(activeR.Error);

        if (currentToken.SessionId != Id)
            return UserErrors.RefreshToken.NotInSession;

        if (currentToken.IsUsed)
        {
            Revoke("Token reuse detected — possible token theft", now);
            return UserErrors.Auth.SessionCompromised;
        }

        if (currentToken.IsRevoked)
            return UserErrors.RefreshToken.Revoked;

        if (currentToken.IsExpired(now))
            return UserErrors.RefreshToken.Expired;

        currentToken.MarkAsUsed(now);
        
        ExtendSlidingExpiration(slidingExpiration, now);
        
        var remainingTime = ClampToAbsoluteExpiration(now, timeRefreshTokenExpiration);
        
        var newToken = UserRefreshToken.Create(
            userId: UserId,
            sessionId: Id,
            tokenHash: newTokenHash,
            parentTokenId: currentToken.Id,
            ipAddress: ipAddress,
            timeRefreshTokenExpiration: remainingTime,
            createdAt: now,
            rotationCounter: currentToken.RotationCounter + 1);

        _tokens.Add(newToken);
        
        LastRotatedAt = now;

        return Result.Success<UserRefreshToken, Error>(newToken);
    }

    internal void Revoke(string reason, DateTime now)
    {
        if (!IsActive) 
            return;

        IsActive = false;
        RevokedAt = now;
        RevokedReason = reason;

        foreach (var token in _tokens.Where(t => !t.IsRevoked))
            token.Revoke(reason, now);
    }

    private UnitResult<Error> EnsureActive(DateTime now)
    {
        if (!IsActive)
            return UserErrors.User.SessionNoLongerActive;

        if (IsAbsoluteExpired(now))
        {
            Revoke("Absolute expiration reached — re-authentication required", now);
            return UserErrors.User.SessionAbsoluteExpired;
        }
        
        if (!IsExpired(now)) 
            return UnitResult.Success<Error>();
        
        Revoke("Family expired", now);
        
        return UserErrors.User.SessionSlidingExpired;
    }
    
    private void ExtendSlidingExpiration(TimeSpan slidingDuration, DateTime now)
    {
        var newSlidingExpiry = now.Add(slidingDuration);

        // Clamp to absolute expiration
        ExpiresAt = newSlidingExpiry > AbsoluteExpiresAt
            ? AbsoluteExpiresAt
            : newSlidingExpiry;
    }
    
    private TimeSpan ClampToAbsoluteExpiration(DateTime now, TimeSpan timeRefreshTokenExpiration)
    {
        var newExpiration = now.Add(timeRefreshTokenExpiration);

        return newExpiration <= AbsoluteExpiresAt ? timeRefreshTokenExpiration : newExpiration - AbsoluteExpiresAt;
    }
}