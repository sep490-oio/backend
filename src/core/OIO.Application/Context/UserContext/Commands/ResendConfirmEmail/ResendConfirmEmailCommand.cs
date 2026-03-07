using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Mail;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.AppDefinitions;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.ResendConfirmEmail;

public sealed record ResendConfirmEmailCommand(string Email) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return ResendConfirmEmailCommand.Check()
            .WithOwnerName("ResendConfirmEmail")
            .Field(Email)
            .NotWhiteSpace()
            .Matches(App.Constraint.UserEmail.Regex);
    }
}

internal sealed class ResendConfirmEmailCommandHandler
    : ICommandHandler<ResendConfirmEmailCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ResendConfirmEmailCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<UnitResult<Error>> Handle(
        ResendConfirmEmailCommand request,
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

        var result = user.RequestEmailVerification(nowUtc);
        
        if (result.IsFailure)
            return result.Error;
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success<Error>();
    }
}