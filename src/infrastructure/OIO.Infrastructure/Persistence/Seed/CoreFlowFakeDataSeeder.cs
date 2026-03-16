using System.Net;
using System.Reflection;
using Bogus;
using Bogus.DataSets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Domain.AppDefinitions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects;
using OIO.Domain.Context.CatalogContext.Aggregates.Categories;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.CatalogContext.Enums;
using OIO.Domain.Context.CatalogContext.ValueObjects;
using OIO.Domain.Context.Shared.Entities;
using OIO.Domain.Context.Shared.Enums;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.Services;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.SeedWork.DomainEvents;
using Address = OIO.Domain.Context.UserContext.ValueObjects.Address;
using Currency = OIO.Domain.Context.Shared.Enums.Currency;

namespace OIO.Infrastructure.Persistence.Seed;

public static class CoreFlowFakeDataSeeder
{
    private const string SeedPassword = "FakeData@123";
    private const string SeedPrefix = "[seed:coreflow:";
    private static readonly IPAddress LoopbackIp = IPAddress.Loopback;

    private sealed record FakeUserSeed(
        string Key,
        string UserName,
        string Email,
        string PhoneNumber,
        string[] Roles,
        bool AddDefaultAddress);

    private sealed record FakeTermsBundle(
        TermsDocument Platform,
        TermsDocument Seller,
        TermsDocument Bidder);

    public static async Task SeedAsync(
        ApplicationDbContext dbContext,
        IServiceProvider services,
        ILogger logger)
    {
        var clock = services.GetRequiredService<IClock>();
        var passwordHasher = services.GetRequiredService<IPasswordHasher>();
        var nowUtc = clock.UtcNow;

        Randomizer.Seed = new Random(20260315);
        var faker = new Faker();

        var admin = await dbContext.Set<User>()
            .Include(x => x.Roles)
            .FirstOrDefaultAsync(x => x.Roles.Any(r => r.RoleName == App.Roles.Catalogs.Admin));

        if (admin is null)
        {
            logger.LogWarning("Skipping coreflow fake data seeding because no admin user was found.");
            return;
        }

        var terms = await EnsureTermsAsync(dbContext, admin, nowUtc, logger);
        var categories = await EnsureCategoriesAsync(dbContext, nowUtc, faker, logger);

        var seller = await EnsureUserAsync(
            dbContext,
            passwordHasher,
            faker,
            nowUtc,
            new FakeUserSeed(
                Key: "seller",
                UserName: "coreflow-seller",
                Email: "coreflow.seller@example.com",
                PhoneNumber: "0910000001",
                Roles: [App.Roles.Catalogs.User, App.Roles.Catalogs.Seller],
                AddDefaultAddress: true),
            logger);

        var bidder1 = await EnsureUserAsync(
            dbContext,
            passwordHasher,
            faker,
            nowUtc,
            new FakeUserSeed(
                Key: "bidder-1",
                UserName: "coreflow-bidder-1",
                Email: "coreflow.bidder1@example.com",
                PhoneNumber: "0910000002",
                Roles: [App.Roles.Catalogs.User, App.Roles.Catalogs.Bidder],
                AddDefaultAddress: true),
            logger);

        var bidder2 = await EnsureUserAsync(
            dbContext,
            passwordHasher,
            faker,
            nowUtc,
            new FakeUserSeed(
                Key: "bidder-2",
                UserName: "coreflow-bidder-2",
                Email: "coreflow.bidder2@example.com",
                PhoneNumber: "0910000003",
                Roles: [App.Roles.Catalogs.User, App.Roles.Catalogs.Bidder],
                AddDefaultAddress: true),
            logger);

        await EnsureApprovedVerificationAsync(dbContext, seller, admin, nowUtc, logger);
        await EnsureSellerProfileAsync(dbContext, seller, faker, nowUtc, logger);

        await EnsureTermsAcceptanceAsync(dbContext, seller, terms.Platform, nowUtc);
        await EnsureTermsAcceptanceAsync(dbContext, seller, terms.Seller, nowUtc);
        await EnsureTermsAcceptanceAsync(dbContext, bidder1, terms.Platform, nowUtc);
        await EnsureTermsAcceptanceAsync(dbContext, bidder1, terms.Bidder, nowUtc);
        await EnsureTermsAcceptanceAsync(dbContext, bidder2, terms.Platform, nowUtc);
        await EnsureTermsAcceptanceAsync(dbContext, bidder2, terms.Bidder, nowUtc);

        await EnsurePendingModerationScenarioAsync(
            dbContext,
            seller,
            categories[0],
            nowUtc,
            faker,
            scenarioKey: "pending-review",
            verifyByPlatform: false,
            title: "Coreflow Pending Review Listing",
            logger: logger);

        await EnsurePendingModerationScenarioAsync(
            dbContext,
            seller,
            categories[1],
            nowUtc,
            faker,
            scenarioKey: "pending-verify",
            verifyByPlatform: true,
            title: "Coreflow Pending Verify Listing",
            logger: logger);

        await EnsureScheduledAuctionScenarioAsync(
            dbContext,
            seller,
            admin,
            bidder1,
            bidder2,
            categories[2],
            nowUtc,
            faker,
            scenarioKey: "regular-scheduled",
            title: "Coreflow Regular Auction",
            auctionType: AuctionType.Regular,
            startingPrice: 1_200_000m,
            bidIncrement: 50_000m,
            reservePrice: 1_500_000m,
            buyNowPrice: 2_200_000m,
            isFeatured: true,
            priorityScore: 75m,
            logger: logger);

        await EnsureScheduledAuctionScenarioAsync(
            dbContext,
            seller,
            admin,
            bidder1,
            bidder2,
            categories[3],
            nowUtc,
            faker,
            scenarioKey: "sealed-scheduled",
            title: "Coreflow Sealed Auction",
            auctionType: AuctionType.Sealed,
            startingPrice: 1_800_000m,
            bidIncrement: 100_000m,
            reservePrice: 2_100_000m,
            buyNowPrice: null,
            isFeatured: false,
            priorityScore: 45m,
            logger: logger);

        await dbContext.SaveChangesAsync();

        logger.LogInformation(
            "Coreflow fake data ready. Accounts: seller={SellerEmail}, bidder1={Bidder1Email}, bidder2={Bidder2Email}. Shared password: {Password}",
            seller.Email.Value,
            bidder1.Email.Value,
            bidder2.Email.Value,
            SeedPassword);
    }

