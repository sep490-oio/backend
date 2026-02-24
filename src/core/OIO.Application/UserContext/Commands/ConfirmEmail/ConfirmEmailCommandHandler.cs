using CSharpFunctionalExtensions;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.UserContext.Services;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.UserContext.Commands.ConfirmEmail;

internal sealed class ConfirmEmailCommandHandler
    : ICommandHandler<ConfirmEmailCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailConfirmationService _emailConfirmationService;
    private readonly IClock _clock;

    public ConfirmEmailCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IEmailConfirmationService emailConfirmationService,
        IClock clock)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _emailConfirmationService = emailConfirmationService;
        _clock = clock;
    }

    public async Task<UnitResult<Error>> Handle(
        ConfirmEmailCommand request,
        CancellationToken cancellationToken)
    {
        var nowUtc = _clock.UtcNow;
        var userId = UserId.From(request.UserId);
        
        var user = await _dbContext.GetByIdAsync<User, UserId>(userId, cancellationToken: cancellationToken);
        
        if (user is null)
            return UserErrors.User.NotFound(userId);

        var isValid = await _emailConfirmationService.ValidateTokenAsync(
            userId, 
            request.Token,
            cancellationToken);

        if (!isValid)
            return UserErrors.User.InvalidConfirmationToken;

        var confirmEmailR = user.ConfirmEmail(nowUtc);

        if (confirmEmailR.IsFailure)
        {
            return confirmEmailR;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return UnitResult.Success<Error>();
    }
}