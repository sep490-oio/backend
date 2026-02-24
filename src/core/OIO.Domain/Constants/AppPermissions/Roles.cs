namespace OIO.Domain.Constants.AppPermissions;

public static partial class AppPermission
{
    public static class Roles
    {
        public const string Read = "roles:read";
        public const string Create = "roles:create";
        public const string Update = "roles:update";
        public const string Delete = "roles:delete";
        
        public static readonly string[] All =
        [
            Read,
            Create,
            Update,
            Delete
        ];
    }
}