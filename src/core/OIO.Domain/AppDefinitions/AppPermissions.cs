using OIO.Domain.Context.UserContext.Aggregates.Roles;

namespace OIO.Domain.AppDefinitions;

public static partial class App
{
    public static class Permissions
    {
        public static class Catalogs
        {
             // ==================== USER CONTEXT ====================
            public static class Me
            {
                public const string Read = "users:me:read";
                
                public const string ReadProfile = "users:me:profile:read";
                public const string UpdateProfile = "users:me:profile:update";
                
                public const string ChangePassword = "users:me:password:change";
                
                public const string ReadSessions = "users:me:sessions:read";
                
                public const string ReadLoginHistory = "users:me:login-history";
                
                public const string ManageTwoFactor = "users:me:2fa:manage";
                
                public const string ManagePhone = "users:me:phone:manage";
                
                public const string ReadAddress = "users:me:address:read";
                
                public const string ManageAddress = "users:me:address:manage";
                
                public const string ReadAuctions = "me:auctions:read";
                
                public const string ReadBids = "me:bids:read";
                
                public const string ReadWatchlist = "me:watchlist:read";
            }

            // ==================== Items ====================
            public static class Items
            {
                public const string Create = "items:create";
                public const string Activate = "items:activate";
                public const string ReadMy = "items:my:read";
                public const string ManageMedia = "items:media:manage";
                public const string AskQuestion = "items:questions:ask";
                public const string AnswerQuestion = "items:questions:answer";
            }

            // ==================== Auctions ====================
            public static class Auctions
            {
                public const string Create = "auctions:create";
                public const string Publish = "auctions:publish";
                public const string Cancel = "auctions:cancel";
                public const string Bid = "auctions:bid";
                public const string BuyNow = "auctions:buy-now";
                public const string AutoBid = "auctions:auto-bid";
                public const string ReadAutoBid = "auctions:auto-bid:read";
                public const string Watch = "auctions:watch";
                public const string Unwatch = "auctions:unwatch";
            }

            // ==================== Media ====================
            public static class Media
            {
                public const string ReadContexts = "media:contexts:read";
                public const string Upload = "media:upload";
                public const string ConfirmUpload = "media:upload:confirm";
            }

            // ==================== Categories ====================
            public static class Categories
            {
                public const string Create = "categories:create";
                public const string Update = "categories:update";
            }
            
            // ==================== Admin ====================
            public static class Admin
            {
                public const string ReadUsers = "admin:users:read";
                public const string ManageUsers = "admin:users:manage";
                public const string DeleteUsers = "admin:users:delete";
                public const string ReadRoles = "admin:roles:read";
                public const string AssignRole = "admin:roles:assign";
                public const string RevokeRole = "admin:roles:revoke";
                public const string ReadPermissions = "admin:permissions:read";
                public const string GrantPermission = "admin:permissions:grant";
                public const string RevokePermission = "admin:permissions:revoke";
                public const string DenyPermission = "admin:permissions:deny";
                public const string ManagePermissions = "admin:permissions:manage";
                public const string ReadSettings = "admin:settings:read";
                public const string ManageSettings = "admin:settings:manage";
            }

            // ==================== ALL PERMISSIONS ====================
            public static HashSet<string> All =>
            [
                // Users
                Me.Read,
                Me.ReadProfile,
                Me.UpdateProfile,
                Me.ChangePassword,
                Me.ReadSessions,
                Me.ReadLoginHistory,
                Me.ManageTwoFactor,
                Me.ManagePhone,
                Me.ReadAddress,
                Me.ManageAddress,
                Me.ReadAuctions,
                Me.ReadBids,
                Me.ReadWatchlist,

                // Admin
                Admin.ReadUsers,
                Admin.ManageUsers,
                Admin.DeleteUsers,
                Admin.ReadRoles,
                Admin.AssignRole,
                Admin.RevokeRole,
                Admin.ReadPermissions,
                Admin.GrantPermission,
                Admin.RevokePermission,
                Admin.DenyPermission,
                Admin.ManagePermissions,
                Admin.ReadSettings,

                // Items
                Items.Create,
                Items.Activate,
                Items.ReadMy,
                Items.ManageMedia,
                Items.AskQuestion,
                Items.AnswerQuestion,

                // Auctions
                Auctions.Create,
                Auctions.Publish,
                Auctions.Cancel,
                Auctions.Bid,
                Auctions.BuyNow,
                Auctions.AutoBid,
                Auctions.ReadAutoBid,
                Auctions.Watch,
                Auctions.Unwatch,

                // Categories
                Categories.Create,
                Categories.Update,
                
                Media.ReadContexts,
                Media.Upload,
                Media.ConfirmUpload,
            ];

