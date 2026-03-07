using CSharpFunctionalExtensions;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Abstractions.Security;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.ConfirmPhoneNumber;

internal sealed class ConfirmPhoneNumberCommandHandler
    : ICommandHandler<ConfirmPhoneNumberCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ISecureTokenStore _secureTokenStore;
    private readonly IClock _clock;

    public ConfirmPhoneNumberCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ISecureTokenStore secureTokenStore,
        IClock clock)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _secureTokenStore = secureTokenStore;
        _clock = clock;
    }

    public async Task<UnitResult<Error>> Handle(
        ConfirmPhoneNumberCommand request,
        CancellationToken cancellationToken)
    {
        var nowUtc = _clock.UtcNow;

        var user = await _dbContext.GetByIdAsync<User, UserId>(_currentUser.UserId, cancellationToken: cancellationToken);
        if (user is null)
            return UserErrors.User.NotFound(_currentUser.UserId);

        if (user.PhoneNumber is null)
            return UserErrors.User.PhoneNotSet;

        var isValid = await _secureTokenStore.ValidateTokenAsync(
            TokenType.PhoneVerification,
            _currentUser.UserId,
            request.VerificationCode,
            cancellationToken);
        
        if (!isValid)
            return UserErrors.User.InvalidConfirmationCode;

        var confirmPhoneNumberResult = user.ConfirmPhoneNumber(nowUtc);

        if (confirmPhoneNumberResult.IsFailure)
        {
            return confirmPhoneNumberResult;
        }
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return confirmPhoneNumberResult;
    }
}