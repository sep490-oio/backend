using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.UserContext.Aggregates.Roles;

public sealed class Permission : BaseEntity<PermissionId>
{
    private readonly List<RolePermission> _rolePermissions = [];
    
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private Permission() {}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    
    public string PermissionCode { get; private set; }

    public string? NormalizedPermissionCode { get; private set; }

    private Permission(string permissionCode)
    {
        PermissionCode = permissionCode;
        NormalizedPermissionCode = permissionCode.ToUpperInvariant();
    }
    
    public IReadOnlyList<RolePermission> RolePermissions => _rolePermissions; 

    public static Permission Create(
        string permissionCode
    )
    {
        return new Permission(permissionCode);
    }
    
    public static Permission Create(
        PermissionId permissionId,
        string permissionCode
    )
    {
        return new Permission(permissionCode)
        {
            Id = permissionId
        };
    }
    internal static Permission Create(
        int id,
        string permissionCode
    )
    {
        return new Permission(permissionCode)
        {
            Id = PermissionId.From(id)
        };
    }
}