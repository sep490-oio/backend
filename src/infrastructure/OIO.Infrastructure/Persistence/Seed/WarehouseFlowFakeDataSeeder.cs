using System.Reflection;
using Bogus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Domain.AppDefinitions;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.CatalogContext.Enums;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseStorage;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.ValueObjects;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.CatalogContext.ValueObjects;
using OIO.Domain.Context.CatalogContext.Aggregates.Categories;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.Context.UserContext.Services;
using OIO.Domain.Context.Shared.Enums;
using OIO.Domain.Context.CatalogContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Seed;

public static class WarehouseFlowFakeDataSeeder
{
    private const string SeedPrefix = "[seed:warehouseflow]";
    private const string SeedPassword = "FakeData@123";

    public static async Task SeedAsync(
        ApplicationDbContext dbContext,
        IServiceProvider services,
        ILogger logger)
    {
        try
        {
            // 1. Safety Check for Deployment: Skip if any Warehouse Data already exists
            if (await dbContext.Set<InboundShipment>().AnyAsync() ||
                await dbContext.Set<WarehouseItem>().AnyAsync() ||
                await dbContext.Set<OutboundShipment>().AnyAsync())
            {
                logger.LogInformation("Skipping warehouse fake data seeding because some data already exists.");
                return;
            }

            var clock = services.GetRequiredService<IClock>();
            var passwordHasher = services.GetRequiredService<IPasswordHasher>();
            var nowUtc = clock.UtcNow;
            var faker = new Faker("vi");
            Randomizer.Seed = new Random(20260329);

            // 2. Required master records (Isolated from core flow)
            var admin = await dbContext.Set<User>().FirstOrDefaultAsync(x => x.Email.Value == "admin@oio.com");
            if (admin is null)
            {
                logger.LogWarning("Skipping warehouse fake data seeding because admin user was not found.");
                return;
            }

            var seller = await EnsureUserAsync(dbContext, passwordHasher, faker, nowUtc, "warehouse.staff@oio.com", "WH-staff", [App.Roles.Catalogs.User, App.Roles.Catalogs.WarehouseStaff], logger);
            var buyer = await EnsureUserAsync(dbContext, passwordHasher, faker, nowUtc, "warehouse.inspector@oio.com", "WH-Inspector", [App.Roles.Catalogs.User, App.Roles.Catalogs.Inspector], logger);
            var category = await EnsureCategoryAsync(dbContext, nowUtc, logger);

            logger.LogInformation("Seeding Warehouse Fake Data (Isolated Environment)...");

            // Seed 1: Stored Warehouse Locations
            var locationIds = await EnsureStorageLocations(dbContext, nowUtc, logger);

            // Seed 2: Items from your Export (Isolated under new Seller/Category)
            var pendingInboundItem = await FindOrCreateItem(dbContext, seller, category.Id, "Bộ tua vít Xỉnui 31 in 1", nowUtc);
            var arrivedInboundItem = await FindOrCreateItem(dbContext, seller, category.Id, "Vintage Watch Auction", nowUtc);
            var inspectionFailedItem = await FindOrCreateItem(dbContext, seller, category.Id, "A beautiful vintage watch", nowUtc);
            var storedItem = await FindOrCreateItem(dbContext, seller, category.Id, "New ABC 13 9370, 13.3, 5th Gen CoreA5-8250U, 8GB RAM, 256GB SSD", nowUtc);
            var outboundItem = await FindOrCreateItem(dbContext, seller, category.Id, "QC Test Item - Vintage Camera 2026", nowUtc);

            // Seed 3: Pending Inbound
            await CreateInboundShipmentScenario(dbContext, pendingInboundItem.Id.Value, seller, InboundShipmentStatus.AwaitingPickup, nowUtc);

            // Seed 4: Arrived Inbound -> Pending Inspection WarehouseItem
            var inboundArrived = await CreateInboundShipmentScenario(dbContext, arrivedInboundItem.Id.Value, seller, InboundShipmentStatus.Arrived, nowUtc);
            var whItemArrived = await EnsureWarehouseItem(dbContext, arrivedInboundItem.Id.Value, inboundArrived.Id, WarehouseItemStatus.Pending, nowUtc);
            SetPrivateProperty(whItemArrived, "Status", WarehouseItemStatus.Received);

            // Seed 5: Inspection Failed -> Returning Outbound
            var inboundFailed = await CreateInboundShipmentScenario(dbContext, inspectionFailedItem.Id.Value, seller, InboundShipmentStatus.Completed, nowUtc);
            var whItemFailed = await EnsureWarehouseItem(dbContext, inspectionFailedItem.Id.Value, inboundFailed.Id, WarehouseItemStatus.Inspected, nowUtc);
            await EnsureWarehouseInspection(dbContext, whItemFailed, inboundFailed.Id, inspectionFailedItem.Id.Value, admin, WarehouseInspectionDecisionStatus.Rejected, nowUtc);
            await EnsureOutboundShipment(dbContext, whItemFailed.Id, seller, seller, OutboundShipmentStatus.InTransit, nowUtc);

            // Seed 6: Inspected & Stored
            var inboundStored = await CreateInboundShipmentScenario(dbContext, storedItem.Id.Value, seller, InboundShipmentStatus.Completed, nowUtc);
            var whItemStored = await EnsureWarehouseItem(dbContext, storedItem.Id.Value, inboundStored.Id, WarehouseItemStatus.Stored, nowUtc);
            SetPrivateProperty(whItemStored, "StorageLocationId", locationIds.First());
            await EnsureWarehouseInspection(dbContext, whItemStored, inboundStored.Id, storedItem.Id.Value, admin, WarehouseInspectionDecisionStatus.Approved, nowUtc);

            // Seed 7: Dispatched Outbound
            var inboundDispatched = await CreateInboundShipmentScenario(dbContext, outboundItem.Id.Value, seller, InboundShipmentStatus.Completed, nowUtc);
            var whItemDispatched = await EnsureWarehouseItem(dbContext, outboundItem.Id.Value, inboundDispatched.Id, WarehouseItemStatus.Dispatched, nowUtc);
            await EnsureWarehouseInspection(dbContext, whItemDispatched, inboundDispatched.Id, outboundItem.Id.Value, admin, WarehouseInspectionDecisionStatus.Approved, nowUtc);
            await EnsureOutboundShipment(dbContext, whItemDispatched.Id, seller, buyer, OutboundShipmentStatus.InTransit, nowUtc);

            await dbContext.SaveChangesAsync();
            logger.LogInformation("Warehouse Fake Data Seeding completed.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Warehouse fake data seeding skipped or failed. This is safe for deployment.");
        }
    }


