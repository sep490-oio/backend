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
            //await SeedAdminUserAsync(dbContext, scope.ServiceProvider, logger);
            await SeedSystemSettingsAsync(scope.ServiceProvider);
            logger.LogInformation("Database seeding completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }
    
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
        var personName = PersonName.Create(efaultAccountOptions.FirstName, efaultAccountOptions.LastName,
            efaultAccountOptions.DisplayName);
        admin.UpdateProfile(
            name: personName,
            now: clock.UtcNow);

        admin.AssignRole(App.Roles.Definitions.Admin.Name, clock.UtcNow);
        admin.AssignRole(App.Roles.Definitions.User.Name, clock.UtcNow);

        dbContext.Set<User>().Add(admin);

        // Clear domain events raised during seeding (we don't want to publish them)
        admin.ClearDomainEvents();

        await dbContext.SaveChangesAsync();

        logger.LogInformation(
            "Seeded admin user: {Email} (ID: {UserId})", email.Value, admin.Id);
    }

    private static async Task SeedRolesAsync(ApplicationDbContext dbContext, ILogger logger)
    {
        var roleInDbs =  await dbContext.Set<Role>().AsNoTrackingWithIdentityResolution().ToListAsync();
        
        var newRoles = new List<Role>();
        
        foreach (var role in App.Roles.Definitions.All)
        {
            if (roleInDbs.Any(r => r.Name == role.Key))
                continue;

            newRoles.Add(role.Value);
        }
        
        if (newRoles.Count == 0)
            return;

        await dbContext.Set<Role>().AddRangeAsync(newRoles);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Seeded {Count} roles.", newRoles.Count);
        foreach (var role in newRoles)
        {
            logger.LogInformation("Seeded roles {role} .", role.Name);
        }
    }
    
    private static async Task SeedPermissionsAsync(ApplicationDbContext dbContext, ILogger logger)
    {
        var permissionInDbs =  await dbContext.Set<Permission>().AsNoTrackingWithIdentityResolution().ToListAsync();
        
        var newPermissions = new List<Permission>();
        
        foreach (var permission in App.Permissions.Definitions.All)
        {
            if (permissionInDbs.Any(p => p.Code == permission.Key))
                continue;

            newPermissions.Add(permission.Value);
        }
        
        if (newPermissions.Count == 0)
            return;

        await dbContext.Set<Permission>().AddRangeAsync(newPermissions);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Seeded {Count} permissions.", newPermissions.Count);
        foreach (var permission in newPermissions)
        {
            logger.LogInformation("Seeded permissions {permission}.", permission.Code);
        }
    }

    // ==================== Role-Permission Mapping ====================
    private static async Task AssignPermissionsToRolesAsync(
        ApplicationDbContext dbContext, ILogger logger, CancellationToken ct = default)
    {
        // Nếu seeding nhiều bước trong cùng DbContext, nên clear để tránh “đã tracked”
        dbContext.ChangeTracker.Clear();

        // Lấy existing theo key thật (RoleId, PermissionId) cho chuẩn và nhanh
        var existingPairs = await dbContext.Set<RolePermission>()
            .AsNoTracking()
            .Select(rp => new { Role = rp.RoleName, Permission = rp.PermissionCode })
            .ToListAsync(ct);

        var existingSet = existingPairs
            .Select(x => (x.Role, x.Permission))
            .ToHashSet();

        var toInsert = new List<RolePermission>();
        var seenInsert = new HashSet<(string RoleId, string PermissionId)>(); // chống trùng trong batch

        foreach (var role in App.Roles.Catalogs.All)
        {
            var assigned = App.Roles.Catalogs.RolePermissions[role];

            foreach (var permission in assigned)
            {
                var key = (role, permission);

                // đã có trong DB hoặc đã thêm vào batch
                if (existingSet.Contains(key) || !seenInsert.Add(key))
                    continue;

                toInsert.Add(new RolePermission(role,permission));
            }
        }

        if (toInsert.Count == 0) return;

        await dbContext.AddRangeAsync(toInsert, ct);
        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("Assigned {Count} new role-permissions.", toInsert.Count);

        // Lưu ý: rolePermission.Permission / Role thường null vì bạn chỉ set FK
        // => log bằng Id/Code thay vì navigation
        foreach (var rp in toInsert)
            logger.LogInformation("Assigned PermissionId={PermissionId} to RoleId={RoleId}.", rp.PermissionCode, rp.RoleName);
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
                options.AuctionDefaults.MaxExtensionsPerAuction, options.AuctionDefaults.MaxExtensionsPerAuction.GetType().Name,
                "Maximum number of anti-sniping extensions per auction"),
            [SettingKeys.AuctionExtensionThreshold] = (
                options.AuctionDefaults.ExtensionThresholdMinutes, options.AuctionDefaults.ExtensionThresholdMinutes.GetType().Name,
                "Time before end when a bid triggers extension"),
            [SettingKeys.AuctionMaxDuration] = (
                options.AuctionDefaults.MaxDuration, options.AuctionDefaults.MaxDuration.GetType().Name,
                "Maximum allowed auction duration"),
            [SettingKeys.AuctionMinDuration] = (
                options.AuctionDefaults.MinDuration, options.AuctionDefaults.MinDuration.GetType().Name,
                "Minimum allowed auction duration"),

            // Items
            [SettingKeys.ItemMaxQuestions] = (
                options.ItemDefaults.MaxQuestionsPerItem, options.ItemDefaults.MaxQuestionsPerItem.GetType().Name,
                "Max questions per item"),

            // Media
            [SettingKeys.MediaSignatureExpiration] = (
                options.MediaDefaults.SignatureExpirationMinutes, options.MediaDefaults.SignatureExpirationMinutes.GetType().Name,
                "Upload signature TTL in minutes"),
            [SettingKeys.MediaOrphanExpiration] = (
                options.MediaDefaults.OrphanExpirationMinutes, options.MediaDefaults.OrphanExpirationMinutes.GetType().Name,
                "Orphan upload cleanup threshold in minutes"),
            [SettingKeys.MediaLinkedRetention] = (
                options.MediaDefaults.LinkedRecordRetentionDays, options.MediaDefaults.LinkedRecordRetentionDays.GetType().Name,
                "Days to keep linked upload records"),
            [SettingKeys.MediaCleanupInterval] = (
                options.MediaDefaults.CleanupIntervalMinutes, options.MediaDefaults.CleanupIntervalMinutes.GetType().Name,
                "Media cleanup job interval in minutes"),
            [SettingKeys.MediaUploadContexts] = (
                options.MediaDefaults.UploadContexts, options.MediaDefaults.UploadContexts.GetType().Name,
                "Upload context configurations"),

            // Auth
            [SettingKeys.AuthPasswordResetExpiration] = (
                options.AuthDefaults.PasswordResetTokenExpirationMinutes, options.AuthDefaults.PasswordResetTokenExpirationMinutes.GetType().Name,
                "Password reset token TTL in minutes"),
            [SettingKeys.AuthResendEmailCooldown] = (
                options.AuthDefaults.ResendEmailCooldownSeconds, options.AuthDefaults.ResendEmailCooldownSeconds.GetType().Name,
                "Cooldown between resend email requests in seconds"),
            [SettingKeys.AuthMaxPasswordResetAttempts] = (
                options.AuthDefaults.MaxPasswordResetAttemptsPerHour, options.AuthDefaults.MaxPasswordResetAttemptsPerHour.GetType().Name,
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