namespace OIO.Domain.Constants.AppPermissions;

public static partial class AppPermission
{
    public static class RolePermissions
    {
        public const string Assign = "role-permissions:assign";
        public const string Revoke = "role-permissions:revoke";
        
        public static readonly string[] All =
        [
            Assign,
            Revoke
        ];
    }   
}