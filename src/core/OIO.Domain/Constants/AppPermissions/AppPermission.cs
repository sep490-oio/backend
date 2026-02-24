using OIO.Domain.Context.UserContext.Aggregates.Roles;

namespace OIO.Domain.Constants.AppPermissions;

public static partial class AppPermission
{
    public const string ExpiredTokenAllowed = "ExpiredTokenAllowed";
    public static readonly string[] All =
    [
        ..Permissions.All,
        ..RolePermissions.All,
        ..Roles.All,
        ..UserPermissions.All,
        ..UserRoles.All,
        ..Users.All
    ];
}