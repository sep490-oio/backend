using OIO.Domain.Context.UserContext.Aggregates.Roles;

namespace OIO.Domain.AppDefinitions;

public static partial class App
{
    public static class Permissions
    {
        public static class Catalogs
        {
             // ==================== USER CONTEXT ====================
            public static class Users
            {
                // Profile
                public const string ReadMe = "users:me:read";
                public const string UpdateMe = "users:me:update";
                public const string ReadProfile = "users:profile:read";
                public const string UpdateProfile = "users:profile:update";

                // Addresses
                public const string ReadAddresses = "users:addresses:read";
                public const string AddAddresses = "users:addresses:add";
                public const string UpdateAddresses = "users:addresses:update";
                public const string RemoveAddresses = "users:addresses:remove";
                public const string SetDefaultAddresses = "users:addresses:set-default";

                // Security
                public const string ChangePassword = "users:password:change";
                public const string EnableTwoFactor = "users:two-factor:enable";
                public const string DisableTwoFactor = "users:two-factor:disable";
                public const string SetPhone = "users:phone:set";
                public const string ConfirmPhone = "users:phone:confirm";

                // Sessions & History
                public const string ReadSessions = "users:sessions:read";
                public const string ReadLoginHistory = "users:login-history:read";

                // Logout
                public const string Logout = "users:auth:logout";
            }

            public static class Admin
            {
                // User Management
                public const string ReadUser = "admin:users:read";
                public const string ReadRole = "admin:roles:read";
                public const string ReadPermission = "admin:permissions:read";
                public const string UpdateUserStatus = "admin:users:status:update";
                public const string DeleteUser = "admin:users:delete";
                public const string UnlockUser = "admin:users:unlock";

                // Role Management
                public const string AssignRole = "admin:users:roles:assign";
                public const string RevokeRole = "admin:users:roles:revoke";
                public const string ToggleRolePermission = "admin:roles:permissions:toggle";

                // Permission Management
                public const string GrantPermission = "admin:users:permissions:grant";
                public const string RevokePermission = "admin:users:permissions:revoke";
                public const string DenyPermission = "admin:users:permissions:deny";
            }

            // ==================== AUCTION CONTEXT ====================
            public static class Items
            {
                // Item CRUD (Seller)
                public const string Create = "items:create";
                public const string ReadMy = "items:my:read";
                public const string Activate = "items:activate";

                // Medias (Seller)
                public const string AddMedia = "items:media:add";
                public const string RemoveMedia = "items:media:remove";

                // Questions
                public const string AskQuestion = "items:questions:ask";
                public const string AnswerQuestion = "items:questions:answer";
                public const string ReadQuestions = "items:questions:read";
            }

            public static class Auctions
            {
                public const string ReadMy = "auctions:me:read";
                public const string ReadMyBids = "auctions:bids:me:read";
                // Auction Lifecycle (Seller)
                public const string Create = "auctions:create";
                public const string Publish = "auctions:publish";
                public const string Cancel = "auctions:cancel";

                // Bidding (Bidder)
                public const string PlaceBid = "auctions:bids:place";
                public const string BuyNow = "auctions:buy-now";
                public const string ReadBids = "auctions:bids:read";

                // Auto-Bid (Bidder)
                public const string ConfigureAutoBid = "auctions:auto-bid:configure";
                public const string PauseAutoBid = "auctions:auto-bid:pause";
                public const string ResumeAutoBid = "auctions:auto-bid:resume";

                // Watch (Bidder)
                public const string Watch = "auctions:watch";
                public const string ReadMyWatch = "auctions:watch:me:read";
                public const string Unwatch = "auctions:unwatch";
            }

            public static class Categories
            {
                // Public read — no permission needed
                public const string ReadAll = "categories:read";
                public const string ReadChildren = "categories:children:read";
            }

