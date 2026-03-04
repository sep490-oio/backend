using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.UserContext.Services;
using OIO.Domain.AppDefinitions;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.UserContext.Commands.GrantPermission;

internal sealed class GrantPermissionCommandHandler
    : ICommandHandler<GrantPermissionCommand>
{
    private readonly IDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPermissionService _permissionService;
    private readonly IClock _clock;

    public GrantPermissionCommandHandler(
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

    public async Task<UnitResult<Error>> Handle(
        GrantPermissionCommand request,
        CancellationToken cancellationToken)
    {
        var nowUtc =  _clock.UtcNow;
        
        var actorId = _currentUser.UserId;
        var targetUserId = UserId.From(request.UserId);
        var targetPermissionId = PermissionId.From(request.PermissionId);
        
        if (actorId == targetUserId)
            return UserErrors.User.CannotManageOwnPermissions;
        
        var actor = await _dbContext.GetByIdAsync<User, UserId>(
            actorId,
            queryBuilder: q => q
                .Include(x => x.Roles)
                .ThenInclude(x => x.Role),
            cancellationToken: cancellationToken);
        
        if (actor is null)
            return UserErrors.User.NotFound(actorId);

        //load target user + roles
        var targetUser = await _dbContext.GetByIdAsync<User, UserId>(
            targetUserId,
            queryBuilder: q => q
                .Include(x => x.Roles)
                .ThenInclude(x => x.Role)
                .Include(x => x.Permissions),
            cancellationToken: cancellationToken);

        if (targetUser is null)
            return UserErrors.User.NotFound(targetUserId);
        
        if (!actor.CanManage(targetUser))
            return UserErrors.User.InsufficientRoleLevel;
        
        var permission = App.Permissions.Definitions.All.FirstOrDefault(x => x.Id == targetPermissionId);

        if (permission is null)
        {
            return UserErrors.Permission.NotFound(targetPermissionId);
        }
        
        if(App.Permissions.Catalogs.CriticalPermissions.Contains(permission.PermissionCode) &&
           actor.GetMaxRoleLevel() < App.Roles.Definitions.Admin.Level)
            return UserErrors.Auth.InsufficientPermissions;
        
        var result = targetUser.GrantPermission(targetPermissionId, nowUtc);

        if (result.IsFailure)
        {
            return result.Error;
        }
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _permissionService.InvalidatePermissionsCacheAsync(targetUserId, cancellationToken);
        
        return result;
    }
}