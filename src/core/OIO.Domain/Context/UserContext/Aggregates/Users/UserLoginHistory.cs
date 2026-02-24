using System.Net;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.ValueObjects;

namespace OIO.Domain.Context.UserContext.Aggregates.Users;

public sealed class UserLoginHistory : SeedWork.Entities.Entity<UserLoginHistoryId>
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private UserLoginHistory() {}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    
    public UserId UserId { get; private set; }

    public IPAddress IpAddress { get; private set; }

    public string UserAgent { get; private set; }

    public DateTime LoginAt { get; private set; }

    public LoginStatus Status { get; private set; }
    
    internal UserLoginHistory(
        UserId userId,
        IPAddress ipAddress,
        string userAgent,
        LoginStatus status,
        DateTime loginAt)
    {
        Id = UserLoginHistoryId.Create();
        UserId = userId;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        LoginAt = loginAt;
        Status = status;
    }
}