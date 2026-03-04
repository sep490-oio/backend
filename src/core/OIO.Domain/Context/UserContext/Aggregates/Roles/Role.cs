using CSharpFunctionalExtensions;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.UserContext.Aggregates.Roles;

public sealed class Role : AggregateRoot<RoleId>, IModifiedAtEntity
{
    private readonly List<RolePermission> _rolePermissions = [];

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private Role() {}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    public string RoleName { get; private set; }

    public string NormalizedRoleName { get; private set; }

    public int Level { get; private set; }
    
    public DateTime? ModifiedAt { get; private set;  }
    
    public IReadOnlyList<RolePermission> RolePermissions => _rolePermissions;

    private Role(
        string roleName,
        int level = 0)
    {
        if (level < 0)
            throw new ArgumentOutOfRangeException(nameof(level), "Role level must be greater than or equal to 0.");

        RoleName = roleName.Trim();
        NormalizedRoleName = roleName.Trim().ToUpperInvariant();
        Level = level;
    }

    public static Role Create(
        string roleName,
        int level = 0)
    {
        return new Role(roleName, level);
    }

    public static Role Create(
        RoleId roleId,
        string roleName,
        int level = 0)
    {
        return new Role(roleName, level)
        {
            Id = roleId,
        };
    }

    public void TogglePermission(
        PermissionId permissionId,
        bool isActive,
        DateTime nowUtc)
    {
        
        var rp = _rolePermissions.FirstOrDefault(x => x.PermissionId == permissionId);
        
        if (rp is null)
        {
            if (isActive)
            {
                _rolePermissions.Add(new RolePermission(Id, permissionId));
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