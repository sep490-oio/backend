using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.UnlockUser;

internal sealed class UnlockUserCommandHandler
    : ICommandHandler<UnlockUserCommand>
{
    private readonly IDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UnlockUserCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<UnitResult<Error>> Handle(
        UnlockUserCommand request,
        CancellationToken cancellationToken)
    {
        var nowUtc = _clock.UtcNow;
       
        var actorId = _currentUser.UserId;
        var targetUserId = UserId.From(request.UserId);
        
        if (actorId == targetUserId)
            return UserErrors.User.CannotUnlockYourself;
        
        var actor = await _dbContext.GetByIdAsync<User, UserId>(
            actorId,
            query => query
                .Include(x => x.Roles)
                .ThenInclude(x => x.Role),
            cancellationToken: cancellationToken);
        
        if (actor is null)
            return UserErrors.User.NotFound(actorId);
        
        var targetUser = await _dbContext.GetByIdAsync<User, UserId>(
            targetUserId, 
            query => query
                .Include(x => x.Roles)
                .ThenInclude(x => x.Role),
            cancellationToken: cancellationToken);
        
        if (targetUser is null)
            return UserErrors.User.NotFound(targetUserId);
        
        if (!actor.CanManage(targetUser))
            return UserErrors.User.InsufficientRoleLevel;
        
        var unlockUserResult = targetUser.Unlock(nowUtc);

        if (unlockUserResult.IsFailure)
        {
            return unlockUserResult.Error;
        }
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return UnitResult.Success<Error>();
    }
}