    private static async Task<List<WarehouseStorageLocationId>> EnsureStorageLocations(ApplicationDbContext dbContext, DateTime nowUtc, ILogger logger)
    {
        var locations = await dbContext.Set<WarehouseStorageLocation>().ToListAsync();
        if (locations.Count > 0) return locations.Select(x => x.Id).ToList();

        var seeds = new[]
        {
            new { Zone = "A", Rack = "1", Shelf = "1", Bin = "1" },
            new { Zone = "A", Rack = "2", Shelf = "1", Bin = "3" },
            new { Zone = "B", Rack = "1", Shelf = "2", Bin = "2" }
        };
        var created = new List<WarehouseStorageLocationId>();

        foreach (var seed in seeds)
        {
            var loc = WarehouseStorageLocation.Create(seed.Zone, seed.Rack, seed.Shelf, seed.Bin, nowUtc);
            dbContext.Insert(loc);
            created.Add(loc.Id);
        }

        logger.LogInformation("Seeded mock storage locations.");
        return created;
    }

    private static async Task<User> EnsureUserAsync(
        ApplicationDbContext dbContext,
        IPasswordHasher passwordHasher,
        Faker faker,
        DateTime nowUtc,
        string emailStr,
        string userNameStr,
        string[] roles,
        ILogger logger)
    {
        var email = UserEmail.Create(emailStr).Value;
        var existing = await dbContext.Set<User>().FirstOrDefaultAsync(x => x.Email == email);
        if (existing is not null) return existing;

        var user = User.Create(
            userName: UserName.Create(userNameStr).Value,
            email: email,
            now: nowUtc,
            personName: PersonName.Create("Warehouse", "Demo", emailStr),
            currency: Currency.Vnd,
            password: Password.Create(SeedPassword, passwordHasher).Value);

        user.ConfirmEmail(nowUtc);
        foreach (var role in roles)
        {
            user.AssignRole(role, nowUtc);
        }

        dbContext.Set<User>().Add(user);
        logger.LogInformation("Seeded dedicated warehouse user: {Email}", emailStr);
        return user;
    }

    private static async Task<Category> EnsureCategoryAsync(ApplicationDbContext dbContext, DateTime nowUtc, ILogger logger)
    {
        var name = "Warehouse Demo Category";
        var existing = await dbContext.Set<Category>().FirstOrDefaultAsync(x => x.Name == name);
        if (existing is not null) return existing;

        var category = Category.Create(
            name: name,
            slug: Slug.Create("warehouse-test").Value,
            nowUtc: nowUtc,
            description: "Dedicated category for warehouse flow testing.");

        dbContext.Set<Category>().Add(category);
        logger.LogInformation("Seeded dedicated warehouse category.");
        return category;
    }

    private static async Task<Item> FindOrCreateItem(ApplicationDbContext dbContext, User seller, CategoryId categoryId, string title, DateTime nowUtc)
    {
        var existing = await dbContext.Set<Item>().FirstOrDefaultAsync(x => x.SellerId == seller.Id && x.Title.Value == title);
        if (existing is not null) return existing;

        var item = Item.Create(nowUtc, seller.Id, ItemTitle.Create(title).Value, ItemCondition.New, SeedPrefix, categoryId, 1, "{}");
        dbContext.Set<Item>().Add(item);
        return item;
    }

