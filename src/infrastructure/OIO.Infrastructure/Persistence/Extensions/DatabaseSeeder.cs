using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Domain.Constants;
using OIO.Domain.Constants.AppPermissions;
using OIO.Domain.Context.UserContext.Aggregates.Roles;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Services;
using OIO.Domain.Context.UserContext.ValueObjects;

namespace OIO.Infrastructure.Persistence.Extensions;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            logger.LogInformation("Starting database seeding...");

            await SeedPermissionsAsync(dbContext, logger);
            await SeedRolesAsync(dbContext, logger);
            await AssignPermissionsToRolesAsync(dbContext, logger);
            await SeedAdminUserAsync(dbContext, scope.ServiceProvider, logger);

            logger.LogInformation("Database seeding completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    // ==================== Permissions ====================
    private static async Task SeedPermissionsAsync(ApplicationDbContext dbContext, ILogger logger)
    {
        if (await dbContext.Set<Permission>().AnyAsync())
            return;

        var permissions = AppPermission.All.Select(Permission.Create).ToList();

        dbContext.Set<Permission>().AddRange(permissions);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Seeded {Count} permissions.", permissions.Count);
    }

    // ==================== Roles ====================
    private static async Task SeedRolesAsync(ApplicationDbContext dbContext, ILogger logger)
    {
        if (await dbContext.Set<Role>().AnyAsync())
            return;

        var roles = AppRole.All.Select(Role.Create).ToList();

        dbContext.Set<Role>().AddRange(roles);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Seeded {Count} roles.", roles.Count);
    }

    // ==================== Role-Permission Mapping ====================
    private static async Task AssignPermissionsToRolesAsync(
        ApplicationDbContext dbContext, ILogger logger)
    {
        if (await dbContext.Set<RolePermission>().AnyAsync())
            return;

        var roles = await dbContext.Set<Role>().ToListAsync();
        var permissions = await dbContext.Set<Permission>().ToListAsync();

        var permissionLookup = permissions.ToDictionary(
            p => p.PermissionCode, p => p.Id, StringComparer.OrdinalIgnoreCase);

        // Helper to find role
        Role FindRole(string name) =>
            roles.First(r => r.RoleName.Equals(name, StringComparison.OrdinalIgnoreCase));

        // Helper to assign permissions to a role
        void Assign(Role role, params string[] permissionCodes)
        {
            foreach (var code in permissionCodes)
            {
                if (permissionLookup.TryGetValue(code, out var permId))
                    role.AddPermission(permId);
            }
        }

        // ----- Admin: Full access -----
        var admin = FindRole(AppRole.Admin);
        Assign(admin, permissions.Select(p => p.PermissionCode).ToArray());

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

        await dbContext.SaveChangesAsync();

        logger.LogInformation("Assigned permissions to all roles.");
    }

    // ==================== Admin User ====================
    private static async Task SeedAdminUserAsync(
        ApplicationDbContext dbContext,
        IServiceProvider serviceProvider,
        ILogger logger)
    {
        if (await dbContext.Set<User>().AnyAsync())
            return;

        var passwordHasher = serviceProvider.GetRequiredService<IPasswordHasher>();
        var clock = serviceProvider.GetRequiredService<IClock>();

        var email = UserEmail.Create("admin@oio.com");
        var passwordHash = Password.Create("Admin@123456", passwordHasher);
        var userName = UserName.Create("admin");
        var admin = User.Create(
            userName: userName.Value,
            email: email.Value,
            now: clock.UtcNow,
            password: passwordHash.Value
        );

        admin.ConfirmEmail(clock.UtcNow);
        admin.UpdateProfile(
            firstName: FirstName.Create("System").Value,
            lastName: LastName.Create("Administrator").Value,
            displayName: DisplayName.Create("Admin").Value,
            now: clock.UtcNow);

        // Assign Admin role
        var adminRole = await dbContext.Set<Role>()
            .FirstAsync(r => r.NormalizedRoleName == AppRole.Admin.ToUpper());

        admin.AssignRole(adminRole.Id, clock.UtcNow);

        dbContext.Set<User>().Add(admin);

        // Clear domain events raised during seeding (we don't want to publish them)
        admin.ClearDomainEvents();

        await dbContext.SaveChangesAsync();

        logger.LogInformation(
            "Seeded admin user: {Email} (ID: {UserId})", email.Value, admin.Id);
    }
}