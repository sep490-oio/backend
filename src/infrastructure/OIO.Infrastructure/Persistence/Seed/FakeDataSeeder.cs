using System.Text.Json;
using Bogus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Settings;
using OIO.Domain.AppDefinitions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Aggregates.Categories;
using OIO.Domain.Context.AuctionContext.Aggregates.Items;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects;
using OIO.Domain.Context.Shared.Entities;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.Context.Shared.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.Aggregates.Roles;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Services;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Infrastructure.Settings;
using OIO.Infrastructure.Settings.Apps;

namespace OIO.Infrastructure.Persistence.Seed;

public static partial class FakeDataSeeder
{
    private const string DefaultPassword = "Password123!";
    private static readonly Faker Faker = new("vi");

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var appConfigs = scope.ServiceProvider.GetRequiredService<IAppConfigs>();

        

        logger.LogInformation("Seeding fake data...");
        var strategy = dbContext.Database.CreateExecutionStrategy();
        
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await dbContext.Database.BeginTransactionAsync();

            // Skip if already seeded
            if (await dbContext.Set<User>().AnyAsync())
            {
                logger.LogInformation("Database already seeded. Skipping.");
                return;
            }
            
            var nowUtc = clock.UtcNow;
            await SeedAdminUserAsync(dbContext, services, logger);
            // 1. Users
            var users = SeedUsers(passwordHasher, nowUtc);
            dbContext.Set<User>().AddRange(users);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Seeded {Count} users.", users.Count);

            // 2. Categories
            var categories = SeedCategories(nowUtc);
            dbContext.Set<Category>().AddRange(categories);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Seeded {Count} categories.", categories.Count);

            // 3. Items
            var items = SeedItems(users, categories, nowUtc);
            SeedQuestions(items, users, await appConfigs.Items.GetMaxQuestionsPerItemAsync(), nowUtc);
            dbContext.Set<Item>().AddRange(items);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("✅ Seeded {Count} items.", items.Count);

            // 4. Auctions
            var auctions = SeedAuctions(items, nowUtc);
            SeedBids(dbContext, auctions, users, nowUtc);
            dbContext.Set<Auction>().AddRange(auctions);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("✅ Seeded {Count} auctions.", auctions.Count);

            // 5. Watchlist
            var watchers = SeedWatchlist(auctions, users, nowUtc);
            dbContext.Set<AuctionWatcher>().AddRange(watchers);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("✅ Seeded {Count} watchers.", watchers.Count);
            