            public static readonly HashSet<string> CriticalPermissions =
            [
                Admin.AssignRole,
                Admin.RevokeRole,
                Admin.GrantPermission,
                Admin.RevokePermission,
                Admin.DenyPermission,
                Admin.ManagePermissions,
                Admin.ManageUsers,
                Admin.ManageSettings,
                Admin.ReadUsers,
                Admin.DeleteUsers,
                Admin.ReadRoles,
                Admin.ReadPermissions,
                Admin.ReadSettings,
            ];
        }

        public static class Definitions
        {
            public static class Me
            {

                // ==================== Users (1001-2000) ====================
                public static readonly Permission Read = Permission.Create(1001, Catalogs.Me.Read);
                //Profile management permissions (1051-1100)
                public static readonly Permission ReadProfile = Permission.Create(1051, Catalogs.Me.ReadProfile);
                public static readonly Permission UpdateProfile = Permission.Create(1100, Catalogs.Me.UpdateProfile);
                // Password management permissions (1101-1150)
                public static readonly Permission ChangePassword = Permission.Create(1101, Catalogs.Me.ChangePassword);
                // Two-factor authentication permissions (1151-1200)
                public static readonly Permission ManageTwoFactor = Permission.Create(1151, Catalogs.Me.ManageTwoFactor);
                // Session management permissions (1201-1250)
                public static readonly Permission ReadSessions = Permission.Create(1201, Catalogs.Me.ReadSessions);
                public static readonly Permission ReadLoginHistory = Permission.Create(1202, Catalogs.Me.ReadLoginHistory);
                // Phone management permissions (1251-1300)
                public static readonly Permission ManagePhone = Permission.Create(1251, Catalogs.Me.ManagePhone);
                // Address management permissions (1301-1350)
                public static readonly Permission ReadAddress = Permission.Create(1301, Catalogs.Me.ReadAddress);
                public static readonly Permission ManageAddress = Permission.Create(1302, Catalogs.Me.ManageAddress);
                
                // Permissions related to the "Me" section of the application, which includes user-specific data and actions (1351 - 1400)
                public static readonly Permission ReadAuctions = Permission.Create(6001, Catalogs.Me.ReadAuctions);
                public static readonly Permission ReadBids = Permission.Create(6002, Catalogs.Me.ReadBids);
                public static readonly Permission ReadWatchlist = Permission.Create(6003, Catalogs.Me.ReadWatchlist);
            }

            public static class Admin
            {
                // ==================== Admin (2001-3000) ====================
                // User management permissions (2001-2050)
                public static readonly Permission ReadUsers = Permission.Create(2001, Catalogs.Admin.ReadUsers);
                public static readonly Permission ManageUsers = Permission.Create(2002, Catalogs.Admin.ManageUsers);
                public static readonly Permission DeleteUsers = Permission.Create(2003, Catalogs.Admin.DeleteUsers);
                // Role management permissions (2051-2100)
                public static readonly Permission ReadRoles = Permission.Create(2051, Catalogs.Admin.ReadRoles);
                public static readonly Permission AssignRole = Permission.Create(2052, Catalogs.Admin.AssignRole);
                public static readonly Permission RevokeRole = Permission.Create(2053, Catalogs.Admin.RevokeRole);
                // Permissions management permissions (2101-2150)
                public static readonly Permission ReadPermissions = Permission.Create(2101, Catalogs.Admin.ReadPermissions);
                public static readonly Permission GrantPermission = Permission.Create(2102, Catalogs.Admin.GrantPermission);
                public static readonly Permission RevokePermission = Permission.Create(2103, Catalogs.Admin.RevokePermission);
                public static readonly Permission DenyPermission = Permission.Create(2104, Catalogs.Admin.DenyPermission);
                public static readonly Permission ManagePermissions = Permission.Create(2105, Catalogs.Admin.ManagePermissions);
                // Settings management permissions (2151 - 2200)
                public static readonly Permission ReadSettings = Permission.Create(2151, Catalogs.Admin.ReadSettings);
                public static readonly Permission ManageSettings = Permission.Create(2152, Catalogs.Admin.ManageSettings);
            }

