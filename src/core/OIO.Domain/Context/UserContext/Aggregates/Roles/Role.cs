using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.UserContext.Aggregates.Roles;

public sealed class Role : AggregateRoot<RoleId>
{
    private readonly List<RolePermission> _rolePermissions = [];

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private Role() {}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    
    public string RoleName { get; private set; }

    public string NormalizedRoleName { get; private set; }
    public IReadOnlyList<RolePermission> RolePermissions => _rolePermissions;

    private Role(
        RoleId id,
        string roleName
    )
    {
        Id = id;
        RoleName = roleName;
        NormalizedRoleName = roleName.ToUpperInvariant();
    }

    public static Role Create(string roleName)
    {
        return new Role(RoleId.Create(), roleName);
    }
    
    public void AddPermission(PermissionId permissionId)
    {
        if (_rolePermissions.Any(rp => rp.PermissionId == permissionId))
            return;

        _rolePermissions.Add(new RolePermission(Id, permissionId));
    }

    public void RemovePermission(PermissionId permissionId)
    {
        var rp = _rolePermissions.FirstOrDefault(x => x.PermissionId == permissionId);
        if (rp is not null)
            _rolePermissions.Remove(rp);
    }

    public void TogglePermission(PermissionId permissionId, bool isActive)
    {
        var rp = _rolePermissions.FirstOrDefault(x => x.PermissionId == permissionId);
        rp?.SetActive(isActive);
    }
}

public sealed class RolePermission : IEntity
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private RolePermission() {}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    
    public RoleId RoleId { get; private set; }

    public PermissionId PermissionId { get; private set; }

    public bool IsActive { get; private set; }
    
    public Role Role { get; private set; }
    public Permission Permission { get; private set; }
    
    internal RolePermission(RoleId roleId, PermissionId permissionId)
    {
        RoleId = roleId;
        PermissionId = permissionId;
        IsActive = true;
    }

    internal void SetActive(bool isActive) => IsActive = isActive;
}