using OIO.Domain.Context.UserContext.Aggregates.Roles;
using OIO.Domain.Context.UserContext.ValueObjects;

namespace OIO.Domain.Context.UserContext.Aggregates.Users;

public sealed class UserRole
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private UserRole() {}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    
    public UserId UserId { get; private set; }

    public RoleId RoleId { get; private set; }

    public DateTime AssignedAt { get; private set; }
    
    public Role Role { get; private set; }
    
    internal UserRole(UserId userId, RoleId roleId, DateTime assignedAt)
    {
        
        UserId = userId;
        RoleId = roleId;
        AssignedAt = assignedAt;
    }
}