using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Settings;
using OIO.Domain.AppDefinitions;
using OIO.Domain.Context.Shared.Entities;
using OIO.Domain.Context.Shared.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.Aggregates.Roles;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Services;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Infrastructure.Settings;
using OIO.Infrastructure.Settings.Apps;

namespace OIO.Infrastructure.Persistence.Seed;

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
            await SeedSystemSettingsAsync(scope.ServiceProvider);
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
        var existing = await dbContext.Set<RolePermission>()
            .AsNoTracking()
            .Select(x => new Tuple<PermissionId, RoleId>(x.PermissionId, x.RoleId))
            .ToHashSetAsync();
        var newRoles = App.RolePermissions.All
            .Where(x => !existing.Contains(new Tuple<PermissionId, RoleId>(x.PermissionId, x.RoleId)))
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
    
    private static async Task SeedSystemSettingsAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<AppInfoOptions>>().Value;
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        var seeds = new Dictionary<string, (object Value, string Type, string Description)>
        {
            // Auction
            [SettingKeys.AuctionMaxExtensions] = (
                options.AuctionDefaults.MaxExtensionsPerAuction, "Int32",
                "Maximum number of anti-sniping extensions per auction"),
            [SettingKeys.AuctionExtensionThreshold] = (
                options.AuctionDefaults.ExtensionThresholdMinutes, "TimeSpan",
                "Time before end when a bid triggers extension"),
            [SettingKeys.AuctionMaxDuration] = (
                options.AuctionDefaults.MaxDuration, "TimeSpan",
                "Maximum allowed auction duration"),
            [SettingKeys.AuctionMinDuration] = (
                options.AuctionDefaults.MinDuration, "TimeSpan",
                "Minimum allowed auction duration"),

            // Items
            [SettingKeys.ItemMaxQuestions] = (
                options.ItemDefaults.MaxQuestionsPerItem, "Int32",
                "Max questions per item"),

            // Media
            [SettingKeys.MediaSignatureExpiration] = (
                options.MediaDefaults.SignatureExpirationMinutes, "Int32",
                "Upload signature TTL in minutes"),
            [SettingKeys.MediaOrphanExpiration] = (
                options.MediaDefaults.OrphanExpirationMinutes, "Int32",
                "Orphan upload cleanup threshold in minutes"),
            [SettingKeys.MediaLinkedRetention] = (
                options.MediaDefaults.LinkedRecordRetentionDays, "Int32",
                "Days to keep linked upload records"),
            [SettingKeys.MediaCleanupInterval] = (
                options.MediaDefaults.CleanupIntervalMinutes, "Int32",
                "Media cleanup job interval in minutes"),
            [SettingKeys.MediaUploadContexts] = (
                options.MediaDefaults.UploadContexts, "List<UploadContextOption>",
                "Upload context configurations"),

            // Auth
            [SettingKeys.AuthPasswordResetExpiration] = (
                options.AuthDefaults.PasswordResetTokenExpirationMinutes, "Int32",
                "Password reset token TTL in minutes"),
            [SettingKeys.AuthResendEmailCooldown] = (
                options.AuthDefaults.ResendEmailCooldownSeconds, "Int32",
                "Cooldown between resend email requests in seconds"),
            [SettingKeys.AuthMaxPasswordResetAttempts] = (
                options.AuthDefaults.MaxPasswordResetAttemptsPerHour, "Int32",
                "Max password reset attempts per hour"),
        };

        foreach (var (key, (value, type, description)) in seeds)
        {
            var settingId = SystemSettingId.From(key);
            var exists = await dbContext.Set<SystemSetting>()
                .AnyAsync(s => s.Id == settingId);

            if (!exists)
            {
                var json = JsonSerializer.Serialize(value);
                var setting = SystemSetting.Create(clock.UtcNow, key, json, type, description);
                dbContext.Set<SystemSetting>().Add(setting);

                logger.LogInformation("Seeded setting: {Key} = {Value}", key, json);
            }
        }

        await dbContext.SaveChangesAsync();
    }
}