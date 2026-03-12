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
                public const string ReadTerms = "users:me:terms:read";
                public const string AcceptTerms = "users:me:terms:accept";

                // Verifications
                public const string ManageVerification = "users:me:verification:manage";
                public const string ReadVerification = "users:me:verification:read";

                // Seller Profile
                public const string ManageSellerProfile = "users:me:seller-profile:manage";
                public const string ReadSellerProfile = "users:me:seller-profile:read";
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
            
            // ==================== Warehouses ====================
            public static class Warehouse
            {
                public const string BookInbound  = "warehouse:inbound:book";
                public const string BookOutbound = "warehouse:outbound:book";
                public const string Inspect = "warehouse:item:inspect";
                public const string Store   = "warehouse:item:store";
                public const string ManageLocations = "warehouse:locations:manage";
                public const string ReadShipments    = "warehouse:shipments:read";

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
                public const string ReadTerms = "admin:terms:read";
                public const string ManageTerms = "admin:terms:manage";

                // Verifications
                public const string ReadVerifications = "admin:verifications:read";
                public const string ManageVerifications = "admin:verifications:manage";

                // Seller Profiles
                public const string ReadSellerProfiles = "admin:seller-profiles:read";
                public const string ManageSellerProfiles = "admin:seller-profiles:manage";
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
                Me.ReadTerms,
                Me.AcceptTerms,
                Me.ManageVerification,
                Me.ReadVerification,
                Me.ManageSellerProfile,
                Me.ReadSellerProfile,

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
                Admin.ReadTerms,
                Admin.ManageTerms,
                Admin.ReadVerifications,
                Admin.ManageVerifications,
                Admin.ReadSellerProfiles,
                Admin.ManageSellerProfiles,
                Admin.ManageSettings,

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
                
                //Warehouse
                Warehouse.BookInbound,
                Warehouse.BookOutbound,
                Warehouse.Inspect,
                Warehouse.Store,
                Warehouse.ManageLocations,
                Warehouse.ReadShipments,

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
                Admin.ReadTerms,
                Admin.ManageTerms,
                Admin.ReadVerifications,
                Admin.ManageVerifications,
                Admin.ReadSellerProfiles,
                Admin.ManageSellerProfiles,
            ];
        }

        public static class Definitions
        {
            public static class Me
            {

                // ==================== Users (1001-2000) ====================
                public static readonly Permission Read = Permission.Create(Catalogs.Me.Read);
                //Profile management permissions (1051-1100)
                public static readonly Permission ReadProfile = Permission.Create(Catalogs.Me.ReadProfile);
                public static readonly Permission UpdateProfile = Permission.Create(Catalogs.Me.UpdateProfile);
                // Password management permissions (1101-1150)
                public static readonly Permission ChangePassword = Permission.Create(Catalogs.Me.ChangePassword);
                // Two-factor authentication permissions (1151-1200)
                public static readonly Permission ManageTwoFactor = Permission.Create(Catalogs.Me.ManageTwoFactor);
                // Session management permissions (1201-1250)
                public static readonly Permission ReadSessions = Permission.Create(Catalogs.Me.ReadSessions);
                public static readonly Permission ReadLoginHistory = Permission.Create(Catalogs.Me.ReadLoginHistory);
                // Phone management permissions (1251-1300)
                public static readonly Permission ManagePhone = Permission.Create(Catalogs.Me.ManagePhone);
                // Address management permissions (1301-1350)
                public static readonly Permission ReadAddress = Permission.Create(Catalogs.Me.ReadAddress);
                public static readonly Permission ManageAddress = Permission.Create(Catalogs.Me.ManageAddress);
                
                // Permissions related to the "Me" section of the which includes user-specific data and actions (1351 - 1400)
                public static readonly Permission ReadAuctions = Permission.Create(Catalogs.Me.ReadAuctions);
                public static readonly Permission ReadBids = Permission.Create(Catalogs.Me.ReadBids);
                public static readonly Permission ReadWatchlist = Permission.Create(Catalogs.Me.ReadWatchlist);
                public static readonly Permission ReadTerms = Permission.Create(Catalogs.Me.ReadTerms);
                public static readonly Permission AcceptTerms = Permission.Create(Catalogs.Me.AcceptTerms);
                public static readonly Permission ManageVerification = Permission.Create(Catalogs.Me.ManageVerification);
                public static readonly Permission ReadVerification = Permission.Create(Catalogs.Me.ReadVerification);
                public static readonly Permission ManageSellerProfile = Permission.Create(Catalogs.Me.ManageSellerProfile);
                public static readonly Permission ReadSellerProfile = Permission.Create(Catalogs.Me.ReadSellerProfile);
            }

            public static class Admin
            {
                // ==================== Admin (2001-3000) ====================
                // User management permissions (2001-2050)
                public static readonly Permission ReadUsers = Permission.Create(Catalogs.Admin.ReadUsers);
                public static readonly Permission ManageUsers = Permission.Create(Catalogs.Admin.ManageUsers);
                public static readonly Permission DeleteUsers = Permission.Create(Catalogs.Admin.DeleteUsers);
                // Role management permissions (2051-2100)
                public static readonly Permission ReadRoles = Permission.Create(Catalogs.Admin.ReadRoles);
                public static readonly Permission AssignRole = Permission.Create(Catalogs.Admin.AssignRole);
                public static readonly Permission RevokeRole = Permission.Create(Catalogs.Admin.RevokeRole);
                // Permissions management permissions (2101-2150)
                public static readonly Permission ReadPermissions = Permission.Create(Catalogs.Admin.ReadPermissions);
                public static readonly Permission GrantPermission = Permission.Create(Catalogs.Admin.GrantPermission);
                public static readonly Permission RevokePermission = Permission.Create(Catalogs.Admin.RevokePermission);
                public static readonly Permission DenyPermission = Permission.Create(Catalogs.Admin.DenyPermission);
                public static readonly Permission ManagePermissions = Permission.Create(Catalogs.Admin.ManagePermissions);
                // Settings management permissions (2151 - 2200)
                public static readonly Permission ReadSettings = Permission.Create(Catalogs.Admin.ReadSettings);
                public static readonly Permission ManageSettings = Permission.Create(Catalogs.Admin.ManageSettings);
                public static readonly Permission ReadTerms = Permission.Create(Catalogs.Admin.ReadTerms);
                public static readonly Permission ManageTerms = Permission.Create(Catalogs.Admin.ManageTerms);
                public static readonly Permission ReadVerifications = Permission.Create(Catalogs.Admin.ReadVerifications);
                public static readonly Permission ManageVerifications = Permission.Create(Catalogs.Admin.ManageVerifications);
                public static readonly Permission ReadSellerProfiles = Permission.Create(Catalogs.Admin.ReadSellerProfiles);
                public static readonly Permission ManageSellerProfiles = Permission.Create(Catalogs.Admin.ManageSellerProfiles);
            }

            public static class Items
            {
                // ==================== Item (3001-4000) ====================
                //Item management permissions (3001 - 3050)
                public static readonly Permission Create = Permission.Create(Catalogs.Items.Create);
                public static readonly Permission Activate = Permission.Create(Catalogs.Items.Activate);
                public static readonly Permission ReadMy = Permission.Create(Catalogs.Items.ReadMy);
                // Item Media management permissions for items (3051 - 3100)
                public static readonly Permission ManageMedia = Permission.Create(Catalogs.Items.ManageMedia);
                // Item questions and answers permissions (3101 - 3150)
                public static readonly Permission AskQuestion = Permission.Create(Catalogs.Items.AskQuestion);
                public static readonly Permission AnswerQuestion = Permission.Create(Catalogs.Items.AnswerQuestion);
            }

            public static class Auctions
            {
                // ==================== Auctions (4001-5000) ====================
                // Auction management permissions (4001 - 4050)
                public static readonly Permission Create = Permission.Create(Catalogs.Auctions.Create);
                public static readonly Permission Publish = Permission.Create(Catalogs.Auctions.Publish);
                public static readonly Permission Cancel = Permission.Create(Catalogs.Auctions.Cancel);
                public static readonly Permission BuyNow = Permission.Create(Catalogs.Auctions.BuyNow);
                // Bidding permissions (4051 - 4100)
                public static readonly Permission Bid = Permission.Create(Catalogs.Auctions.Bid);
                public static readonly Permission AutoBid = Permission.Create(Catalogs.Auctions.AutoBid);
                public static readonly Permission ReadAutoBid = Permission.Create(Catalogs.Auctions.ReadAutoBid);
                // Watchlist permissions (4101 - 4150)
                public static readonly Permission Watch = Permission.Create(Catalogs.Auctions.Watch);
                public static readonly Permission Unwatch = Permission.Create(Catalogs.Auctions.Unwatch);
            }

            public static class Warehouse
            {
                // ==================== Warehouse (7001-8000) ====================
                public static readonly Permission BookInbound  = Permission.Create(Catalogs.Warehouse.BookInbound);
                public static readonly Permission BookOutbound = Permission.Create(Catalogs.Warehouse.BookOutbound);
                public static readonly Permission Store   = Permission.Create(Catalogs.Warehouse.Store);
                public static readonly Permission Inspect = Permission.Create(Catalogs.Warehouse.Inspect);
                public static readonly Permission ManageLocations = Permission.Create(Catalogs.Warehouse.ManageLocations);
                public static readonly Permission ReadShipments = Permission.Create(Catalogs.Warehouse.ReadShipments);
            }
            
            public static class Categories
            {
                // ==================== Categories (5001-6000) ====================
                // Category management permissions (5001 - 5050)
                public static readonly Permission Create = Permission.Create(Catalogs.Categories.Create);
                public static readonly Permission Update = Permission.Create(Catalogs.Categories.Update);
                
            }
            
            public static class Media
            {
                // ==================== Media (6001-7000) ====================
                // Permissions related to media management, including uploading and confirming media files (7001 - 7050)
                public static readonly Permission ReadContexts = Permission.Create(Catalogs.Media.ReadContexts);
                public static readonly Permission Upload = Permission.Create(Catalogs.Media.Upload);
                public static readonly Permission ConfirmUpload = Permission.Create(Catalogs.Media.ConfirmUpload);
            }
            public static IReadOnlyDictionary<string, Permission> All => new Dictionary<string, Permission>()
            {
                // Users
                [Catalogs.Me.Read] = Me.Read,

                [Catalogs.Me.ReadProfile] = Me.ReadProfile,
                [Catalogs.Me.UpdateProfile] = Me.UpdateProfile,
                [Catalogs.Me.ChangePassword] = Me.ChangePassword,
                [Catalogs.Me.ReadSessions] = Me.ReadSessions,
                [Catalogs.Me.ReadLoginHistory] = Me.ReadLoginHistory,
                [Catalogs.Me.ManageTwoFactor] = Me.ManageTwoFactor,
                [Catalogs.Me.ManagePhone] = Me.ManagePhone,
                [Catalogs.Me.ReadAddress] = Me.ReadAddress,
                [Catalogs.Me.ManageAddress] = Me.ManageAddress,
                [Catalogs.Me.ReadAuctions] = Me.ReadAuctions,
                [Catalogs.Me.ReadBids] = Me.ReadBids,
                [Catalogs.Me.ReadWatchlist] = Me.ReadWatchlist,
                [Catalogs.Me.ReadTerms] = Me.ReadTerms,
                [Catalogs.Me.AcceptTerms] = Me.AcceptTerms,
                [Catalogs.Me.ManageVerification] = Me.ManageVerification,
                [Catalogs.Me.ReadVerification] = Me.ReadVerification,
                [Catalogs.Me.ManageSellerProfile] = Me.ManageSellerProfile,
                [Catalogs.Me.ReadSellerProfile] = Me.ReadSellerProfile,

                // Items
                [Catalogs.Items.Create] = Items.Create,
                [Catalogs.Items.Activate] = Items.Activate,
                [Catalogs.Items.ReadMy] = Items.ReadMy,
                [Catalogs.Items.ManageMedia] = Items.ManageMedia,
                [Catalogs.Items.AskQuestion] = Items.AskQuestion,
                [Catalogs.Items.AnswerQuestion] = Items.AnswerQuestion,

                // Auctions
                [Catalogs.Auctions.Create] = Auctions.Create,
                [Catalogs.Auctions.Publish] = Auctions.Publish,
                [Catalogs.Auctions.Cancel] = Auctions.Cancel,
                [Catalogs.Auctions.Bid] = Auctions.Bid,
                [Catalogs.Auctions.BuyNow] = Auctions.BuyNow,
                [Catalogs.Auctions.AutoBid] = Auctions.AutoBid,
                [Catalogs.Auctions.ReadAutoBid] = Auctions.ReadAutoBid,
                [Catalogs.Auctions.Watch] = Auctions.Watch,
                [Catalogs.Auctions.Unwatch] = Auctions.Unwatch,

                //Warehouse
                [Catalogs.Warehouse.BookInbound] = Warehouse.BookInbound,
                [Catalogs.Warehouse.BookOutbound] = Warehouse.BookOutbound,
                [Catalogs.Warehouse.Inspect] = Warehouse.Inspect,
                [Catalogs.Warehouse.Store] = Warehouse.Store,
                [Catalogs.Warehouse.ManageLocations] = Warehouse.ManageLocations,
                [Catalogs.Warehouse.ReadShipments] = Warehouse.ReadShipments,
                
                // Media
                [Catalogs.Media.ReadContexts] = Media.ReadContexts,
                [Catalogs.Media.Upload] = Media.Upload,
                [Catalogs.Media.ConfirmUpload] = Media.ConfirmUpload,

                // Categories
                [Catalogs.Categories.Create] = Categories.Create,
                [Catalogs.Categories.Update] = Categories.Update,

                // Admin
                [Catalogs.Admin.ReadUsers] = Admin.ReadUsers,
                [Catalogs.Admin.ManageUsers] = Admin.ManageUsers,
                [Catalogs.Admin.DeleteUsers] = Admin.DeleteUsers,
                [Catalogs.Admin.ReadRoles] = Admin.ReadRoles,
                [Catalogs.Admin.AssignRole] = Admin.AssignRole,
                [Catalogs.Admin.RevokeRole] = Admin.RevokeRole,
                [Catalogs.Admin.ReadPermissions] = Admin.ReadPermissions,
                [Catalogs.Admin.GrantPermission] = Admin.GrantPermission,
                [Catalogs.Admin.RevokePermission] = Admin.RevokePermission,
                [Catalogs.Admin.DenyPermission] = Admin.DenyPermission,
                [Catalogs.Admin.ManagePermissions] = Admin.ManagePermissions,
                [Catalogs.Admin.ReadSettings] = Admin.ReadSettings,
                [Catalogs.Admin.ManageSettings] = Admin.ManageSettings,
                [Catalogs.Admin.ReadTerms] = Admin.ReadTerms,
                [Catalogs.Admin.ManageTerms] = Admin.ManageTerms,
                [Catalogs.Admin.ReadVerifications] = Admin.ReadVerifications,
                [Catalogs.Admin.ManageVerifications] = Admin.ManageVerifications,
                [Catalogs.Admin.ReadSellerProfiles] = Admin.ReadSellerProfiles,
                [Catalogs.Admin.ManageSellerProfiles] = Admin.ManageSellerProfiles,
            };
        }
    }
}