            logger.LogInformation("🎉 Fake data seeding complete!");
            logger.LogInformation("📋 Login credentials: any seeded email + password: {Password}", DefaultPassword);
            logger.LogInformation("Fake data seeding complete.");
            await tx.CommitAsync();
        });
        
    }

    // ==================== Users ====================
    
    private static async Task SeedAdminUserAsync(
        ApplicationDbContext dbContext,
        IServiceProvider serviceProvider,
        ILogger logger)
    {
        if (await dbContext.Set<User>().AnyAsync(x => x.Email == UserEmail.Create("admin@oio.com").Value))
            return;

        var passwordHasher = serviceProvider.GetRequiredService<IPasswordHasher>();
        var clock = serviceProvider.GetRequiredService<IClock>();
        var efaultAccountOptions = serviceProvider.GetRequiredService<IOptions<DefaultAccountOptions>>().Value;

        var email = UserEmail.Create(efaultAccountOptions.Email);
        var passwordHash = Password.Create(efaultAccountOptions.Password, passwordHasher);
        var userName = UserName.Create(efaultAccountOptions.UserName);
        var admin = User.Create(
            userName: userName.Value,
            email: email.Value,
            now: clock.UtcNow,
            password: passwordHash.Value
        );

        admin.ConfirmEmail(clock.UtcNow);
        admin.UpdateProfile(
            firstName: FirstName.Create(efaultAccountOptions.FirstName).Value,
            lastName: LastName.Create(efaultAccountOptions.LastName).Value,
            displayName: DisplayName.Create(efaultAccountOptions.DisplayName).Value,
            now: clock.UtcNow);

        admin.AssignRole(App.Roles.Definitions.Admin.Id, clock.UtcNow);
        admin.AssignRole(App.Roles.Definitions.User.Id, clock.UtcNow);

        dbContext.Set<User>().Add(admin);

        // Clear domain events raised during seeding (we don't want to publish them)
        admin.ClearDomainEvents();

        await dbContext.SaveChangesAsync();

        logger.LogInformation(
            "Seeded admin user: {Email} (ID: {UserId})", email.Value, admin.Id);
    }

    private static List<User> SeedUsers(IPasswordHasher passwordHasher, DateTime nowUtc)
    {
        var hashedPassword = passwordHasher.Hash(DefaultPassword);
        var users = new List<User>();
        
        // Sellers
        var sellerFaker = new Faker<UserSeedData>("vi")
            .CustomInstantiator(f => new UserSeedData
            {
                UserName = f.Internet.UserName().ToLowerInvariant().Replace(".", "_"),
                Email = f.Internet.Email(provider: "oio.test")
            });

        for (var i = 0; i < 10; i++)
        {
            var data = sellerFaker.Generate();
            var seller = User.Create(
                userName: UserName.Create($"seller_{i + 1}_{data.UserName}").Value,
                email: UserEmail.Create($"seller{i + 1}@oio.test").Value,
                now: nowUtc,
                Password.CreateFromHash(hashedPassword));
            seller.ConfirmEmail(nowUtc);
            seller.AssignRole(App.Roles.Definitions.User.Id, nowUtc);
            seller.AssignRole(App.Roles.Definitions.Seller.Id, nowUtc);
            users.Add(seller);
        }

        // Bidders
        for (var i = 0; i < 20; i++)
        {
            var data = sellerFaker.Generate();
            var bidder = User.Create(
                userName: UserName.Create($"bidder_{i + 1}_{data.UserName}").Value,
                email: UserEmail.Create($"bidder{i + 1}@oio.test").Value,
                now: nowUtc,
                Password.CreateFromHash(hashedPassword));
            bidder.ConfirmEmail(nowUtc);
            
            bidder.AssignRole(App.Roles.Definitions.Bidder.Id, nowUtc);
            bidder.AssignRole(App.Roles.Definitions.User.Id, nowUtc);
            users.Add(bidder);
        }

        // Unconfirmed user
        var unconfirmed = User.Create(
            userName: UserName.Create("unconfirmed_user").Value,
            email: UserEmail.Create("unconfirmed@oio.test").Value,
            now: nowUtc,
            Password.CreateFromHash(hashedPassword));
        users.Add(unconfirmed);

        foreach (var user in users)
        {
            user.ClearDomainEvents();
        }

        return users;
    }

    // ==================== Categories ====================

    private static List<Category> SeedCategories(DateTime nowUtc)
    {
        var categories = new List<Category>();

        var roots = new[]
        {
            ("Điện tử", "dien-tu", "Thiết bị điện tử, máy tính, điện thoại"),
            ("Thời trang", "thoi-trang", "Quần áo, giày dép, phụ kiện"),
            ("Xe cộ", "xe-co", "Ô tô, xe máy, xe đạp"),
            ("Nhà cửa", "nha-cua", "Nội thất, đồ gia dụng"),
            ("Sưu tầm", "suu-tam", "Đồ cổ, tem, tiền xu, tranh"),
            ("Thể thao", "the-thao", "Dụng cụ thể thao, đồ outdoor"),
            ("Sách & Giải trí", "sach-giai-tri", "Sách, đĩa nhạc, game"),
        };

        foreach (var (name, slug, description) in roots)
        {
            var category = Category.Create(
                name: name,
                slug: slug,
                nowUtc,
                description: description,
                sortOrder: categories.Count);
            categories.Add(category);
        }

        // Sub-categories for Điện tử
        var dienTu = categories[0];
        var dienTuSubs = new[]
        {
            ("Điện thoại", "dien-thoai"),
            ("Laptop", "laptop"),
            ("Máy tính bảng", "may-tinh-bang"),
            ("Phụ kiện", "phu-kien-dien-tu"),
            ("Máy ảnh", "may-anh"),
        };

        foreach (var (name, slug) in dienTuSubs)
        {
            var sub = Category.Create(
                name: name,
                slug: slug,
                nowUtc,
                parentPath: CategoryPath.Create("dien-tu").Value,
                parentId: dienTu.Id,
                sortOrder: categories.Count);
            categories.Add(sub);
        }

        // Sub-categories for Sưu tầm
        var suuTam = categories[4];
        var suuTamSubs = new[]
        {
            ("Đồ cổ", "do-co"),
            ("Tem", "tem"),
            ("Tiền xu", "tien-xu"),
            ("Tranh", "tranh"),
            ("Đồng hồ cổ", "dong-ho-co"),
        };

        foreach (var (name, slug) in suuTamSubs)
        {
            var sub = Category.Create(
                name: name,
                slug: slug,
                nowUtc: nowUtc,
                parentPath: CategoryPath.Create("suu-tam").Value,
                parentId: suuTam.Id,
                sortOrder: categories.Count);
            categories.Add(sub);
        }

        // Sub-categories for Thời trang
        var thoiTrang = categories[1];
        var thoiTrangSubs = new[]
        {
            ("Quần áo nam", "quan-ao-nam"),
            ("Quần áo nữ", "quan-ao-nu"),
            ("Giày dép", "giay-dep"),
            ("Túi xách", "tui-xach"),
            ("Đồng hồ", "dong-ho"),
        };

        foreach (var (name, slug) in thoiTrangSubs)
        {
            var sub = Category.Create(
                name: name,
                slug: slug,
                nowUtc: nowUtc,
                parentPath: CategoryPath.Create("thoi-trang").Value,
                parentId: thoiTrang.Id,
                sortOrder: categories.Count);
            categories.Add(sub);
        }

        return categories;
    }

    // ==================== Items ====================

    private static List<Item> SeedItems(List<User> users, List<Category> categories, DateTime nowUtc)
    {
        var sellers = users.Where(u => u.UserName.Value.StartsWith("seller")).ToList();
        var leafCategories = categories.Where(c => c.ParentId.HasValue).ToList();
        var items = new List<Item>();

        var conditionValues = new[]
        {
            ItemCondition.New,
            ItemCondition.LikeNew,
            ItemCondition.Good,
            ItemCondition.VeryGood,
            ItemCondition.Acceptable,
        };

        var itemFaker = new Faker<ItemSeedData>("vi")
            .RuleFor(x => x.Title, f => f.Commerce.ProductName())
            .RuleFor(x => x.Description, f => f.Commerce.ProductDescription())
            .RuleFor(x => x.Quantity, f => f.Random.Int(1, 5))
            .RuleFor(x => x.Condition, f => f.PickRandom(conditionValues));

        // Each seller creates 3-8 items
        foreach (var seller in sellers)
        {
            var itemCount = Faker.Random.Int(3, 8);

            for (var i = 0; i < itemCount; i++)
            {
                var data = itemFaker.Generate();
                var category = Faker.PickRandom(leafCategories);

                var item = Item.Create(
                    nowUtc: nowUtc,
                    sellerId: seller.Id,
                    title: data.Title,
                    condition: data.Condition,
                    categoryId: category.Id,
                    description: data.Description,
                    quantity: data.Quantity);

                // Add fake media (without actual Cloudinary URLs)
                var imageCount = Faker.Random.Int(1, 5);
                for (var img = 0; img < imageCount; img++)
                {
                    var mediaUpload = MediaUpload.Create(
                        seller.Id,
                        context: "items_image",
                        resourceType: "image",
                        null,
                        publicId: $"items/{item.Id.Value}/img_{Guid.NewGuid().ToString("N")[..12]}",
                        folder: $"items/{item.Id}",
                        fileName: $"product_{img + 1}.jpg",
                        signatureExpirationMinutes: TimeSpan.FromMinutes(60),
                        nowUtc
                    );

                    mediaUpload.Confirm(
                        secureUrl: $"https://picsum.photos/seed/{Guid.NewGuid():N}/800/600",
                        bytes: Faker.Random.Long(100_000, 5_000_000),
                        format: "jpg",
                        width: 800,
                        height: 600,
                        durationSeconds: null,
                        orphanExpirationMinutes: TimeSpan.FromMinutes(30),
                        nowUtc: nowUtc
                    );
                    
                    // Sử dụng reflection hoặc method internal để add
                    // Giả sử có AddMediaForSeed internal method
                    item.AddMedia(nowUtc ,mediaUpload, false, imageCount);
                }

                // Activate items (most of them)
                if (Faker.Random.Bool(0.85f))
                {
                    item.Activate(nowUtc);
                }

                items.Add(item);
            }
        }
        
        foreach (var item in items)
        {
            item.ClearDomainEvents();
        }

        return items;
    }

    // ==================== Auctions ====================

    private static List<Auction> SeedAuctions(List<Item> items, DateTime nowUtc)
    {
        var activeItems = items
            .Where(i => i.Status == ItemStatus.Active)
            .ToList();

        var auctions = new List<Auction>();

        var auctionFaker = new Faker("vi");

        foreach (var item in activeItems)
        {
            // 70% items have auctions
            if (!auctionFaker.Random.Bool(0.7f))
                continue;

            var startingPrice = auctionFaker.Random.Decimal(50_000, 10_000_000);
            startingPrice = Math.Round(startingPrice / 10_000) * 10_000; // Round to 10k

            var bidIncrement = startingPrice switch
            {
                < 500_000 => 10_000m,
                < 2_000_000 => 50_000m,
                < 10_000_000 => 100_000m,
                _ => 500_000m
            };

            var hasBuyNow = auctionFaker.Random.Bool(0.4f);
            var buyNowPrice = hasBuyNow
                ? startingPrice * auctionFaker.Random.Decimal(2m, 5m)
                : (decimal?)null;
            if (buyNowPrice.HasValue)
                buyNowPrice = Math.Round(buyNowPrice.Value / 10_000) * 10_000;

            var hasReserve = auctionFaker.Random.Bool(0.3f);
            var reservePrice = hasReserve
                ? startingPrice * auctionFaker.Random.Decimal(1.5m, 3m)
                : (decimal?)null;
            if (reservePrice.HasValue)
                reservePrice = Math.Round(reservePrice.Value / 10_000) * 10_000;

            // Mix of past, active, and future auctions
            var auctionType = auctionFaker.Random.WeightedRandom(
                new[] { "past", "active", "future", "pending" },
                new[] { 0.2f, 0.4f, 0.2f, 0.2f });

            DateTime startTime, endTime;

            switch (auctionType)
            {
                case "past":
                    startTime = nowUtc.AddDays(auctionFaker.Random.Int(-30, -3));
                    endTime = startTime.AddDays(auctionFaker.Random.Int(1, 7));
                    break;

                case "active":
                    startTime = nowUtc.AddDays(auctionFaker.Random.Int(-5, -1));
                    endTime = nowUtc.AddDays(auctionFaker.Random.Int(1, 14));
                    break;

                case "future":
                    startTime = nowUtc.AddDays(auctionFaker.Random.Int(1, 14));
                    endTime = startTime.AddDays(auctionFaker.Random.Int(3, 14));
                    break;

                default: // pending
                    startTime = nowUtc.AddHours(auctionFaker.Random.Int(1, 48));
                    endTime = startTime.AddDays(auctionFaker.Random.Int(3, 7));
                    break;
            }
            
           

            var auction = Auction.Create(
                itemId: item.Id,
                sellerId: item.SellerId,
                nowUtc,
                startingPrice: Money.Create(startingPrice, "VND").Value,
                bidIncrement: Money.Create(bidIncrement, "VND").Value,
                duration:  AuctionDuration.Create(startTime, endTime, TimeSpan.FromMinutes(30), TimeSpan.FromDays(180), nowUtc.AddDays(-31)).Value,
                reservePrice: reservePrice.HasValue ? Money.Create(reservePrice.Value, "VND").Value : null,
                buyNowPrice: buyNowPrice.HasValue ? Money.Create(buyNowPrice.Value, "VND").Value : null);

            // Set status based on type
            switch (auctionType)
            {
                case "active":
                    auction.Value.Publish(nowUtc);
                    auction.Value.Start(nowUtc); // Pending → Active
                    item.MarkInAuction(nowUtc);
                    break;

                case "pending":
                    auction.Value.Publish(nowUtc); // Draft → Pending
                    break;

                case "past":
                    auction.Value.Publish(nowUtc);
                    auction.Value.Start(nowUtc); 
                    item.MarkInAuction(nowUtc);
                    // Don't end here — lifecycle job handles that
                    break;
            }

            auctions.Add(auction.Value);
        }
        
        foreach (var auction in auctions)
        {
            auction.ClearDomainEvents();
        }

        return auctions;
    }

    private class ItemSeedData
    {
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public int Quantity { get; set; }
        public ItemCondition Condition { get; set; } = null!;
    }
    
    public class UserSeedData
    {
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
    }
}