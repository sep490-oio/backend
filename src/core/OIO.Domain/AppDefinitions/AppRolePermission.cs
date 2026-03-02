using OIO.Domain.Context.UserContext.Aggregates.Roles;

namespace OIO.Domain.AppDefinitions;

public static partial class App
{
    public static class RolePermissions
    {
        public static readonly Func<RolePermission[]> User = () =>
        {
            return
                [
                    Create(Permissions.Definitions.Users.ReadMe),
                    Create(Permissions.Definitions.Users.UpdateMe),
                    Create(Permissions.Definitions.Users.ReadAddresses),
                    Create(Permissions.Definitions.Users.AddAddresses),
                    Create(Permissions.Definitions.Users.UpdateAddresses),
                    Create(Permissions.Definitions.Users.RemoveAddresses),
                    Create(Permissions.Definitions.Users.SetDefaultAddresses),
                    Create(Permissions.Definitions.Users.ChangePassword),
                    Create(Permissions.Definitions.Users.EnableTwoFactor),
                    Create(Permissions.Definitions.Users.DisableTwoFactor),
                    Create(Permissions.Definitions.Users.SetPhone),
                    Create(Permissions.Definitions.Users.ConfirmPhone),
                    Create(Permissions.Definitions.Users.ReadSessions),
                    Create(Permissions.Definitions.Users.ReadLoginHistory),
                ];

            RolePermission Create(Permission permission)
            {
                return new RolePermission(Roles.Definitions.User, permission);
            }
        };


        public static readonly Func<RolePermission[]> Admin = () =>
        {
            return Permissions.Definitions.All
                .Select(permission => new RolePermission(Roles.Definitions.Admin, permission))
                .ToArray();
        };

        public static readonly RolePermission[] All =
        [
            ..User(),
            ..Admin(),
        ];
        // // ----- Moderator -----
        // var moderator = FindRole("Moderator");
        // Assign(moderator,
        //     "users.read",
        //     "users.change_status",
        //     "users.unlock",
        //     "sellers.read",
        //     "sellers.verify",
        //     "sellers.reject",
        //     "sellers.kyc_review",
        //     "auctions.read",
        //     "auctions.cancel",
        //     "auctions.feature",
        //     "items.read",
        //     "orders.read",
        //     "disputes.read",
        //     "disputes.manage",
        //     "disputes.resolve",
        //     "disputes.escalate",
        //     "reviews.read",
        //     "reviews.moderate",
        //     "reviews.delete",
        //     "admin.dashboard",
        //     "admin.reports");
        //
        // // ----- Support -----
        // var support = FindRole("Support");
        // Assign(support,
        //     "users.read",
        //     "users.unlock",
        //     "sellers.read",
        //     "auctions.read",
        //     "items.read",
        //     "orders.read",
        //     "disputes.read",
        //     "disputes.manage",
        //     "disputes.resolve",
        //     "reviews.read",
        //     "reviews.moderate",
        //     "notifications.send");
        //
        // // ----- Seller -----
        // var seller = FindRole("Seller");
        // Assign(seller,
        //     "items.create",
        //     "items.read",
        //     "items.update",
        //     "items.delete",
        //     "auctions.create",
        //     "auctions.read",
        //     "auctions.update",
        //     "orders.read_own",
        //     "orders.update",
        //     "disputes.read_own",
        //     "disputes.create",
        //     "reviews.read",
        //     "notifications.read",
        //     "wallets.read");
        //
        // // ----- Buyer -----
        // var buyer = FindRole("Buyer");
        // Assign(buyer,
        //     "auctions.read",
        //     "auctions.bid",
        //     "items.read",
        //     "orders.read_own",
        //     "orders.cancel",
        //     "disputes.create",
        //     "disputes.read_own",
        //     "reviews.create",
        //     "reviews.read",
        //     "notifications.read",
        //     "wallets.read");
    }
}