            public static class Items
            {
                // ==================== Item (3001-4000) ====================
                //Item management permissions (3001 - 3050)
                public static readonly Permission Create = Permission.Create(3001, Catalogs.Items.Create);
                public static readonly Permission Activate = Permission.Create(3002, Catalogs.Items.Activate);
                public static readonly Permission ReadMy = Permission.Create(3003, Catalogs.Items.ReadMy);
                // Item Media management permissions for items (3051 - 3100)
                public static readonly Permission ManageMedia = Permission.Create(3051, Catalogs.Items.ManageMedia);
                // Item questions and answers permissions (3101 - 3150)
                public static readonly Permission AskQuestion = Permission.Create(3101, Catalogs.Items.AskQuestion);
                public static readonly Permission AnswerQuestion = Permission.Create(3102, Catalogs.Items.AnswerQuestion);
            }

            public static class Auctions
            {
                // ==================== Auctions (4001-5000) ====================
                // Auction management permissions (4001 - 4050)
                public static readonly Permission Create = Permission.Create(4001, Catalogs.Auctions.Create);
                public static readonly Permission Publish = Permission.Create(4002, Catalogs.Auctions.Publish);
                public static readonly Permission Cancel = Permission.Create(4003, Catalogs.Auctions.Cancel);
                public static readonly Permission BuyNow = Permission.Create(4004, Catalogs.Auctions.BuyNow);
                // Bidding permissions (4051 - 4100)
                public static readonly Permission Bid = Permission.Create(4051, Catalogs.Auctions.Bid);
                public static readonly Permission AutoBid = Permission.Create(4052, Catalogs.Auctions.AutoBid);
                public static readonly Permission ReadAutoBid = Permission.Create(4053, Catalogs.Auctions.ReadAutoBid);
                // Watchlist permissions (4101 - 4150)
                public static readonly Permission Watch = Permission.Create(4101, Catalogs.Auctions.Watch);
                public static readonly Permission Unwatch = Permission.Create(4102, Catalogs.Auctions.Unwatch);
            }

            public static class Categories
            {
                // ==================== Categories (5001-6000) ====================
                // Category management permissions (5001 - 5050)
                public static readonly Permission Create = Permission.Create(5001, Catalogs.Categories.Create);
                public static readonly Permission Update = Permission.Create(5002, Catalogs.Categories.Update);
                
            }
            
            public static class Media
            {
                // ==================== Media (6001-7000) ====================
                // Permissions related to media management, including uploading and confirming media files (7001 - 7050)
                public static readonly Permission ReadContexts = Permission.Create(6001, Catalogs.Media.ReadContexts);
                public static readonly Permission Upload = Permission.Create(6002, Catalogs.Media.Upload);
                public static readonly Permission ConfirmUpload = Permission.Create(6003, Catalogs.Media.ConfirmUpload);
            }
            public static IReadOnlyList<Permission> All =>
            [
                // Users
                Me.Read, 
                Me.ReadProfile,
                Me.UpdateProfile,
                Me.ChangePassword,
                Me.ReadSessions, 
                Me.ReadLoginHistory,
                Me.ManageTwoFactor,
                Me.ManagePhone, 
                Me.ReadAddress,
                Me.ManageAddress,
                Me.ReadAuctions,
                Me.ReadBids,
                Me.ReadWatchlist,

                // Items
                Items.Create, 
                Items.Activate,
                Items.ReadMy,
                Items.ManageMedia,
                Items.AskQuestion,
                Items.AnswerQuestion,

                // Auctions
                Auctions.Create,
                Auctions.Publish, 
                Auctions.Cancel,
                Auctions.Bid,
                Auctions.BuyNow,
                Auctions.AutoBid,
                Auctions.ReadAutoBid,
                Auctions.Watch,
                Auctions.Unwatch,

                // Media
                Media.ReadContexts,
                Media.Upload,
                Media.ConfirmUpload,

                // Categories
                Categories.Create,
                Categories.Update,

                // Admin
                Admin.ReadUsers,
                Admin.ManageUsers,
                Admin.DeleteUsers,
                Admin.ReadRoles,
                Admin.AssignRole,
                Admin.RevokeRole,
                Admin.ReadPermissions,
                Admin.GrantPermission,
                Admin.RevokePermission,
                Admin.DenyPermission, 
                Admin.ManagePermissions,
                Admin.ReadSettings,
                Admin.ManageSettings,
            ];
        }
    }
}
