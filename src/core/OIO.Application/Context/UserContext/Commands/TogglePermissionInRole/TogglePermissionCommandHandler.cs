using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.AppDefinitions;
using OIO.Domain.Context.UserContext.Aggregates.Roles;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.TogglePermissionInRole;

internal sealed class TogglePermissionCommandHandler : ICommandHandler<TogglePermissionCommand>
{
    private readonly IDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPermissionService _permissionService;
    private readonly IClock _clock;
    
    public TogglePermissionCommandHandler(
        IDbContext dbContext,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork,
        IPermissionService permissionService,
        IClock clock)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _permissionService = permissionService;
        _clock = clock;
    }
    
    public async Task<UnitResult<Error>> Handle(TogglePermissionCommand request, CancellationToken cancellationToken)
    {
        var nowUtc =  _clock.UtcNow;
        
        var actorId = _currentUser.UserId;
        
        var actor = await _dbContext.GetByIdAsync<User, UserId>(
            actorId,
            queryBuilder: q => q
                .Include(x => x.Roles)
                .ThenInclude(x => x.Role),
            cancellationToken: cancellationToken);
        
        if (actor is null)
            return UserErrors.User.NotFound(actorId);
        
        App.Roles.Definitions.All.TryGetValue(request.Role, out var targetRole);
        
        if (targetRole is null)
            return RoleErrors.Role.NotFound(request.Role);
        
        App.Permissions.Definitions.All.TryGetValue(request.Permission, out var targetPermission);
        
        if (targetPermission is null)
            return UserErrors.Permission.NotFound(request.Permission);

        if (actor.GetMaxRoleLevel() <= targetRole.Level || targetRole.Name == App.Roles.Definitions.Admin.Name)
        {
            return UserErrors.User.InsufficientRoleLevel;
        }
        
        if(App.Permissions.Catalogs.CriticalPermissions.Contains(targetPermission.Code) &&
           actor.GetMaxRoleLevel() < App.Roles.Definitions.Admin.Level)
            return UserErrors.Auth.InsufficientPermissions;
        
        var roleInDb = await _dbContext.Set<Role>()
            .Include(x => x.RolePermissions)
            .FirstOrDefaultAsync(r => r.Name == targetRole.Name, cancellationToken);
        
        if (roleInDb is null)            
            return RoleErrors.Role.NotFound(request.Role);
        
        roleInDb.TogglePermission(request.Permission, request.IsActive, nowUtc);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var affectedUserIds = await _dbContext.Set<User>()
            .AsNoTracking()
            .Where(u => u.Roles.Any(r => r.RoleName == request.Role))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        foreach (var userId in affectedUserIds)
        {
            await _permissionService.InvalidatePermissionsCacheAsync(userId, cancellationToken);
        }
        
        return UnitResult.Success<Error>();
    }
}