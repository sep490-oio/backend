using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OIO.Application.Abstractions.Clock;
using OIO.Domain.Context.UserContext.Aggregates.Roles;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Services;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.AppDefinitions;
using OIO.Infrastructure.Settings;

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

            // NOTE: Permission and role seeding are temporarily disabled (e.g. for testing or when using an already-seeded database).
            // Re-enable the following calls when initializing a new environment that requires full permission and role seeding.
            //await SeedPermissionsAsync(dbContext, logger);
            //await SeedRolesAsync(dbContext, logger);
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
        var permissions =  await dbContext.Set<Permission>().AsNoTrackingWithIdentityResolution().ToListAsync();
        var newPermissions = App.Permissions.Definitions.All.Except(permissions).ToList();
        if (newPermissions.Count == 0)
            return;

        await dbContext.Set<Permission>().AddRangeAsync(newPermissions);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Seeded {Count} permissions.", newPermissions.Count);
        foreach (var permission in newPermissions)
        {
            logger.LogInformation("Seeded permissions {permission}.", permission.PermissionCode);
        }
    }

    // ==================== Roles ====================
    private static async Task SeedRolesAsync(ApplicationDbContext dbContext, ILogger logger)
    {
        var roles =  await dbContext.Set<Role>().AsNoTrackingWithIdentityResolution().ToListAsync();
        var newRoles = App.Roles.Definitions.All.Except(roles).ToList();
        if (newRoles.Count == 0)
            return;

        await dbContext.Set<Role>().AddRangeAsync(newRoles);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Seeded {Count} roles.", newRoles.Count);
        foreach (var role in newRoles)
        {
            logger.LogInformation("Seeded roles {role} .", role.RoleName);
        }
    }

    // ==================== Role-Permission Mapping ====================
    private static async Task AssignPermissionsToRolesAsync(
        ApplicationDbContext dbContext, ILogger logger)
    {
        var rolePermissions =  await dbContext.Set<RolePermission>()
            .AsNoTrackingWithIdentityResolution()
            .ToListAsync();
        
        var newRoles = App.RolePermissions.All
            .Where(x => !rolePermissions.Any(y => y.PermissionId == x.PermissionId && y.RoleId == x.RoleId))
            .ToList();
        
        if (newRoles.Count == 0)
            return;

            
        await dbContext.Set<RolePermission>().AddRangeAsync(newRoles);

        await dbContext.SaveChangesAsync();

        logger.LogInformation("Assigned permissions to all roles.");
        logger.LogInformation("Assigned {Count} permissions to all roles.", newRoles.Count);

        foreach (var rolePermission in newRoles)
        {
            logger.LogInformation("Assigned {permission} to {role}.", rolePermission.Permission.PermissionCode, rolePermission.Role.RoleName);
        }
        
    }

    // ==================== Admin User ====================
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

        dbContext.Set<User>().Add(admin);

        // Clear domain events raised during seeding (we don't want to publish them)
        admin.ClearDomainEvents();

        await dbContext.SaveChangesAsync();

        logger.LogInformation(
            "Seeded admin user: {Email} (ID: {UserId})", email.Value, admin.Id);
    }
}