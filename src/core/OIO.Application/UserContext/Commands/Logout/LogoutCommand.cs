using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.UserContext.Services;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.Repositories;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.UserContext.Commands.Logout;

public sealed record LogoutCommand(Guid? DeviceId = null) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return LogoutCommand.Check()
            .WithOwnerName("Logout")
            .Field(DeviceId)
            .WhenHasValue(x => x.NotEmptyGuid());
    }
}

internal sealed class LogoutCommandHandler
    : ICommandHandler<LogoutCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public LogoutCommandHandler(
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
        LogoutCommand request,
        CancellationToken cancellationToken)
    {
        var nowUtc = _clock.UtcNow;
        
        if (_currentUser.UserId is null)
            return UserErrors.Auth.UserNotLoggedIn;

        var user = await _dbContext.GetByIdAsync<User, UserId>(
            id: _currentUser.UserId,
            queryBuilder: query => query
                .Include(x => x.RefreshTokenFamilies)
                .ThenInclude(x => x.Tokens),
            cancellationToken: cancellationToken);
        if (user is null)
            return UserErrors.User.NotFound(_currentUser.UserId);
        
        if (request.DeviceId.HasValue)
        {
            // Logout specific session
            user.RevokeTokenFamilyByDevice(request.DeviceId.Value, "User logout", nowUtc);
        }
        else
        {
            // Logout all sessions
            user.RevokeAllTokenFamilies("User logout all", nowUtc);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return UnitResult.Success<Error>();
    }
}