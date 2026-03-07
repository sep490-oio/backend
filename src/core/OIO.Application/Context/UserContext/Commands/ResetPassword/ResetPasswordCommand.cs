using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Abstractions.Security;
using OIO.Domain.AppDefinitions;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.Services;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.ResetPassword;

public sealed record ResetPasswordCommand(
    string Email,
    string Token,
    string NewPassword,
    string ConfirmPassword) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return ResetPasswordCommand.Check()
            .WithOwnerName("ResetPassword")
            .Field(Email)
            .NotWhiteSpace()
            .Matches(App.Constraint.UserEmail.Regex)
            .Field(Token)
            .NotWhiteSpace()
            .Field(NewPassword)
            .NotWhiteSpace()
            .MinLength(App.Constraint.Password.MinLength)
            .MaxLength(App.Constraint.Password.MaxLength)
            .Format(
                message: App.Constraint.Password.FormatMessage,
                validators: App.Constraint.Password.Validator)
            .Field(ConfirmPassword)
            .NotWhiteSpace()
            .EqualTo(NewPassword, "ConfirmPassword must be equal to NewPassword");
    }
}

internal sealed class ResetPasswordCommandHandler
    : ICommandHandler<ResetPasswordCommand>
{
    private readonly IDbContext _dbContext;
    private readonly ISecureTokenStore _secureTokenStore;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock; 

    public ResetPasswordCommandHandler(
        IDbContext dbContext,
        ISecureTokenStore secureTokenStore,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _dbContext = dbContext;
        _secureTokenStore = secureTokenStore;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<UnitResult<Error>> Handle(
        ResetPasswordCommand request,
        CancellationToken cancellationToken)
    {
        var (_, isFailure, email, error) = UserEmail.Create(request.Email);
        
        if (isFailure)
            return error;
        
        var user = await _dbContext.Set<User>()
            .FirstOrDefaultAsync(x => x.Email == email, cancellationToken);

        if (user is null)
        {
            return UserErrors.User.InvalidCredentials;
        }

        // Validate token
        var isValid = await _secureTokenStore.ValidateTokenAsync(
            TokenType.PasswordReset,
            user.Id,
            request.Token,
            cancellationToken);

        if (!isValid)
            return UserErrors.User.InvalidConfirmationToken;

        // Hash and set new password
        var hashedPassword = _passwordHasher.Hash(request.NewPassword);
        
        var password = Password.CreateFromHash(hashedPassword);
        
        user.ChangePassword(password, _clock.UtcNow);

        // Invalidate the reset token
        await _secureTokenStore.InvalidateTokenAsync(
            TokenType.PasswordReset,
            user.Id,
            cancellationToken);

        _dbContext.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success<Error>();
    }
}