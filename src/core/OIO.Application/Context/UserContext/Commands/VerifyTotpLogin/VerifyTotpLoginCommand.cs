using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Auth;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.Services;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.VerifyTotpLogin;

public sealed record VerifyTotpLoginCommand(
    string Code,
    Guid DeviceId,
    System.Net.IPAddress IpAddress,
    string UserAgent) : ICommand<AuthTokenDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return VerifyTotpLoginCommand.Check()
            .WithOwnerName("VerifyTotpLogin")
            .Field(Code)
            .NotWhiteSpace()
            .Field(UserAgent)
            .NotWhiteSpace();
    }
}

internal sealed class VerifyTotpLoginCommandHandler
    : ICommandHandler<VerifyTotpLoginCommand, AuthTokenDto>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly ITotpService _totpService;
    private readonly IJwtTokenProvider _tokenProvider;
    private readonly ITokenHasher _tokenHasher;
    private readonly ITokenExpirationSettings _expirationSettings;
    private readonly ISessionRevocationStore _revocationStore;

    public VerifyTotpLoginCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock,
        ITotpService totpService,
        IJwtTokenProvider tokenProvider,
        ITokenHasher tokenHasher,
        ITokenExpirationSettings expirationSettings,
        ISessionRevocationStore revocationStore)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _totpService = totpService;
        _tokenProvider = tokenProvider;
        _tokenHasher = tokenHasher;
        _expirationSettings = expirationSettings;
        _revocationStore = revocationStore;
    }

    public async Task<Result<AuthTokenDto, Error>> Handle(
        VerifyTotpLoginCommand request,
        CancellationToken cancellationToken)
    {
        var nowUtc = _clock.UtcNow;

        var user = await _dbContext.Set<User>()
            .Where(x => x.Id == _currentUser.UserId)
            .Include(x => x.Sessions)
            .ThenInclude(x => x.Tokens)
            .Include(x => x.Roles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
            return UserErrors.User.NotFound(_currentUser.UserId);

        if (string.IsNullOrWhiteSpace(user.TwoFactorSecret))
        {
            return Error.Forbidden(
                code: "User.Totp.NotConfigured",
                description: "TOTP two-factor authentication is not configured for this user.");
        }

        // Try TOTP verification first
        var totpValid = _totpService.VerifyCode(user.TwoFactorSecret, request.Code, out var timeStep);

        if (totpValid)
        {
            // Check for replay attack
            if (user.LastUsedTotpTimeStep.HasValue && user.LastUsedTotpTimeStep.Value >= timeStep)
            {
                return Error.Unauthorized(
                    code: "User.Totp.ReplayDetected",
                    description: "This TOTP code has already been used. Please wait for a new code.");
            }

            user.RecordTotpTimeStep(timeStep);
        }
        else
        {
            // Try recovery codes
            var recoveryUsed = await TryUseRecoveryCode(request.Code, nowUtc, cancellationToken);

            if (!recoveryUsed)
            {
                return Error.Unauthorized(
                    code: "User.Totp.InvalidCode",
                    description: "The provided TOTP code or recovery code is invalid.");
            }
        }

        // Complete login: create session + refresh token + access JWT
        var (_, isFailure, session, error) = user.CreateSession(
            deviceId: request.DeviceId,
            userAgent: request.UserAgent,
            ipAddress: request.IpAddress,
            slidingExpiration: _expirationSettings.FamilySlidingExpiration,
            absoluteExpiration: _expirationSettings.FamilyAbsoluteExpiration,
            now: nowUtc);

        if (isFailure)
            return error;

        // Refresh token
        var rawRefreshToken = _tokenProvider.Generate();
        var hashedRefreshToken = _tokenHasher.Hash(rawRefreshToken);

        (_, isFailure, var refreshToken, error) = user.CreateRefreshToken(
            sessionId: session.Id,
            tokenHash: hashedRefreshToken,
            ipAddress: request.IpAddress,
            timeRefreshTokenExpiration: _expirationSettings.RefreshTokenExpiration,
            now: nowUtc);

        if (isFailure)
            return error;

        // Access token
        var roles = user.Roles.Select(ur => ur.Role.Name).ToList();

        var accessToken = _tokenProvider.GenerateJwt(
            userId: user.Id,
            email: user.Email,
            userName: user.UserName,
            deviceId: request.DeviceId,
            roles: roles,
            now: nowUtc);

        var accessTokenExpiresAt = nowUtc.Add(_expirationSettings.AccessTokenExpiration);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _revocationStore.ClearDeviceRevocationAsync(
            user.Id, cancellationToken);

        await _revocationStore.ClearUserRevocationAsync(
            user.Id, cancellationToken);

        return new AuthTokenDto(
            AccessToken: accessToken,
            RefreshToken: rawRefreshToken,
            AccessTokenExpiresAt: accessTokenExpiresAt,
            RefreshTokenExpiresAt: refreshToken.ExpiresAt,
            Session: new SessionExpirationDto(
                SessionId: session.Id.Value,
                DeviceId: session.DeviceId,
                SlidingExpiresAt: session.ExpiresAt,
                AbsoluteExpiresAt: session.AbsoluteExpiresAt,
                IsNearingAbsoluteExpiration: session.IsNearingAbsoluteExpiration(nowUtc),
                RemainingAbsoluteTime: session.RemainingAbsoluteTime(nowUtc)));
    }

    private async Task<bool> TryUseRecoveryCode(
        string code,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var recoveryCodes = await _dbContext.Set<RecoveryCode>()
            .Where(rc => rc.UserId == _currentUser.UserId && !rc.IsUsed)
            .ToListAsync(cancellationToken);

        foreach (var recoveryCode in recoveryCodes)
        {
            if (_tokenHasher.Verify(code, recoveryCode.CodeHash))
            {
                recoveryCode.MarkAsUsed(nowUtc);
                return true;
            }
        }

        return false;
    }
}
