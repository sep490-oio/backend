using OIO.Domain.Context.UserContext.Aggregates.Roles;

namespace OIO.Domain.AppDefinitions;

public static partial class App
{
    public static class Permissions
    {
        public static class Catalogs
        {
            public static class Users
            {
                public const string ReadMe = "users:me:read";
                public const string UpdateMe = "users:me:update";

                public const string ReadAddresses = "users:addresses:read";
                public const string AddAddresses = "users:addresses:add";
                public const string UpdateAddresses = "users:addresses:update";
                public const string RemoveAddresses = "users:addresses:remove";
                public const string SetDefaultAddresses = "users:addresses:set-default";
                
                public const string UpdateStatus =  "users:status:update";

                public const string ChangePassword = "users:password:change";
                public const string EnableTwoFactor = "users:two-factor:enable";
                public const string DisableTwoFactor = "users:two-factor:disable";
                public const string SetPhone = "users:phone:set";
                public const string ConfirmPhone = "users:phone:confirm";

                public const string ReadSessions = "users:sessions:read";
                public const string ReadLoginHistory = "users:login-history:read";

                public const string Read = "users:read";
                public const string Update = "users:update";
                public const string Remove = "users:remove";
                public const string Unlock = "users:unlock";

                public const string AssignRoles = "users:roles:assign";
                public const string RevokeRoles = "users:roles:revoke";

                public const string GrantPermissions = "users:permissions:grant";
                public const string RevokePermissions = "users:permissions:revoke";
            }
            
            public static IReadOnlyList<string> All =>
            [
                Users.ReadMe,
                Users.UpdateMe,
                Users.ReadAddresses,
                Users.AddAddresses,
                Users.RemoveAddresses,
                Users.SetDefaultAddresses,
                Users.UpdateAddresses,
                Users.UpdateStatus,
                Users.ChangePassword,
                Users.EnableTwoFactor,
                Users.DisableTwoFactor,
                Users.SetPhone,
                Users.ConfirmPhone,
                Users.ReadSessions,
                Users.ReadLoginHistory,
                Users.Read,
                Users.Update,
                Users.Remove,
                Users.Unlock,
                Users.AssignRoles,
                Users.RevokeRoles,
                Users.GrantPermissions,
                Users.RevokePermissions
            ];
        }

        public static class Definitions
        {
            public static class Users
            {
                public static readonly Permission ReadMe = Permission.Create(1, Catalogs.Users.ReadMe);
                public static readonly Permission UpdateMe = Permission.Create(2, Catalogs.Users.UpdateMe);

                public static readonly Permission ReadAddresses = Permission.Create(3, Catalogs.Users.ReadAddresses);
                public static readonly Permission AddAddresses = Permission.Create(4, Catalogs.Users.AddAddresses);
                public static readonly Permission UpdateAddresses = Permission.Create(5, Catalogs.Users.UpdateAddresses);
                public static readonly Permission RemoveAddresses = Permission.Create(6, Catalogs.Users.RemoveAddresses);
                public static readonly Permission SetDefaultAddresses = Permission.Create(7, Catalogs.Users.SetDefaultAddresses);

                public static readonly Permission ChangePassword = Permission.Create(8, Catalogs.Users.ChangePassword);
                public static readonly Permission EnableTwoFactor = Permission.Create(9, Catalogs.Users.EnableTwoFactor);
                public static readonly Permission DisableTwoFactor = Permission.Create(10, Catalogs.Users.DisableTwoFactor);
                public static readonly Permission SetPhone = Permission.Create(11, Catalogs.Users.SetPhone);
                public static readonly Permission ConfirmPhone = Permission.Create(12, Catalogs.Users.ConfirmPhone);

                public static readonly Permission ReadSessions = Permission.Create(13, Catalogs.Users.ReadSessions);
                public static readonly Permission ReadLoginHistory = Permission.Create(14, Catalogs.Users.ReadLoginHistory);

                public static readonly Permission Read = Permission.Create(15, Catalogs.Users.Read);
                public static readonly Permission Update = Permission.Create(16, Catalogs.Users.Update);
                public static readonly Permission Remove = Permission.Create(17, Catalogs.Users.Remove);
                public static readonly Permission Unlock = Permission.Create(18, Catalogs.Users.Unlock);

                public static readonly Permission AssignRoles = Permission.Create(19, Catalogs.Users.AssignRoles);
                public static readonly Permission RevokeRoles = Permission.Create(20, Catalogs.Users.RevokeRoles);

                public static readonly Permission GrantPermissions = Permission.Create(21, Catalogs.Users.GrantPermissions);
                public static readonly Permission RevokePermissions = Permission.Create(22, Catalogs.Users.RevokePermissions);
                
                public static readonly Permission UpdateStatus = Permission.Create(23, Catalogs.Users.UpdateStatus);
            }
            
            public static IReadOnlyList<Permission> All =>
            [
                Users.ReadMe,
                Users.UpdateMe,
                Users.ReadAddresses,
                Users.AddAddresses,
                Users.UpdateAddresses,
                Users.RemoveAddresses,
                Users.SetDefaultAddresses,
                Users.ChangePassword,
                Users.EnableTwoFactor,
                Users.DisableTwoFactor,
                Users.SetPhone,
                Users.ConfirmPhone,
                Users.ReadSessions,
                Users.ReadLoginHistory,
                Users.Read,
                Users.Update,
                Users.Remove,
                Users.Unlock,
                Users.AssignRoles,
                Users.RevokeRoles,
                Users.GrantPermissions,
                Users.RevokePermissions,
                Users.UpdateStatus
            ];
        }
    }
}
