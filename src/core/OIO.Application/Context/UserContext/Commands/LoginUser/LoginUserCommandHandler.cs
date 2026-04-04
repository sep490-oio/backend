using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.Services;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.LoginUser;

internal sealed class LoginUserCommandHandler
    : ICommandHandler<LoginUserCommand, AuthTokenDto>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenProvider _tokenProvider;
    private readonly ITokenHasher _tokenHasher;
    private readonly ITokenExpirationSettings _expirationSettings;
    private readonly ISessionRevocationStore _revocationStore;
    private readonly IClock _clock;

    public LoginUserCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtTokenProvider tokenProvider,
        ITokenHasher tokenHasher,
        ITokenExpirationSettings expirationSettings,
        ISessionRevocationStore revocationStore,
        IClock clock)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenProvider = tokenProvider;
        _tokenHasher = tokenHasher;
        _expirationSettings = expirationSettings;
        _revocationStore = revocationStore;
        _clock = clock;
    }

    public async Task<Result<AuthTokenDto, Error>> Handle(
        LoginUserCommand request,
        CancellationToken cancellationToken)
    {
        var (_,isFailure, account, error) = GetNormalizedAccount(request.Account);
        
        if (isFailure)
        {
            return error;
        }
        
        var user = await _dbContext.Set<User>()
            .Where(x => x.Email.Normalized == account || x.UserName.Normalized == account)
            .Include(x => x.Sessions)
            .ThenInclude(x => x.Tokens)
            .Include(x => x.Roles)
            .ThenInclude(x => x.Role)
            .Include(x => x.LoginHistories)
            .FirstOrDefaultAsync(cancellationToken);
        
        if (user is null)
            return UserErrors.User.InvalidCredentials;
        
        var nowUtc = _clock.UtcNow;

        // locked?
        var lockedR = user.EnsureNotLockedOut(nowUtc);
        
        if (lockedR.IsFailure)
            return await FailLogin(lockedR.Error);

        if (user.Status == UserStatus.Locked)
            return UserErrors.User.UserLocked;
        
        if(!user.EmailConfirmed)
            return UserErrors.User.EmailNotConfirmed;
        
        if (user.Status == UserStatus.Inactive)
            return UserErrors.User.UserInactive;

        // password
        if (user.Password is null || !user.Password.Verify(request.Password, _passwordHasher))
            return await FailLogin(UserErrors.User.InvalidCredentials);

        // Successful login
        var successR = user.RecordSuccessfulLogin(request.IpAddress, request.UserAgent, nowUtc);

        if (successR.IsFailure)
            return successR.Error;

        // 2FA check
        if (user.TwoFactorEnabled && user.TwoFactorProvider == TwoFactorProvider.Totp)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Generate limited 2FA token
            var twoFactorToken = _tokenProvider.GenerateTwoFactorJwt(user.Id, request.DeviceId, nowUtc);

            return new AuthTokenDto(
                AccessToken: twoFactorToken,
                RefreshToken: string.Empty,
                AccessTokenExpiresAt: nowUtc.AddMinutes(3),
                RefreshTokenExpiresAt: DateTime.MinValue,
                Session: null,
                RequiresTwoFactor: true);
        }

        // Create token family + refresh token
        (_, isFailure,var session, error) = user.CreateSession(
            deviceId: request.DeviceId, 
            userAgent: request.UserAgent,
            ipAddress: request.IpAddress, 
            slidingExpiration: _expirationSettings.FamilySlidingExpiration,
            absoluteExpiration: _expirationSettings.FamilyAbsoluteExpiration, 
            now: nowUtc);
        
        if (isFailure) 
            return error;

        // refresh token
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

        // access token
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

        async Task<Result<AuthTokenDto, Error>> FailLogin(Error e)
        {
            var r = user.RecordFailedLogin(request.IpAddress, request.UserAgent, nowUtc);
            if (r.IsFailure) 
                return r.Error;

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return e;
        }
    }

    private static Result<string, Error> GetNormalizedAccount(string account)
    {
        if (account.Contains('@'))
        { 
            var emailR =  UserEmail.Create(account);
            if (emailR.IsFailure)
            {
                return emailR.Error;
            }
            
            return emailR.Value.Normalized;
        }

        var userNameR = UserName.Create(account);
        if (userNameR.IsFailure)
        {
            return userNameR.Error;
        }
            
        return userNameR.Value.Normalized;
    }
}