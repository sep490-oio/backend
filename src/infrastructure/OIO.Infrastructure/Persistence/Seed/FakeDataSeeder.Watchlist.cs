using Bogus;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.UserContext.Aggregates.Users;

namespace OIO.Infrastructure.Persistence.Seed;

public static partial class FakeDataSeeder
{
    private static List<AuctionWatcher> SeedWatchlist(
        List<Auction> auctions,
        List<User> users,
        DateTime nowUtc)
    {
        var bidders = users
            .Where(u => u.UserName.Value.StartsWith("bidder"))
            .ToList();

        var faker = new Faker("vi");
        var watchers = new List<AuctionWatcher>();

        var activeAuctions = auctions
            .Where(a => a.Status == AuctionStatus.Active ||
                        a.Status == AuctionStatus.Pending)
            .ToList();

        foreach (var bidder in bidders)
        {
            // Each bidder watches 2-6 auctions
            var watchCount = faker.Random.Int(2, Math.Min(6, activeAuctions.Count));
            var toWatch = faker.PickRandom(activeAuctions, watchCount)
                .Where(a => a.SellerId != bidder.Id);

            foreach (var auction in toWatch)
            {
                var watcher = AuctionWatcher.Create(auction.Id, bidder.Id, nowUtc);
                watchers.Add(watcher);
            }
        }

        return watchers;
    }
}