    private static async Task<FakeTermsBundle> EnsureTermsAsync(
        ApplicationDbContext dbContext,
        User admin,
        DateTime nowUtc,
        ILogger logger)
    {
        var platform = await EnsureTermsDocumentAsync(dbContext, admin, "platform", nowUtc, logger);
        var seller = await EnsureTermsDocumentAsync(dbContext, admin, "seller", nowUtc, logger);
        var bidder = await EnsureTermsDocumentAsync(dbContext, admin, "bidder", nowUtc, logger);

        return new FakeTermsBundle(platform, seller, bidder);
    }

    private static async Task<TermsDocument> EnsureTermsDocumentAsync(
        ApplicationDbContext dbContext,
        User owner,
        string termType,
        DateTime nowUtc,
        ILogger logger)
    {
        var existing = await dbContext.Set<TermsDocument>()
            .FirstOrDefaultAsync(x => x.TermType == termType && x.IsActive);

        if (existing is not null)
            return existing;

        var upload = CreateConfirmedUpload(
            userId: owner.Id,
            context: "terms_document",
            resourceType: "raw",
            folder: "seed/terms",
            publicId: $"{termType}-v1",
            secureUrl: $"https://example.com/seed/terms/{termType}-v1.pdf",
            fileName: $"{termType}-terms-v1.pdf",
            nowUtc: nowUtc,
            format: "pdf",
            bytes: 8_192);

        dbContext.Insert(upload);

        var terms = TermsDocument.Create(termType, 1, upload, nowUtc).Value;
        terms.Activate(nowUtc);

        dbContext.Insert(terms);

        logger.LogInformation("Seeded fake active terms document for type '{TermType}'.", termType);

        return terms;
    }

