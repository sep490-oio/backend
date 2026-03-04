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
        }
    }

    
}

