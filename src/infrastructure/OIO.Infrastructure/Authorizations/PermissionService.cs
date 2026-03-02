using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using OIO.Application.Abstractions.Data;
using OIO.Application.UserContext.Services;
using OIO.Domain.Context.UserContext.Aggregates.Roles;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Authorizations;

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

    private static readonly HybridCacheEntryOptions HybridCacheEntryOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(30),
        LocalCacheExpiration = TimeSpan.FromMinutes(5)
    };

    private static string BuildCacheKey(UserId userId) => $"{CacheKeyPrefix}{userId:N}";
    
    public async Task<HashSet<string>> GetPermissionsAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrCreateAsync(
            BuildCacheKey(userId),
            async token => await GetPermissionsByAccountIdAsync(userId, token),
            HybridCacheEntryOptions,
            cancellationToken: cancellationToken
        );
    }
    
    public async Task InvalidatePermissionsCacheAsync(UserId userId, CancellationToken cancellationToken = default) =>
        await _cache.RemoveAsync(BuildCacheKey(userId), cancellationToken);
    
    private async Task<HashSet<string>> GetPermissionsByAccountIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        //Get permissions from the Roles that the Account owns.
        var rolePermissions = _dbContext.Set<User>()
            .AsNoTrackingWithIdentityResolution()
            .Where(user => user.Id == userId) //get user with specific id
            .SelectMany(user => user.Roles) //flat list user role of the user find above
            .SelectMany(userRole => _dbContext.Set<RolePermission>()
                .AsNoTrackingWithIdentityResolution()
                .Where(rolePermission => rolePermission.RoleId == userRole.RoleId && rolePermission.IsActive) // find each Role Permission with every roleId in the UserRole
                .SelectMany(rolePermission => _dbContext.Set<Permission>()
                    .AsNoTrackingWithIdentityResolution()
                    .Where(permission => permission.Id == rolePermission.PermissionId) //find each Permission with the permission in the permission in RolePermission
                    .Select(permission => permission.PermissionCode)))
            .Distinct();

        //Get direct permissions for the Account (Based on the AccountPermission table with IsAllowed added).
        var directPermissions = _dbContext.Set<UserPermission>()
            .AsNoTrackingWithIdentityResolution()
            .Where(userPermission => userPermission.UserId == userId && userPermission.IsAllowed)
            .SelectMany(userPermission => _dbContext.Set<Permission>()
                .AsNoTrackingWithIdentityResolution()
                .Where(permission => permission.Id == userPermission.PermissionId)
                .Select(permission => permission.PermissionCode)
            );

        //Merge both lists (UNION automatically removes duplicates).
        var allPermissions = await rolePermissions
            .Union(directPermissions)
            .ToListAsync(cancellationToken);

        //Deny handler (IsAllowed = false to deny permission)
        var deniedPermissions = await _dbContext.Set<UserPermission>().AsNoTrackingWithIdentityResolution()
            .Where(userPermission => userPermission.UserId == userId && !userPermission.IsAllowed)
            .SelectMany(userPermission => _dbContext.Set<Permission>()
                .AsNoTrackingWithIdentityResolution()
                .Where(permission => permission.Id == userPermission.PermissionId)
                .Select(permission => permission.PermissionCode))
            .ToListAsync(cancellationToken);
        
        return allPermissions
            .Except(deniedPermissions)
            .ToHashSet();
    }
}