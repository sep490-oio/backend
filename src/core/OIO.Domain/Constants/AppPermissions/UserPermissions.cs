namespace OIO.Domain.Constants.AppPermissions;

public static partial class AppPermission
{
    public static class UserPermissions
    {
        public const string Read = "user-permissions:read";
        public const string Assign = "user-permissions:assign";
        public const string Revoke = "user-permissions:revoke";
        public const string Deny = "user-permissions:deny";
        public const string Accept = "user-permissions:accept";
        
        public static readonly string[] All =
        [
            Read,
            Assign,
            Revoke,
            Deny,
            Accept
        ];
    }
}