    private static async Task<List<Category>> EnsureCategoriesAsync(
        ApplicationDbContext dbContext,
        DateTime nowUtc,
        Faker faker,
        ILogger logger)
    {
        var seeds = new[]
        {
            new { Name = "Collectibles", Slug = "collectibles" },
            new { Name = "Luxury Watches", Slug = "luxury-watches" },
            new { Name = "Fine Art", Slug = "fine-art" },
            new { Name = "Consumer Electronics", Slug = "consumer-electronics" },
            new { Name = "Fashion", Slug = "fashion" }
        };

        var categories = new List<Category>(seeds.Length);

        foreach (var seed in seeds)
        {
            var existing = await dbContext.Set<Category>()
                .FirstOrDefaultAsync(x => x.Name == seed.Name);

            if (existing is not null)
            {
                categories.Add(existing);
                continue;
            }

            var slug = Slug.Create(seed.Slug).Value;
            var category = Category.Create(
                name: seed.Name,
                slug: slug,
                nowUtc: nowUtc,
                description: faker.Commerce.Categories(1).FirstOrDefault() ?? faker.Lorem.Sentence(8));

            dbContext.Insert(category);
            categories.Add(category);

            logger.LogInformation("Seeded fake category '{CategoryName}'.", seed.Name);
        }

        return categories;
    }

    private static async Task<User> EnsureUserAsync(
        ApplicationDbContext dbContext,
        IPasswordHasher passwordHasher,
        Faker faker,
        DateTime nowUtc,
        FakeUserSeed seed,
        ILogger logger)
    {
        var email = UserEmail.Create(seed.Email).Value;

        var existing = await dbContext.Set<User>()
            .Include(x => x.Roles)
            .Include(x => x.Addresses)
            .FirstOrDefaultAsync(x => x.Email == email);

        if (existing is not null)
        {
            EnsureUserRoles(existing, seed.Roles, nowUtc);
            EnsurePhone(existing, seed.PhoneNumber, nowUtc);
            if (seed.AddDefaultAddress && existing.Addresses.Count == 0)
            {
                AddDefaultAddress(existing, faker, nowUtc);
            }

            ClearDomainEvents(existing);
            return existing;
        }

        var person = new Person();
        var fullName = person.FullName;
        var firstName = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Coreflow";
        var lastName = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1).FirstOrDefault() ?? seed.Key;

        var user = User.Create(
            userName: UserName.Create(seed.UserName).Value,
            email: email,
            now: nowUtc,
            personName: PersonName.Create(firstName, lastName, fullName),
            currency: Currency.Vnd,
            password: Password.Create(SeedPassword, passwordHasher).Value);

        user.ConfirmEmail(nowUtc);
        EnsureUserRoles(user, seed.Roles, nowUtc);
        EnsurePhone(user, seed.PhoneNumber, nowUtc);

        if (seed.AddDefaultAddress)
        {
            AddDefaultAddress(user, faker, nowUtc);
        }

        dbContext.Insert(user);
        ClearDomainEvents(user);

        logger.LogInformation("Seeded fake user '{Email}' with roles [{Roles}].", seed.Email, string.Join(", ", seed.Roles));