            // ==================== ALL PERMISSIONS ====================
            public static IReadOnlyList<string> All =>
            [
                // Users
                Users.ReadMe,
                Users.UpdateMe,
                Users.ReadProfile,
                Users.UpdateProfile,
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
                Users.Logout,

                // Admin
                Admin.ReadUser,
                Admin.ReadPermission,
                Admin.UpdateUserStatus,
                Admin.DeleteUser,
                Admin.UnlockUser,
                Admin.AssignRole,
                Admin.RevokeRole,
                Admin.ReadRole,
                Admin.ToggleRolePermission,
                Admin.GrantPermission,
                Admin.RevokePermission,
                Admin.DenyPermission,

                // Items
                Items.Create,
                Items.ReadMy,
                Items.Activate,
                Items.AddMedia,
                Items.RemoveMedia,
                Items.AskQuestion,
                Items.AnswerQuestion,
                Items.ReadQuestions,

                // Auctions
                Auctions.Create,
                Auctions.Publish,
                Auctions.Cancel,
                Auctions.PlaceBid,
                Auctions.BuyNow,
                Auctions.ReadBids,
                Auctions.ConfigureAutoBid,
                Auctions.PauseAutoBid,
                Auctions.ResumeAutoBid,
                Auctions.Watch,
                Auctions.Unwatch,
                Auctions.ReadMy,
                Auctions.ReadMyBids,
                Auctions.ReadMyWatch,

                // Categories
                Categories.ReadAll,
                Categories.ReadChildren,
            ];

            public static readonly HashSet<string> CriticalPermissions =
            [
                Admin.AssignRole,
                Admin.RevokeRole,
                Admin.GrantPermission,
                Admin.RevokePermission,
                Admin.DenyPermission,
                Admin.ToggleRolePermission,
                Admin.DeleteUser,
            ];
        }

        public static class Definitions
        {
            public static class Users
            {
                public static readonly Permission ReadMe = Permission.Create(1, Catalogs.Users.ReadMe);
                public static readonly Permission UpdateMe = Permission.Create(2, Catalogs.Users.UpdateMe);
                public static readonly Permission ReadProfile = Permission.Create(3, Catalogs.Users.ReadProfile);
                public static readonly Permission UpdateProfile = Permission.Create(4, Catalogs.Users.UpdateProfile);

                public static readonly Permission ReadAddresses = Permission.Create(5, Catalogs.Users.ReadAddresses);
                public static readonly Permission AddAddresses = Permission.Create(6, Catalogs.Users.AddAddresses);
                public static readonly Permission UpdateAddresses = Permission.Create(7, Catalogs.Users.UpdateAddresses);
                public static readonly Permission RemoveAddresses = Permission.Create(8, Catalogs.Users.RemoveAddresses);
                public static readonly Permission SetDefaultAddresses = Permission.Create(9, Catalogs.Users.SetDefaultAddresses);

                public static readonly Permission ChangePassword = Permission.Create(10, Catalogs.Users.ChangePassword);
                public static readonly Permission EnableTwoFactor = Permission.Create(11, Catalogs.Users.EnableTwoFactor);
                public static readonly Permission DisableTwoFactor = Permission.Create(12, Catalogs.Users.DisableTwoFactor);
                public static readonly Permission SetPhone = Permission.Create(13, Catalogs.Users.SetPhone);
                public static readonly Permission ConfirmPhone = Permission.Create(14, Catalogs.Users.ConfirmPhone);

                public static readonly Permission ReadSessions = Permission.Create(15, Catalogs.Users.ReadSessions);
                public static readonly Permission ReadLoginHistory = Permission.Create(16, Catalogs.Users.ReadLoginHistory);

                public static readonly Permission Logout = Permission.Create(17, Catalogs.Users.Logout);
            }

            public static class Admin
            {
                public static readonly Permission ReadUser = Permission.Create(18, Catalogs.Admin.ReadUser);
                public static readonly Permission UpdateUserStatus = Permission.Create(19, Catalogs.Admin.UpdateUserStatus);
                public static readonly Permission DeleteUser = Permission.Create(20, Catalogs.Admin.DeleteUser);
                public static readonly Permission UnlockUser = Permission.Create(21, Catalogs.Admin.UnlockUser);

                public static readonly Permission AssignRole = Permission.Create(22, Catalogs.Admin.AssignRole);
                public static readonly Permission RevokeRole = Permission.Create(23, Catalogs.Admin.RevokeRole);
                public static readonly Permission ReadRole = Permission.Create(24, Catalogs.Admin.ReadRole);
                public static readonly Permission ToggleRolePermission = Permission.Create(25, Catalogs.Admin.ToggleRolePermission);

