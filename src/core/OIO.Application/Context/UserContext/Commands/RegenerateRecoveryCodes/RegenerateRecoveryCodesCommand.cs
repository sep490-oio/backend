using System.Security.Cryptography;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Auth;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.Services;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.RegenerateRecoveryCodes;

/// <summary>
/// Regenerate recovery codes — requires current TOTP code for verification.
/// Old codes are deleted and replaced.
/// </summary>
public sealed record RegenerateRecoveryCodesCommand(
    string Code) : ICommand<RegenerateRecoveryCodesResponse>, IHasValidate
{
    public ViolationsError Validate()
    {
        return RegenerateRecoveryCodesCommand.Check()
            .WithOwnerName("RegenerateRecoveryCodes")
            .Field(Code)
            .NotWhiteSpace();
    }
}

public sealed record RegenerateRecoveryCodesResponse(IReadOnlyList<string> RecoveryCodes);

internal sealed class RegenerateRecoveryCodesCommandHandler
    : ICommandHandler<RegenerateRecoveryCodesCommand, RegenerateRecoveryCodesResponse>
{
    private const int RecoveryCodeCount = 8;
    private const int RecoveryCodeByteLength = 5;

    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly ITotpService _totpService;
    private readonly ITokenHasher _tokenHasher;

    public RegenerateRecoveryCodesCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock,
        ITotpService totpService,
        ITokenHasher tokenHasher)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _totpService = totpService;
        _tokenHasher = tokenHasher;
    }

    public async Task<Result<RegenerateRecoveryCodesResponse, Error>> Handle(
        RegenerateRecoveryCodesCommand request,
        CancellationToken cancellationToken)
    {
        var nowUtc = _clock.UtcNow;

        var user = await _dbContext.GetByIdAsync<User, UserId>(
            _currentUser.UserId,
            cancellationToken: cancellationToken);

        if (user is null)
            return UserErrors.User.NotFound(_currentUser.UserId);

        if (!user.TwoFactorEnabled || string.IsNullOrWhiteSpace(user.TwoFactorSecret))
        {
            return Error.Forbidden(
                code: "User.Totp.NotEnabled",
                description: "TOTP two-factor authentication is not enabled.");
        }

        // Verify current TOTP code before regenerating
        if (!_totpService.VerifyCode(user.TwoFactorSecret, request.Code, out _))
        {
            return Error.Unauthorized(
                code: "User.Totp.InvalidCode",
                description: "The provided TOTP code is invalid.");
        }

        // Remove existing recovery codes
        var existingCodes = await _dbContext.Set<RecoveryCode>()
            .Where(rc => rc.UserId == _currentUser.UserId)
            .ToListAsync(cancellationToken);

        foreach (var existing in existingCodes)
            _dbContext.Remove(existing);

        // Generate new recovery codes
        var plainTextCodes = new List<string>(RecoveryCodeCount);

        for (var i = 0; i < RecoveryCodeCount; i++)
        {
            var bytes = RandomNumberGenerator.GetBytes(RecoveryCodeByteLength);
            var plainCode = Convert.ToHexString(bytes).ToLower();
            plainTextCodes.Add(plainCode);

            var hashedCode = _tokenHasher.Hash(plainCode);
            _dbContext.Insert(RecoveryCode.Create(_currentUser.UserId, hashedCode, nowUtc));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RegenerateRecoveryCodesResponse(plainTextCodes.AsReadOnly());
    }
}
