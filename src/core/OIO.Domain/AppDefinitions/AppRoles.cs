using OIO.Domain.Context.UserContext.Aggregates.Roles;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Domain.AppDefinitions;

public static partial class App
{
    public static class Roles
    {
        public static class Catalogs
        {
            public const string User = "user";
            public const string Admin = "admin";
            public const string Seller = "seller";
            public const string Bidder = "bidder";
            public const string Inspector = "inspector";

            public static readonly HashSet<string> All =
            [
                User,
                Admin,
                Seller,
                Bidder,
                Inspector
            ];
            
            public static IReadOnlyDictionary<string, HashSet<string>> RolePermissions => new Dictionary<string, HashSet<string>>
            {
                [User] = 
                [
                    // Profile
                    Permissions.Catalogs.Me.Read,
                    Permissions.Catalogs.Me.ReadProfile,
                    Permissions.Catalogs.Me.UpdateProfile,
                    Permissions.Catalogs.Me.ChangePassword,
                    Permissions.Catalogs.Me.ReadSessions,
                    Permissions.Catalogs.Me.ReadLoginHistory,
                    Permissions.Catalogs.Me.ManageTwoFactor,
                    Permissions.Catalogs.Me.ManagePhone,
                    Permissions.Catalogs.Me.ReadAddress,
                    Permissions.Catalogs.Me.ManageAddress,

                    // Items (read + ask question)
                    Permissions.Catalogs.Items.AskQuestion,

                    // Auctions (watch only)
                    Permissions.Catalogs.Auctions.Watch,
                    Permissions.Catalogs.Auctions.Unwatch,

                    // Media (upload for avatar, etc.)
                    Permissions.Catalogs.Media.ReadContexts,
                    Permissions.Catalogs.Media.Upload,
                    Permissions.Catalogs.Media.ConfirmUpload,

                    // Me
                    Permissions.Catalogs.Me.ReadWatchlist,
                    Permissions.Catalogs.Me.ReadTerms,
                    Permissions.Catalogs.Me.AcceptTerms,

                    // Verification & Seller Profile
                    Permissions.Catalogs.Me.ManageVerification,
                    Permissions.Catalogs.Me.ReadVerification,
                    Permissions.Catalogs.Me.ManageSellerProfile,
                    Permissions.Catalogs.Me.ReadSellerProfile,

                    // Notification Preferences
                    Permissions.Catalogs.Me.ReadNotificationPreferences,
                    Permissions.Catalogs.Me.ManageNotificationPreferences,
                ],

                [Bidder] =
                [
                    // Profile
                    Permissions.Catalogs.Me.Read,
                    Permissions.Catalogs.Me.ReadProfile,
                    Permissions.Catalogs.Me.UpdateProfile,
                    Permissions.Catalogs.Me.ChangePassword,
                    Permissions.Catalogs.Me.ReadSessions,
                    Permissions.Catalogs.Me.ReadLoginHistory,
                    Permissions.Catalogs.Me.ManageTwoFactor,
                    Permissions.Catalogs.Me.ManagePhone,
                    Permissions.Catalogs.Me.ReadAddress,
                    Permissions.Catalogs.Me.ManageAddress,

                    // Items (read + ask question)
                    Permissions.Catalogs.Items.AskQuestion,

                    // Auctions (watch only)
                    Permissions.Catalogs.Auctions.Watch,
                    Permissions.Catalogs.Auctions.Unwatch,

                    // Media (upload for avatar, etc.)
                    Permissions.Catalogs.Media.ReadContexts,
                    Permissions.Catalogs.Media.Upload,
                    Permissions.Catalogs.Media.ConfirmUpload,

                    // Me
                    Permissions.Catalogs.Me.ReadWatchlist,
                    Permissions.Catalogs.Me.ReadTerms,
                    Permissions.Catalogs.Me.AcceptTerms,

                    // Bidding
                    Permissions.Catalogs.Auctions.Bid,
                    Permissions.Catalogs.Auctions.BuyNow,
                    Permissions.Catalogs.Auctions.AutoBid,
                    Permissions.Catalogs.Auctions.ReadAutoBid,
                    Permissions.Catalogs.Me.ReadBids,

                    // Verification & Seller Profile
                    Permissions.Catalogs.Me.ManageVerification,
                    Permissions.Catalogs.Me.ReadVerification,
                    Permissions.Catalogs.Me.ManageSellerProfile,
                    Permissions.Catalogs.Me.ReadSellerProfile,

                    // Notification Preferences
                    Permissions.Catalogs.Me.ReadNotificationPreferences,
                    Permissions.Catalogs.Me.ManageNotificationPreferences,

                    // Warehouse
                    Permissions.Catalogs.Warehouse.ReadShipments,
                    Permissions.Catalogs.Warehouse.CalculateShippingFee,
                    Permissions.Catalogs.Warehouse.CalculateLeadTime,
                ],

                [Seller] =
                [
                    // Profile
                    Permissions.Catalogs.Me.Read,
                    Permissions.Catalogs.Me.ReadProfile,
                    Permissions.Catalogs.Me.UpdateProfile,
                    Permissions.Catalogs.Me.ChangePassword,
                    Permissions.Catalogs.Me.ReadSessions,
                    Permissions.Catalogs.Me.ReadLoginHistory,
                    Permissions.Catalogs.Me.ManageTwoFactor,
                    Permissions.Catalogs.Me.ManagePhone,
                    Permissions.Catalogs.Me.ReadAddress,
                    Permissions.Catalogs.Me.ManageAddress,

                    // Items (read + ask question)
                    Permissions.Catalogs.Items.AskQuestion,

                    // Auctions (watch only)
                    Permissions.Catalogs.Auctions.Watch,
                    Permissions.Catalogs.Auctions.Unwatch,

                    // Media (upload for avatar, etc.)
                    Permissions.Catalogs.Media.ReadContexts,
                    Permissions.Catalogs.Media.Upload,
                    Permissions.Catalogs.Media.ConfirmUpload,

                    // Me
                    Permissions.Catalogs.Me.ReadWatchlist,
                    Permissions.Catalogs.Me.ReadTerms,
                    Permissions.Catalogs.Me.AcceptTerms,

                    // Items
                    Permissions.Catalogs.Items.Create,
                    Permissions.Catalogs.Items.Activate,
                    Permissions.Catalogs.Items.Resubmit,
                    Permissions.Catalogs.Items.ReadMy,
                    Permissions.Catalogs.Items.ManageMedia,
                    Permissions.Catalogs.Items.AnswerQuestion,

                    // Auctions
                    Permissions.Catalogs.Auctions.Create,
                    Permissions.Catalogs.Auctions.Publish,
                    Permissions.Catalogs.Auctions.Submit,
                    Permissions.Catalogs.Auctions.Cancel,
                    Permissions.Catalogs.Me.ReadAuctions,

                    // Verification & Seller Profile
                    Permissions.Catalogs.Me.ReadVerification,
                    Permissions.Catalogs.Me.ManageSellerProfile,
                    Permissions.Catalogs.Me.ReadSellerProfile,

                    // Notification Preferences
                    Permissions.Catalogs.Me.ReadNotificationPreferences,
                    Permissions.Catalogs.Me.ManageNotificationPreferences,

                    Permissions.Catalogs.Warehouse.ReadShipments,
                    Permissions.Catalogs.Warehouse.BookInbound,
                    Permissions.Catalogs.Warehouse.SelfShipOutbound,
                    Permissions.Catalogs.Warehouse.CalculateShippingFee,
                    Permissions.Catalogs.Warehouse.CalculateLeadTime,
                ],

                [Inspector] =
                [
                    Permissions.Catalogs.Me.Read,
                    Permissions.Catalogs.Me.ReadProfile,
                    Permissions.Catalogs.Me.UpdateProfile,
                    Permissions.Catalogs.Me.ChangePassword,
                    Permissions.Catalogs.Me.ReadSessions,
                    Permissions.Catalogs.Me.ReadLoginHistory,
                    Permissions.Catalogs.Me.ManageTwoFactor,
                    Permissions.Catalogs.Me.ManagePhone,
                    Permissions.Catalogs.Media.ReadContexts,
                    Permissions.Catalogs.Media.Upload,
                    Permissions.Catalogs.Media.ConfirmUpload,
                    Permissions.Catalogs.Warehouse.ReadShipments,
                    Permissions.Catalogs.Warehouse.Inspect,
                    Permissions.Catalogs.Warehouse.Store,
                    Permissions.Catalogs.Warehouse.BookInbound,
                    Permissions.Catalogs.Warehouse.BookOutbound,
                    Permissions.Catalogs.Warehouse.UpdateExternalStatus,
                    Permissions.Catalogs.Warehouse.ManageLocations,
                    Permissions.Catalogs.Warehouse.CalculateShippingFee,
                    Permissions.Catalogs.Warehouse.CalculateLeadTime,
                ],

                [Admin] = Permissions.Catalogs.All
                
            };
        }

        public static class Definitions
        {
            public static readonly Role User = Role.Create(Catalogs.User, 50);
            public static readonly Role Admin = Role.Create(Catalogs.Admin, 100);
            public static readonly Role Seller = Role.Create(Catalogs.Seller, 50);
            public static readonly Role Bidder = Role.Create(Catalogs.Bidder, 50);
            public static readonly Role Inspector = Role.Create(Catalogs.Inspector, 60);

            public static readonly IReadOnlyDictionary<string, Role> All = new Dictionary<string, Role>
            {
                [Catalogs.User] = User,
                [Catalogs.Admin] = Admin,
                [Catalogs.Seller] = Seller,
                [Catalogs.Bidder] = Bidder,
                [Catalogs.Inspector] = Inspector
            };
        }
    }

    
}

