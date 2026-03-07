using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.AppDefinitions;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.RevokeRole;

internal sealed class RevokeRoleCommandHandler
    : ICommandHandler<RevokeRoleCommand>
{
    private readonly IDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPermissionService _permissionService;
    private readonly IClock _clock;

    public RevokeRoleCommandHandler(
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
        RevokeRoleCommand request,
        CancellationToken cancellationToken)
    {
        var nowUtc =  _clock.UtcNow;
        
        var actorId = _currentUser.UserId;
        var targetUserId = UserId.From(request.UserId);
        var targetRoleId = RoleId.From(request.RoleId);
        
        if (actorId == targetUserId)
            return UserErrors.User.CannotRevokeRoleYourself;
        
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
            queryBuilder: q => q.Include(x => x.Roles).ThenInclude(x => x.Role),
            cancellationToken: cancellationToken);

        if (targetUser is null)
            return UserErrors.User.NotFound(targetUserId);
        
        var targetRole = App.Roles.Definitions.All.FirstOrDefault(x => x.Id == targetRoleId);
        
        if (targetRole is null)
            return RoleErrors.Role.NotFound(targetRoleId);
        
        if (!actor.CanManage(targetUser))
            return UserErrors.User.InsufficientRoleLevel;
        
        if (actor.GetMaxRoleLevel() <= targetRole.Level)
            return RoleErrors.Role.CannotRevokeHigherOrEqualRole;

        if (targetRoleId == App.Roles.Definitions.Admin.Id)
        {
            var numberOfAdmin = await _dbContext.Set<User>()
                .Where(x => x.Roles.Any(r => r.RoleId == App.Roles.Definitions.Admin.Id) && x.Id != targetUserId)
                .CountAsync(cancellationToken);

            if (numberOfAdmin == 0)
            {
                return UserErrors.User.CannotRevokeLastAdminUser;
            }
        }
        
        var removeRoleResult = targetUser.RevokeRole(targetRoleId, nowUtc);

        if (removeRoleResult.IsFailure)
        {
            return removeRoleResult;
        }
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _permissionService.InvalidatePermissionsCacheAsync(targetUserId, cancellationToken);

        return removeRoleResult;
    }
}