using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Domain.AppDefinitions;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return ForgotPasswordCommand.Check()
            .WithOwnerName("ForgotPassword")
            .Field(Email)
            .NotWhiteSpace()
            .Matches(App.Constraint.UserEmail.Regex);
    }
}

internal sealed class ForgotPasswordCommandHandler
    : ICommandHandler<ForgotPasswordCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ForgotPasswordCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<UnitResult<Error>> Handle(
        ForgotPasswordCommand request,
        CancellationToken cancellationToken)
    {
        var (_, isFailure, email, error) = UserEmail.Create(request.Email);
        
        if (isFailure)
            return error;
        
        var user = await _dbContext.Set<User>()
            .FirstOrDefaultAsync(x => x.Email == email, cancellationToken);

        if (user is null)
            return UnitResult.Success<Error>();

        if (user.EmailConfirmedAt is not null)
            return Result.Success<Error>();

        var nowUtc = _clock.UtcNow;

        var result = user.RequestPasswordReset(nowUtc);
        
        if (result.IsFailure)
            return result.Error;
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success<Error>();
    }
}