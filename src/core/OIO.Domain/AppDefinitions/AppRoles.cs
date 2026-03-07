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

            public static readonly string[] All =
            [
                Admin,
                Seller,
                Bidder
            ];
        }

        public static class Definitions
        {
            public static readonly Role User = Role.Create(RoleId.From(1), Catalogs.User, 50);
            public static readonly Role Admin = Role.Create(RoleId.From(2), Catalogs.Admin, 100);
            public static readonly Role Seller = Role.Create(RoleId.From(3), Catalogs.Seller, 50);
            public static readonly Role Bidder = Role.Create(RoleId.From(4), Catalogs.Bidder, 50);

            public static readonly Role[] All =
            [
                User,
                Admin,
                Seller,
                Bidder
            ];

            public static readonly IReadOnlyList<Permission> UserPermissions =
            [
                // Profile
                Permissions.Definitions.Me.Read,
                Permissions.Definitions.Me.ReadProfile,
                Permissions.Definitions.Me.UpdateProfile,
                Permissions.Definitions.Me.ChangePassword,
                Permissions.Definitions.Me.ReadSessions,
                Permissions.Definitions.Me.ReadLoginHistory,
                Permissions.Definitions.Me.ManageTwoFactor,
                Permissions.Definitions.Me.ManagePhone,
                Permissions.Definitions.Me.ReadAddress,
                Permissions.Definitions.Me.ManageAddress,

                // Items (read + ask question)
                Permissions.Definitions.Items.AskQuestion,

                // Auctions (watch only)
                Permissions.Definitions.Auctions.Watch,
                Permissions.Definitions.Auctions.Unwatch,

                // Media (upload for avatar, etc.)
                Permissions.Definitions.Media.ReadContexts,
                Permissions.Definitions.Media.Upload,
                Permissions.Definitions.Media.ConfirmUpload,

                // Me
                Permissions.Definitions.Me.ReadWatchlist,
            ];
            
            public static IReadOnlyDictionary<Role, IReadOnlyList<Permission>> RolePermissions => new Dictionary<Role, IReadOnlyList<Permission>>
            {
                [User] = UserPermissions,

                [Bidder] = UserPermissions.Concat([
                    // Bidding
                    Permissions.Definitions.Auctions.Bid,
                    Permissions.Definitions.Auctions.BuyNow,
                    Permissions.Definitions.Auctions.AutoBid,
                    Permissions.Definitions.Auctions.ReadAutoBid,

                    // Me
                    Permissions.Definitions.Me.ReadBids,
                ]).ToList(),

                [Seller] = UserPermissions.Concat([
                    // Items
                    Permissions.Definitions.Items.Create,
                    Permissions.Definitions.Items.Activate,
                    Permissions.Definitions.Items.ReadMy,
                    Permissions.Definitions.Items.ManageMedia,
                    Permissions.Definitions.Items.AnswerQuestion,

                    // Auctions
                    Permissions.Definitions.Auctions.Create,
                    Permissions.Definitions.Auctions.Publish,
                    Permissions.Definitions.Auctions.Cancel,

                    // Me
                    Permissions.Definitions.Me.ReadAuctions,
                ]).ToList(),

                [Admin] = Permissions.Definitions.All
                
            };
        }
    }

    
}

