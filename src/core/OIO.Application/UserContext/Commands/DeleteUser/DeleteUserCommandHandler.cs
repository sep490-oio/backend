using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.UserContext.Services;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.UserContext.Commands.DeleteUser;

internal sealed class DeleteUserCommandHandler
    : ICommandHandler<DeleteUserCommand>
{
    private readonly IDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ISessionRevocationStore _sessionRevocationStore;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public DeleteUserCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ISessionRevocationStore sessionRevocationStore,
        ICurrentUser currentUser,
        IClock clock)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _sessionRevocationStore = sessionRevocationStore;
        _clock = clock;
    }

    public async Task<UnitResult<Error>> Handle(
        DeleteUserCommand request,
        CancellationToken cancellationToken)
    {
        var nowUtc = _clock.UtcNow;
        
        var actorId = _currentUser.UserId;
        var targetUserId = UserId.From(request.UserId);
        
        if (actorId == targetUserId)
            return UserErrors.User.CannotRemoveYourself;
        
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
            queryBuilder: query => query
                .Include(x => x.Roles)
                .ThenInclude(x => x.Role)
                .Include(x => x.Sessions)
                .ThenInclude(x => x.Tokens),
            cancellationToken: cancellationToken);
        
        if (targetUser is null)
            return UserErrors.User.NotFound(targetUserId);
        
        if (!actor.CanManage(targetUser))
            return UserErrors.User.InsufficientRoleLevel;

        targetUser.SoftDelete(nowUtc);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _sessionRevocationStore.RevokeAllDevicesAsync(
            targetUserId,
            cancellationToken);
        
        return UnitResult.Success<Error>();
    }
}