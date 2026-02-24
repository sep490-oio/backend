using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.UserContext.Services;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.UserContext.Commands.GrantPermission;

internal sealed class GrantPermissionCommandHandler
    : ICommandHandler<GrantPermissionCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPermissionService _permissionService;
    private readonly IClock _clock;

    public GrantPermissionCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IPermissionService permissionService,
        IClock clock)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _permissionService = permissionService;
        _clock = clock;
    }

    public async Task<UnitResult<Error>> Handle(
        GrantPermissionCommand request,
        CancellationToken cancellationToken)
    {
        var nowUtc =  _clock.UtcNow;
        
        var userId = UserId.From(request.UserId);
        var permissionId = PermissionId.From(request.PermissionId);

        var user = await _dbContext.GetByIdAsync<User, UserId>(userId,
            queryBuilder: query => query.Include(x => x.Permissions),
            cancellationToken: cancellationToken);
        if (user is null)
            return UserErrors.User.NotFound(userId);
        
        var grantPermissionResult = user.GrantPermission(permissionId, nowUtc);

        if (grantPermissionResult.IsFailure)
        {
            return grantPermissionResult.Error;
        }
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _permissionService.InvalidatePermissionsCacheAsync(userId, cancellationToken);
        
        return grantPermissionResult;
    }
}