                public static readonly Permission GrantPermission = Permission.Create(26, Catalogs.Admin.GrantPermission);
                public static readonly Permission RevokePermission = Permission.Create(27, Catalogs.Admin.RevokePermission);
                public static readonly Permission DenyPermission = Permission.Create(28, Catalogs.Admin.DenyPermission);
                
            }

            public static class Items
            {
                public static readonly Permission Create = Permission.Create(29, Catalogs.Items.Create);
                public static readonly Permission ReadMy = Permission.Create(30, Catalogs.Items.ReadMy);
                public static readonly Permission Activate = Permission.Create(31, Catalogs.Items.Activate);
                public static readonly Permission AddMedia = Permission.Create(32, Catalogs.Items.AddMedia);
                public static readonly Permission RemoveMedia = Permission.Create(33, Catalogs.Items.RemoveMedia);
                public static readonly Permission AskQuestion = Permission.Create(34, Catalogs.Items.AskQuestion);
                public static readonly Permission AnswerQuestion = Permission.Create(35, Catalogs.Items.AnswerQuestion);
                public static readonly Permission ReadQuestions = Permission.Create(36, Catalogs.Items.ReadQuestions);
            }

            public static class Auctions
            {
                public static readonly Permission Create = Permission.Create(37, Catalogs.Auctions.Create);
                public static readonly Permission Publish = Permission.Create(38, Catalogs.Auctions.Publish);
                public static readonly Permission Cancel = Permission.Create(39, Catalogs.Auctions.Cancel);
                public static readonly Permission PlaceBid = Permission.Create(40, Catalogs.Auctions.PlaceBid);
                public static readonly Permission BuyNow = Permission.Create(41, Catalogs.Auctions.BuyNow);
                public static readonly Permission ReadBids = Permission.Create(42, Catalogs.Auctions.ReadBids);
                public static readonly Permission ConfigureAutoBid = Permission.Create(43, Catalogs.Auctions.ConfigureAutoBid);
                public static readonly Permission PauseAutoBid = Permission.Create(44, Catalogs.Auctions.PauseAutoBid);
                public static readonly Permission ResumeAutoBid = Permission.Create(45, Catalogs.Auctions.ResumeAutoBid);
                public static readonly Permission Watch = Permission.Create(46, Catalogs.Auctions.Watch);
                public static readonly Permission Unwatch = Permission.Create(47, Catalogs.Auctions.Unwatch);
            }

            public static class Categories
            {
                public static readonly Permission ReadAll = Permission.Create(48, Catalogs.Categories.ReadAll);
                public static readonly Permission ReadChildren = Permission.Create(49, Catalogs.Categories.ReadChildren);
                public static readonly Permission ReadMyWatch = Permission.Create(51, Catalogs.Auctions.ReadMyWatch);
                
            }

            public static IReadOnlyList<Permission> All =>
            [
                // Users
                Users.ReadMe,
                Users.UpdateMe,
                Users.ReadProfile,
                Users.UpdateProfile,
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
                Users.Logout,

                // Admin
                Admin.ReadUser,
                Admin.UpdateUserStatus,
                Admin.DeleteUser,
                Admin.UnlockUser,
                Admin.AssignRole,
                Admin.RevokeRole,
                Admin.ReadRole,
                Admin.ToggleRolePermission,
                Admin.GrantPermission,
                Admin.RevokePermission,
                Admin.DenyPermission,

                // Items
                Items.Create,
                Items.ReadMy,
                Items.Activate,
                Items.AddMedia,
                Items.RemoveMedia,
                Items.AskQuestion,
                Items.AnswerQuestion,
                Items.ReadQuestions,

                // Auctions
                Auctions.Create,
                Auctions.Publish,
                Auctions.Cancel,
                Auctions.PlaceBid,
                Auctions.BuyNow,
                Auctions.ReadBids,
                Auctions.ConfigureAutoBid,
                Auctions.PauseAutoBid,
                Auctions.ResumeAutoBid,
                Auctions.Watch,
                Auctions.Unwatch,

                // Categories
                Categories.ReadAll,
                Categories.ReadChildren,
            ];
        }
    }
}
