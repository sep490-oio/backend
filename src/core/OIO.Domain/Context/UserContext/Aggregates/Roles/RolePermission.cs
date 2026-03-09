using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.UserContext.Aggregates.Roles;

public sealed class RolePermission : IEntity, IModifiedAtEntity
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private RolePermission() {}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    
    public string RoleName { get; private set; }

    public string PermissionCode { get; private set; }

    public bool IsActive { get; private set; }

    public Role Role { get; private set; } = null!;
    public Permission Permission { get; private set; } = null!;
    
    
    public DateTime? ModifiedAt { get; private set; }
    
    public RolePermission(string roleName, string permissionCode)
    {
        RoleName = roleName;
        PermissionCode = permissionCode;
        IsActive = true;
    }
    
    internal RolePermission(Role role, Permission permission)
    {
        RoleName = role.Name;
        PermissionCode = permission.Code;
        Role = role;
        Permission = permission;
        IsActive = true;
    }

    internal void Activate(DateTime nowUtc)
    {
        IsActive = true;
        ModifiedAt = nowUtc;
    }

    internal void Deactivate(DateTime nowUtc)
    {
        IsActive = false;
        ModifiedAt = nowUtc;
    }
}