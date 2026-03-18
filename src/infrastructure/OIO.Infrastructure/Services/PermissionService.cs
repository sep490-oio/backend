using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.UserContext.Aggregates.Roles;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Services;

public class PermissionService : IPermissionService
{
    private readonly IDbContext _dbContext;
    private readonly HybridCache _cache; 
    
    public PermissionService(IDbContext dbContext, HybridCache cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    private const string CacheKeyPrefix = "auth:permissions:user:";
    private const string CacheTag = "auth:permissions:user";

    private static readonly HybridCacheEntryOptions HybridCacheEntryOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(30),
        LocalCacheExpiration = TimeSpan.FromMinutes(5)
    };

    private static string BuildCacheKey(UserId userId) => $"{CacheKeyPrefix}{userId:N}";
    
    public async Task<HashSet<string>> GetPermissionsAsync(
        UserId userId, 
        CancellationToken cancellationToken = default)
    {
        var cacheKey = BuildCacheKey(userId);
        return await _cache.GetOrCreateAsync(
            cacheKey,
            async token => await GetPermissionsByUserIdAsync(userId, token),
            HybridCacheEntryOptions,
            tags: [CacheTag, cacheKey],
            cancellationToken: cancellationToken
        );
    }

    public async Task InvalidatePermissionsCacheAsync(
        UserId? userId = null,
        CancellationToken cancellationToken = default)
    {
        var tag = userId.HasValue ? BuildCacheKey(userId.Value) : CacheTag;
        await _cache.RemoveByTagAsync(tag, cancellationToken);
    }
    
    // private async Task<HashSet<string>> GetPermissionsByAccountIdAsync(UserId userId, CancellationToken cancellationToken = default)
    // {
    //     //Get permissions from the Roles that the Account owns.
    //     var rolePermissions = _dbContext.Set<User>()
    //         .AsNoTrackingWithIdentityResolution()
    //         .Where(user => user.Id == userId) //get user with specific id
    //         .SelectMany(user => user.Roles) //flat list user role of the user find above
    //         .SelectMany(userRole => _dbContext.Set<RolePermission>()
    //             .AsNoTrackingWithIdentityResolution()
    //             .Where(rolePermission => rolePermission.RoleId == userRole.RoleId && rolePermission.IsActive) // find each Role Permission with every roleId in the UserRole
    //             .SelectMany(rolePermission => _dbContext.Set<Permission>()
    //                 .AsNoTrackingWithIdentityResolution()
    //                 .Where(permission => permission.Id == rolePermission.PermissionId) //find each Permission with the permission in the permission in RolePermission
    //                 .Select(permission => permission.Id.Value)))
    //         .Distinct();
    //
    //     //Get direct permissions for the Account (Based on the AccountPermission table with IsAllowed added).
    //     var directPermissions = _dbContext.Set<UserPermission>()
    //         .AsNoTrackingWithIdentityResolution()
    //         .Where(userPermission => userPermission.UserId == userId && userPermission.IsAllowed)
    //         .SelectMany(userPermission => _dbContext.Set<Permission>()
    //             .AsNoTrackingWithIdentityResolution()
    //             .Where(permission => permission.Id == userPermission.PermissionId)
    //             .Select(permission => permission.Id.Value)
    //         );
    //
    //     //Merge both lists (UNION automatically removes duplicates).
    //     var allPermissions = await rolePermissions
    //         .Union(directPermissions)
    //         .ToListAsync(cancellationToken);
    //     
    //    
    //
    //     //Deny handler (IsAllowed = false to deny permission)
    //     var deniedPermissions = await _dbContext.Set<UserPermission>().AsNoTrackingWithIdentityResolution()
    //         .Where(userPermission => userPermission.UserId == userId && !userPermission.IsAllowed)
    //         .SelectMany(userPermission => _dbContext.Set<Permission>()
    //             .AsNoTrackingWithIdentityResolution()
    //             .Where(permission => permission.Id == userPermission.PermissionId)
    //             .Select(permission => permission.Id.Value))
    //         .ToListAsync(cancellationToken);
    //     
    //     return allPermissions
    //         .Except(deniedPermissions)
    //         .ToHashSet();
    // }

    private async Task<HashSet<string>> GetPermissionsByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        var permissionIdsOfRole  = _dbContext.Set<User>()
            .AsNoTrackingWithIdentityResolution()
            .Where(user => user.Id == userId)
            .SelectMany(user => user.Roles)
            .Select(userRole => userRole.Role)
            .SelectMany(role => role.RolePermissions)
            .Where(rolePermission => rolePermission.IsActive)
            .Select(rolePermission => rolePermission.PermissionCode)
            .Distinct();
        
        var directPermissionIds = _dbContext.Set<User>()
            .AsNoTrackingWithIdentityResolution()
            .Where(user => user.Id == userId)
            .SelectMany(user => user.Permissions)
            .Where(userPermission => userPermission.IsAllowed)
            .Select(userPermission => userPermission.PermissionCode);
        
        
        var deniedPermissionIds = await _dbContext.Set<User>()
            .AsNoTrackingWithIdentityResolution()
            .Where(user => user.Id == userId)
            .SelectMany(user => user.Permissions)
            .Where(userPermission => !userPermission.IsAllowed)
            .Select(userPermission => userPermission.PermissionCode)
            .ToListAsync(cancellationToken);
        
        var effectivePermissionIds = await permissionIdsOfRole
            .Union(directPermissionIds)
            .Except(deniedPermissionIds)
            .ToListAsync(cancellationToken);

        return effectivePermissionIds
            .ToHashSet();
    }
}