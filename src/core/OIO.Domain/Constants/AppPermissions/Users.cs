namespace OIO.Domain.Constants.AppPermissions;

public static partial class AppPermission
{
    public static class Users
    {
        public const string Read = "users:read";
        public const string Create = "users:create";
        public const string Update = "users:update";
        public const string Delete = "users:delete";
        public const string Ban = "users:ban";
        public const string Unban = "users:unban";
        public const string Verify = "users:verify";
        public const string ReadLoginHistory = "users:login-history:read";
        
        public static readonly string[] All =
        [
            Read,
            Create,
            Update,
            Delete,
            Ban,
            Unban,
            Verify,
            ReadLoginHistory
        ];
    }
}