    private static async Task<InboundShipment> CreateInboundShipmentScenario(ApplicationDbContext dbContext, Guid itemId, User seller, InboundShipmentStatus status, DateTime nowUtc)
    {
        var existing = await dbContext.Set<InboundShipment>().FirstOrDefaultAsync(x => x.ItemId == itemId);
        if (existing is not null) return existing;

        var shipment = InboundShipment.Create(
            itemId: itemId,
            sellerId: seller.Id,
            providerCode: ShippingProviderCode.Ghn,
            clientOrderCode: "MOC-WB-" + Guid.NewGuid().ToString()[..8],
            senderName: seller.Profile?.Name?.FullName ?? "Seller",
            senderPhone: "0900000000",
            senderAddress: "Address",
            senderWard: "Ward",
            senderDistrict: "District",
            senderProvince: "Province",
            dimensions: PackageDimensions.Create(500, 10, 10, 10).Value,
            now: nowUtc).Value;

        SetPrivateProperty(shipment, "Status", status);
        dbContext.Insert(shipment);
        return shipment;
    }

    private static async Task<WarehouseItem> EnsureWarehouseItem(ApplicationDbContext dbContext, Guid itemId, InboundShipmentId inboundId, WarehouseItemStatus status, DateTime nowUtc)
    {
        var existing = await dbContext.Set<WarehouseItem>().FirstOrDefaultAsync(x => x.ItemId == itemId);
        if (existing is not null) return existing;

        var whItem = WarehouseItem.Create(itemId, inboundId, nowUtc);
        SetPrivateProperty(whItem, "Status", status);
        dbContext.Insert(whItem);
        return whItem;
    }

    private static async Task<WarehouseInspection> EnsureWarehouseInspection(ApplicationDbContext dbContext, WarehouseItem whItem, InboundShipmentId inbId, Guid itemId, User admin, WarehouseInspectionDecisionStatus decision, DateTime nowUtc)
    {
        var existing = await dbContext.Set<WarehouseInspection>().FirstOrDefaultAsync(x => x.WarehouseItemId == whItem.Id);
        if (existing is not null) return existing;

        var mockEvidenceJson = "[{\"publicId\":\"seed-evidence-1\",\"folder\":\"seed/inspections\",\"secureUrl\":\"https://picsum.photos/seed/evidence/800/600\",\"fileName\":\"evidence.jpg\",\"bytes\":50000,\"format\":\"jpg\",\"width\":800,\"height\":600}]";
        var inspection = WarehouseInspection.Create(whItem.Id, inbId, itemId, ItemCondition.New, WarehouseItemCondition.New, InspectionEvidence.From(mockEvidenceJson), admin.Id, nowUtc).Value;
        SetPrivateProperty(inspection, "DecisionStatus", decision);
        dbContext.Insert(inspection);
        return inspection;
    }

    private static async Task<OutboundShipment> EnsureOutboundShipment(ApplicationDbContext dbContext, WarehouseItemId whItemId, User sender, User recipient, OutboundShipmentStatus status, DateTime nowUtc)
    {
        var existing = await dbContext.Set<OutboundShipment>().FirstOrDefaultAsync(x => x.WarehouseItemId == whItemId);
        if (existing is not null) return existing;

        // Satisfy EF Core Foreign Key constraint and Owned Types constraints by seeding a proper Order
        var order = OIO.Domain.Context.OrderContext.Aggregates.Orders.Order.Create(
            OIO.Domain.Context.AuctionContext.ValueObjects.Ids.AuctionId.From(Guid.NewGuid()),
            recipient.Id,
            sender.Id,
            OIO.Domain.Context.OrderContext.ValueObjects.ShippingSnapshot.Create("Recipient", "0900000000", "Add", "Ward", "Dist", "Province"),
            null,
            null,
            OIO.Domain.Context.OrderContext.ValueObjects.OrderPricing.Create(OIO.Domain.Context.Shared.ValueObjects.Money.Create(100, "VND").Value, 0, 0, 0, OIO.Domain.Context.Shared.ValueObjects.Money.Create(100, "VND").Value),
            "VND",
            nowUtc.AddDays(1),
            nowUtc).Value;

        var orderId = order.Id;
        order.MarkAsPaid(nowUtc);
        dbContext.Insert(order);

        var dim = PackageDimensions.Create(500, 10, 10, 10).Value;
        var shipment = OutboundShipment.Create(
            orderId: orderId,
            warehouseItemId: whItemId,
            shipmentMode: OutboundShipmentMode.PlatformManaged,
            providerCode: ShippingProviderCode.Ghn,
            clientOrderCode: "MOC-OUT-" + Guid.NewGuid().ToString()[..8],
            dimensions: dim,
            now: nowUtc);

        SetPrivateProperty(shipment, "Status", status);
        dbContext.Insert(shipment);
        return shipment;
    }

    private static void SetPrivateProperty<T>(T target, string propertyName, object value)
    {
        target!.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(target, value);
    }
}

