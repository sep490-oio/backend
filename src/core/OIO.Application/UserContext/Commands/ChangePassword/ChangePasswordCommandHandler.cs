using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.UserContext.Services;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.Repositories;
using OIO.Domain.Context.UserContext.Services;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.UserContext.Commands.ChangePassword;

internal sealed class ChangePasswordCommandHandler
    : ICommandHandler<ChangePasswordCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public ChangePasswordCommandHandler(
        IUserRepository userRepository,
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ICurrentUser currentUser,
        IClock clock)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<UnitResult<Error>> Handle(
        ChangePasswordCommand request,
        CancellationToken cancellationToken)
    {
        var nowUtc = _clock.UtcNow;

        if (_currentUser.UserId is null)
            return UserErrors.Auth.UserNotLoggedIn;

        var user = await _dbContext.GetByIdAsync<User, UserId>(
            _currentUser.UserId,
            queryBuilder: query => query
                .Include(x => x.RefreshTokenFamilies)
                .ThenInclude(x => x.Tokens),
            cancellationToken: cancellationToken);
        
        if (user is null)
            return UserErrors.User.NotFound(_currentUser.UserId);

        // Verify current password
        if (user.Password is null || !user.Password.Verify(request.CurrentPassword, _passwordHasher))
            return UserErrors.User.InvalidCredentials;

        var newHashR = Password.Create(request.NewPassword, _passwordHasher);

        if (newHashR.IsFailure)
        {
            return newHashR.Error;
        }
        
        user.ChangePassword(newHashR.Value, nowUtc);

        // Revoke all refresh tokens for security
        user.RevokeAllTokenFamilies("Password changed", nowUtc);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return UnitResult.Success<Error>();
        
    }
}