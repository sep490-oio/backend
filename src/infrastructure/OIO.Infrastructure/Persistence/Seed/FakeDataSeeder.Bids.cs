using Bogus;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.Context.UserContext.Aggregates.Users;

namespace OIO.Infrastructure.Persistence.Seed;

public static partial class FakeDataSeeder
{
    private static void SeedBids(
        ApplicationDbContext dbContext,
        List<Auction> auctions,
        List<User> users, 
        DateTime nowUtc)
    {
        var bidders = users
            .Where(u => u.UserName.Value.StartsWith("bidder"))
            .ToList();

        var faker = new Faker("vi");

        var activeAuctions = auctions
            .Where(a => a.Status == AuctionStatus.Active)
            .ToList();

        foreach (var auction in activeAuctions)
        {
            var bidCount = faker.Random.Int(0, 15);
            var currentPrice = auction.StartingPrice.Amount;

            // Pick random subset of bidders
            var auctionBidders = faker.PickRandom(bidders,
                    faker.Random.Int(2, Math.Min(6, bidders.Count)))
                .Where(b => b.Id != auction.SellerId)
                .ToList();

            if (auctionBidders.Count == 0) continue;

            for (var i = 0; i < bidCount; i++)
            {
                var bidder = faker.PickRandom(auctionBidders);
                var bidAmount = currentPrice + auction.BidIncrement.Amount;

                // Occasionally bid higher than minimum
                if (faker.Random.Bool(0.3f))
                {
                    bidAmount += auction.BidIncrement.Amount *
                                 faker.Random.Int(1, 5);
                }

                bidAmount = Math.Round(bidAmount / 10_000) * 10_000;

                try
                {
                    auction.PlaceBid(bidder.Id, Money.Create(bidAmount, "VND").Value, nowUtc, TimeSpan.FromMinutes(5), 10, TimeSpan.FromMinutes(180) );
                    currentPrice = bidAmount;
                }
                catch
                {
                    // Skip invalid bids (own auction, etc.)
                }
            }
        }
    }
}