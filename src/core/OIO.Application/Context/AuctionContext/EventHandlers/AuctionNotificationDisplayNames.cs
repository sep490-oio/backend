using OIO.Domain.Context.UserContext.Aggregates.Users;

namespace OIO.Application.Context.AuctionContext.EventHandlers;

internal static class AuctionNotificationDisplayNames
{
    public static string Resolve(User? user)
    {
        var displayName = user?.Profile?.Name?.DisplayName;
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            return displayName;
        }

        var fullName = user?.Profile?.Name?.FullName;
        if (!string.IsNullOrWhiteSpace(fullName))
        {
            return fullName;
        }

        return user?.UserName.Value ?? string.Empty;
    }
}
