using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.UserContext.Aggregates.Roles;

public sealed class Role : IEntity, IModifiedAtEntity
{
    private readonly List<RolePermission> _rolePermissions = [];

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private Role() {}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    public string Name { get; private set; }
    
    public int Level { get; private set; }
    
    public DateTime? ModifiedAt { get; private set;  }
    
    public IReadOnlyList<RolePermission> RolePermissions => _rolePermissions;

    private Role(
        string name,
        int level = 0)
    {
        if (level < 0)
            throw new ArgumentOutOfRangeException(nameof(level), "Role level must be greater than or equal to 0.");

        Name = name;
        Level = level;
    }

    public static Role Create(
        string name,
        int level = 0)
    {
        return new Role(name, level);
    }

    public void TogglePermission(
        string permissionCode,
        bool isActive,
        DateTime nowUtc)
    {
        
        var rp = _rolePermissions.FirstOrDefault(x => x.PermissionCode == permissionCode);
        
        if (rp is null)
        {
            if (isActive)
            {
                _rolePermissions.Add(new RolePermission(Name, permissionCode));
            }
            return;
        }

        ModifiedAt = nowUtc;
        
        if (isActive)
        {
            rp.Activate(nowUtc);
            return;
        }

        rp.Deactivate(nowUtc);
    }
}