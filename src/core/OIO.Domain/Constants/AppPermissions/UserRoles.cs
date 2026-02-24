namespace OIO.Domain.Constants.AppPermissions;

public static partial class AppPermission
{
    public static class UserRoles
    {
        public const string Assign = "user-roles:assign";
        public const string Revoke = "user-roles:revoke";
        
        public static readonly string[] All =
        [
            Assign,
            Revoke,
        ];
    }
}