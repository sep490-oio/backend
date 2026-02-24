namespace OIO.Domain.Constants;

public static class AppRole
{
    public const string Admin = "admin";
    public const string Seller = "seller";
    public const string Bidder = "bidder";
    public const string Moderator = "moderator";
    public const string Support = "upport";

    public static readonly string[] All =
    [
        Admin,
        Seller,
        Bidder,
        Moderator,
        Support
    ];
}

