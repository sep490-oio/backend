using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.UserContext.Aggregates.Roles;

public sealed class Permission : Entity<PermissionId>
{
    private readonly List<RolePermission> _rolePermissions = [];
    
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private Permission() {}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    
    public string PermissionCode { get; private set; }

    public string? NormalizedPermissionCode { get; private set; }

    private Permission(PermissionId id, string permissionCode)
    {
        Id = id;
        PermissionCode = permissionCode;
        NormalizedPermissionCode = permissionCode.ToUpperInvariant();
    }
    
    public IReadOnlyList<RolePermission> RolePermissions => _rolePermissions; 

    public static Permission Create(
        string permissionCode
    )
    {
        return new Permission(PermissionId.Create(), permissionCode);
    }
}