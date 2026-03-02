using CSharpFunctionalExtensions;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.UserContext.Services;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.UserContext.Commands.DisableTwoFactor;

internal sealed class DisableTwoFactorCommandHandler
    : ICommandHandler<DisableTwoFactorCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public DisableTwoFactorCommandHandler(
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
        DisableTwoFactorCommand request,
        CancellationToken cancellationToken)
    {
        var nowUtc = _clock.UtcNow;
        

        var user = await _dbContext.GetByIdAsync<User,UserId>(_currentUser.UserId, cancellationToken: cancellationToken);
        
        if (user is null)
            return UserErrors.User.NotFound(_currentUser.UserId);

        var disableTwoFactorResult = user.DisableTwoFactor(nowUtc);

        if (disableTwoFactorResult.IsFailure)
        {
            return disableTwoFactorResult;
        }
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return disableTwoFactorResult;
    }
}