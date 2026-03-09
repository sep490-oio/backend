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

    public string PermissionCode { get; private set; }
    public bool IsAllowed { get; private set; }

    public Permission Permission { get; private set; } = null!;

    private UserPermission(
        UserId userId,
        string permissionCode,
        bool isAllowed = true)
    {
        UserId = userId;
        PermissionCode = permissionCode;
        IsAllowed = isAllowed;
    }

    public static UserPermission Grant(
        UserId userId,
        string permissionCode)
    {
        return new UserPermission(userId, permissionCode);
    }

    public static UserPermission Deny(
        UserId userId,
        string permissionCode)
    {
        return new UserPermission(userId, permissionCode, false);
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