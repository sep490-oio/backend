using OIO.Domain.Context.UserContext.Aggregates.Roles;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.UserContext.Aggregates.Users;

public sealed class UserPermission : IEntity
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private UserPermission() {}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    public UserId UserId { get; private set; }

    public PermissionId PermissionId { get; private set; }
    public bool IsAllowed { get; private set; }

    public Permission Permission { get; private set; } = null!;

    private UserPermission(
        UserId userId,
        PermissionId permissionId,
        bool isAllowed = true)
    {
        UserId = userId;
        PermissionId = permissionId;
        IsAllowed = isAllowed;
    }

    public static UserPermission Grant(
        UserId userId,
        PermissionId permissionId)
    {
        return new UserPermission(userId, permissionId);
    }

    public static UserPermission Deny(
        UserId userId,
        PermissionId permissionId)
    {
        return new UserPermission(userId, permissionId, false);
    }

    internal void Grant()
    {
        IsAllowed = true;
    }

    internal void Deny()
    {
        IsAllowed = false;
    }
}