        return user;
    }

    private static async Task EnsureApprovedVerificationAsync(
        ApplicationDbContext dbContext,
        User seller,
        User admin,
        DateTime nowUtc,
        ILogger logger)
    {
        var verifications = await dbContext.Set<IdentityVerification>()
            .Where(x => x.UserId == seller.Id)
            .ToListAsync();

        if (verifications.Any(x =>
                x.Status == IdentityVerificationStatus.Approved &&
                x.VerificationType == VerificationType.GovernmentId))
            return;

        var verification = IdentityVerification.Create(seller.Id, VerificationType.GovernmentId, nowUtc);

        var upload = CreateConfirmedUpload(
            userId: seller.Id,
            context: "verification_document",
            resourceType: "image",
            folder: "seed/verifications",
            publicId: $"{seller.Id.Value:N}-government-id-front",
            secureUrl: $"https://picsum.photos/seed/{seller.Id.Value:N}-id-front/1200/800",
            fileName: "government-id-front.jpg",
            nowUtc: nowUtc,
            format: "jpg",
            bytes: 154_321,
            width: 1200,
            height: 800);

        dbContext.Insert(upload);

        verification.AddDocument(VerificationDocumentType.IdFront, upload, maxForType: 5, nowUtc);
        verification.Submit(nowUtc);
        verification.Approve(admin.Id, nowUtc);

        dbContext.Insert(verification);
        ClearDomainEvents(verification);

        logger.LogInformation("Seeded fake approved verification for seller '{SellerEmail}'.", seller.Email.Value);
    }

    private static async Task EnsureSellerProfileAsync(
        ApplicationDbContext dbContext,
        User seller,
        Faker faker,
        DateTime nowUtc,
        ILogger logger)
    {
        var profile = await dbContext.Set<SellerProfile>()
            .FirstOrDefaultAsync(x => x.Id == seller.Id);

        if (profile is null)
        {
            profile = SellerProfile.Create(
                seller.Id,
                storeName: $"{faker.Company.CompanyName()} Auctions",
                storeDescription: faker.Company.CatchPhrase(),
                nowUtc: nowUtc);

            profile.Verify(nowUtc);
            dbContext.Insert(profile);

            logger.LogInformation("Seeded fake verified seller profile for '{SellerEmail}'.", seller.Email.Value);
            return;
        }

        if (profile.Status == SellerProfileStatus.Verified)
            return;

        profile.Update(
            storeName: profile.StoreName,
            storeDescription: string.IsNullOrWhiteSpace(profile.StoreDescription)
                ? faker.Company.CatchPhrase()
                : profile.StoreDescription,
            nowUtc: nowUtc);

        profile.Verify(nowUtc);
    }

    private static async Task EnsureTermsAcceptanceAsync(
        ApplicationDbContext dbContext,
        User user,
        TermsDocument term,
        DateTime nowUtc)
    {
        var exists = await dbContext.Set<TermsAcceptance>()
            .AnyAsync(x => x.UserId == user.Id && x.TermDocumentId == term.Id);

        if (exists)
            return;

        var acceptance = TermsAcceptance.Create(user.Id, term.Id, nowUtc, LoopbackIp, "CoreflowFakeDataSeeder").Value;
        dbContext.Insert(acceptance);
    }

    private static async Task EnsurePendingModerationScenarioAsync(
        ApplicationDbContext dbContext,
        User seller,
        Category category,
        DateTime nowUtc,
        Faker faker,
        string scenarioKey,
        bool verifyByPlatform,
        string title,
        ILogger logger)
    {
        var marker = $"{SeedPrefix}{scenarioKey}]";
        var existingItem = await dbContext.Set<Item>()
            .FirstOrDefaultAsync(x => x.SellerId == seller.Id && x.Description != null && x.Description.Contains(marker));

        if (existingItem is not null)
            return;

        var item = CreateSeedItem(
            seller,
            category,
            title,
            $"{faker.Commerce.ProductDescription()}\n\n{marker}",
            nowUtc);

        var upload = CreateConfirmedUpload(
            userId: seller.Id,
            context: "item_image",
            resourceType: "image",
            folder: "seed/items",
            publicId: $"{scenarioKey}-cover",
            secureUrl: $"https://picsum.photos/seed/{scenarioKey}/1200/900",
            fileName: $"{scenarioKey}-cover.jpg",
            nowUtc: nowUtc,
            format: "jpg",
            bytes: 208_764,
            width: 1200,
            height: 900);

        dbContext.Insert(upload);
        item.AddMedia(nowUtc, upload, isPrimary: true, maxForType: 10);
        item.Submit(verifyByPlatform, nowUtc);

        dbContext.Insert(item);
        ClearDomainEvents(item);

        var auction = CreateDraftAuctionForItem(
            seller,
            item,
            auctionType: AuctionType.Regular,
            startingPrice: verifyByPlatform ? 950_000m : 850_000m,
            bidIncrement: 50_000m,
            reservePrice: verifyByPlatform ? 1_150_000m : 1_000_000m,
            buyNowPrice: verifyByPlatform ? 1_650_000m : 1_350_000m,
            nowUtc: nowUtc);

        dbContext.Insert(auction);
        ClearDomainEvents(auction);

        logger.LogInformation("Seeded fake moderation scenario '{ScenarioKey}'.", scenarioKey);
    }

    private static async Task EnsureScheduledAuctionScenarioAsync(
        ApplicationDbContext dbContext,
        User seller,
        User admin,
        User bidder1,
        User bidder2,
        Category category,
        DateTime nowUtc,
        Faker faker,
        string scenarioKey,
        string title,
        AuctionType auctionType,
        decimal startingPrice,
        decimal bidIncrement,
        decimal? reservePrice,
        decimal? buyNowPrice,
        bool isFeatured,
        decimal priorityScore,
        ILogger logger)
    {
        var marker = $"{SeedPrefix}{scenarioKey}]";
        var existingItem = await dbContext.Set<Item>()
            .FirstOrDefaultAsync(x => x.SellerId == seller.Id && x.Description != null && x.Description.Contains(marker));

        var existingAuction = existingItem is not null
            ? await dbContext.Set<Auction>().FirstOrDefaultAsync(x => x.ItemId == existingItem.Id)
            : null;

        if (existingAuction is not null)
            return;

        var item = existingItem ?? CreateSeedItem(
            seller,
            category,
            title,
            $"{faker.Commerce.ProductDescription()}\n\n{marker}",
            nowUtc);

        if (existingItem is null)
        {
            var upload = CreateConfirmedUpload(
                userId: seller.Id,
                context: "item_image",
                resourceType: "image",
                folder: "seed/items",
                publicId: $"{scenarioKey}-hero",
                secureUrl: $"https://picsum.photos/seed/{scenarioKey}-hero/1200/900",
                fileName: $"{scenarioKey}-hero.jpg",
                nowUtc: nowUtc,
                format: "jpg",
                bytes: 248_110,
                width: 1200,
                height: 900);

            dbContext.Insert(upload);
            item.AddMedia(nowUtc, upload, isPrimary: true, maxForType: 10);
            item.Submit(false, nowUtc);
            item.AssignAdmin(admin.Id, nowUtc);
            item.Approve(admin.Id, nowUtc);
            item.MarkInAuction(nowUtc);

            dbContext.Insert(item);
            ClearDomainEvents(item);
        }

        var auction = CreateDraftAuctionForItem(
            seller,
            item,
            auctionType,
            startingPrice,
            bidIncrement,
            reservePrice,
            buyNowPrice,
            nowUtc);

        auction.SubmitConfiguration(nowUtc);

        var qualificationWindow = QualificationWindow.Create(
            startTime: nowUtc.AddHours(1),
            endTime: nowUtc.AddHours(6)).Value;

        var timing = AuctionInfo.Create(
            nowUtc: nowUtc,
            startTime: nowUtc.AddHours(8),
            endTime: nowUtc.AddDays(2),
            autoExtend: true,
            extensionMinutes: 5,
            qualification: qualificationWindow).Value;

        auction.SetTiming(timing, nowUtc);
        auction.ApplyCuration(
            assignedAdminId: admin.Id,
            priority: PriorityInfo.Create(priorityScore, "{\"seed\":true,\"source\":\"coreflow\"}"),
            isFeatured: isFeatured,
            nowUtc: nowUtc);
        auction.Publish(nowUtc);
        auction.AddWatcher(bidder1.Id, nowUtc, notifyOnBid: true, notifyOnEnd: true);
        auction.AddWatcher(bidder2.Id, nowUtc, notifyOnBid: true, notifyOnEnd: true);

        for (var i = 0; i < 12; i++)
        {
            auction.IncrementView(nowUtc);
        }

        dbContext.Insert(auction);
        ClearDomainEvents(auction);

        logger.LogInformation("Seeded fake scheduled auction scenario '{ScenarioKey}'.", scenarioKey);
    }

    private static Item CreateSeedItem(
        User seller,
        Category category,
        string title,
        string description,
        DateTime nowUtc)
    {
        return Item.Create(
            nowUtc: nowUtc,
            sellerId: seller.Id,
            title: ItemTitle.Create(title).Value,
            condition: ItemCondition.New,
            description: description,
            categoryId: category.Id,
            quantity: 1,
            attributes: "{\"seed\":true,\"source\":\"coreflow\"}");
    }

    private static Auction CreateDraftAuctionForItem(
        User seller,
        Item item,
        AuctionType auctionType,
        decimal startingPrice,
        decimal bidIncrement,
        decimal? reservePrice,
        decimal? buyNowPrice,
        DateTime nowUtc)
    {
        var pricing = AuctionPricing.Create(
            startingPrice: startingPrice,
            bidIncrement: bidIncrement,
            currency: Currency.Vnd,
            reservePrice: reservePrice,
            buyNowPrice: buyNowPrice).Value;

        var auction = Auction.Create(
            sellerId: seller.Id,
            itemId: item.Id,
            auctionType: auctionType,
            pricing: pricing,
            nowUtc: nowUtc).Value;

        SetPrivateProperty(auction, nameof(Auction.Item), item);
        return auction;
    }

    private static MediaUpload CreateConfirmedUpload(
        OIO.Domain.Context.UserContext.ValueObjects.Ids.UserId userId,
        string context,
        string resourceType,
        string folder,
        string publicId,
        string secureUrl,
        string fileName,
        DateTime nowUtc,
        string format,
        long bytes,
        int? width = null,
        int? height = null,
        double? durationSeconds = null)
    {
        var storageRef = StorageRef.Create(publicId, folder).Value;
        var placeholderInfo = MediaInfo.Create(fileName: fileName);

        var upload = MediaUpload.Create(
            userId: userId,
            context: context,
            resourceType: resourceType,
            entityId: null,
            idType: null,
            mediaInfo: placeholderInfo,
            storageRef: storageRef,
            signatureExpirationMinutes: TimeSpan.FromMinutes(15),
            nowUtc: nowUtc);

        var confirmedInfo = MediaInfo.Create(
            secureUrl: secureUrl,
            fileName: fileName,
            bytes: bytes,
            format: format,
            width: width,
            height: height,
            durationSeconds: durationSeconds);

        upload.Confirm(
            mediaInfo: confirmedInfo,
            orphanExpirationMinutes: TimeSpan.FromDays(30),
            nowUtc: nowUtc);

        return upload;
    }

    private static void EnsureUserRoles(User user, IEnumerable<string> roles, DateTime nowUtc)
    {
        foreach (var role in roles)
        {
            user.AssignRole(role, nowUtc);
        }
    }

    private static void EnsurePhone(User user, string phoneNumber, DateTime nowUtc)
    {
        if (user.PhoneNumberConfirmed)
            return;

        user.SetPhoneNumber(PhoneNumber.Create(phoneNumber, PhoneNumber.DefaultRegion).Value, nowUtc);
        user.ConfirmPhoneNumber(nowUtc);
    }

    private static void AddDefaultAddress(User user, Faker faker, DateTime nowUtc)
    {
        var address = Address.Create(
            street: $"{faker.Random.Int(1, 250)} {faker.Address.StreetName()}",
            ward: "Ben Nghe",
            district: "District 1",
            city: "Ho Chi Minh City",
            postalCode: "700000").Value;

        var recipient = RecipientInfo.Create(
            recipientName: user.Profile.Name.DisplayName ?? user.Profile.Name.FullName,
            phoneNumber: user.PhoneNumber?.Value ?? "0910000999",
            countryCode: PhoneNumber.DefaultRegion).Value;

        user.AddAddress(
            type: AddressType.Home,
            recipient: recipient,
            address: address,
            now: nowUtc,
            isDefault: true);
    }

    private static void SetPrivateProperty<TTarget, TValue>(
        TTarget target,
        string propertyName,
        TValue value)
    {
        var property = typeof(TTarget).GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        property?.SetValue(target, value);
    }

    private static void ClearDomainEvents(params object?[] entities)
    {
        foreach (var entity in entities)
        {
            if (entity is IHasDomainEvents hasDomainEvents)
            {
                hasDomainEvents.ClearDomainEvents();
            }
        }
    }
}
