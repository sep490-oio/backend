using OIO.Domain.Context.UserContext.Aggregates.Roles;

namespace OIO.Domain.AppDefinitions;

public static partial class App
{
    public static class RolePermissions
    {
        // ==================== USER ====================
        // Base role — profile management, view public data
        private static readonly IReadOnlyList<Permission> UserPermissions =
        [
            // Profile
            Permissions.Definitions.Users.ReadMe,
            Permissions.Definitions.Users.UpdateMe,
            Permissions.Definitions.Users.ReadProfile,
            Permissions.Definitions.Users.UpdateProfile,

            // Addresses
            Permissions.Definitions.Users.ReadAddresses,
            Permissions.Definitions.Users.AddAddresses,
            Permissions.Definitions.Users.UpdateAddresses,
            Permissions.Definitions.Users.RemoveAddresses,
            Permissions.Definitions.Users.SetDefaultAddresses,

            // Security
            Permissions.Definitions.Users.ChangePassword,
            Permissions.Definitions.Users.EnableTwoFactor,
            Permissions.Definitions.Users.DisableTwoFactor,
            Permissions.Definitions.Users.SetPhone,
            Permissions.Definitions.Users.ConfirmPhone,

            // Sessions
            Permissions.Definitions.Users.ReadSessions,
            Permissions.Definitions.Users.ReadLoginHistory,
            Permissions.Definitions.Users.Logout,

            // Public read
            Permissions.Definitions.Categories.ReadAll,
            Permissions.Definitions.Categories.ReadChildren,
            Permissions.Definitions.Items.ReadQuestions,
            Permissions.Definitions.Auctions.ReadBids,
        ];
        
        // ==================== BIDDER ====================
        // Inherits User + bidding capabilities
        private static readonly IReadOnlyList<Permission> BidderPermissions =
        [
            // All User permissions
            ..UserPermissions,

            // Bidding
            Permissions.Definitions.Auctions.PlaceBid,
            Permissions.Definitions.Auctions.BuyNow,
            Permissions.Definitions.Auctions.ConfigureAutoBid,
            Permissions.Definitions.Auctions.PauseAutoBid,
            Permissions.Definitions.Auctions.ResumeAutoBid,

            // Watch
            Permissions.Definitions.Auctions.Watch,
            Permissions.Definitions.Auctions.Unwatch,

            // Ask questions about items
            Permissions.Definitions.Items.AskQuestion,
        ];

        // ==================== SELLER ====================
        // Inherits User + selling capabilities
        private static readonly IReadOnlyList<Permission> SellerPermissions =
        [
            // All User permissions
            ..UserPermissions,

            // Items
            Permissions.Definitions.Items.Create,
            Permissions.Definitions.Items.ReadMy,
            Permissions.Definitions.Items.Activate,
            Permissions.Definitions.Items.AddMedia,
            Permissions.Definitions.Items.RemoveMedia,
            Permissions.Definitions.Items.AnswerQuestion,

            // Auctions
            Permissions.Definitions.Auctions.Create,
            Permissions.Definitions.Auctions.Publish,
            Permissions.Definitions.Auctions.Cancel,

            // Watch (seller can watch other auctions too)
            Permissions.Definitions.Auctions.Watch,
            Permissions.Definitions.Auctions.Unwatch,
        ];

        // ==================== ADMIN ====================
        // ALL permissions
        private static readonly IReadOnlyList<Permission> AdminPermissions =
        [
            ..Permissions.Definitions.All,
        ];
        
        public static readonly Func<RolePermission[]> User = () =>
        {
            return UserPermissions.Select(x => new RolePermission(Roles.Definitions.User, x))
                .ToArray();
        };
        
        public static readonly Func<RolePermission[]> Admin = () =>
        {
            return AdminPermissions
                .Select(permission => new RolePermission(Roles.Definitions.Admin, permission))
                .ToArray();
        };

        public static readonly Func<RolePermission[]> Bidder = () =>
        {
            return AdminPermissions
                .Select(permission => new RolePermission(Roles.Definitions.Bidder, permission))
                .ToArray();
        };

        public static readonly Func<RolePermission[]> Seller = () =>
        {
            return AdminPermissions
                .Select(permission => new RolePermission(Roles.Definitions.Seller, permission))
                .ToArray();
        };

        public static readonly RolePermission[] All =
        [
            ..User(),
            ..Admin(),
            ..Bidder(),
            ..Seller(),
        ];
    }
}