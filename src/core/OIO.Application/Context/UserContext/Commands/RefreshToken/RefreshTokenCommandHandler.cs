using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.Services;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.RefreshToken;

internal sealed class RefreshTokenCommandHandler
    : ICommandHandler<RefreshTokenCommand, AuthTokenDto>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenHasher _tokenHasher;
    private readonly IJwtTokenProvider _tokenProvider;
    private readonly ICurrentUser _currentUser;
    private readonly ITokenExpirationSettings _expirationSettings;
    private readonly ISessionRevocationStore _revocationStore;
    private readonly IClock _clock;

    public RefreshTokenCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ITokenHasher tokenHasher,
        IJwtTokenProvider tokenProvider,
        ICurrentUser currentUser,
        ITokenExpirationSettings expirationSettings,
        ISessionRevocationStore revocationStore,
        IClock clock)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _tokenHasher = tokenHasher;
        _tokenProvider = tokenProvider;
        _currentUser = currentUser;
        _expirationSettings = expirationSettings;
        _revocationStore = revocationStore;
        _clock = clock;
    }

    public async Task<Result<AuthTokenDto, Error>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _dbContext.Set<User>()
            .Where(u => u.Id == _currentUser.UserId)
            .Include(u => u.Sessions)
            .ThenInclude(f => f.Tokens)
            .Include(u => u.Roles)
            .ThenInclude(r => r.Role)
            .FirstOrDefaultAsync(cancellationToken);
        
        if (user is null)
            return UserErrors.RefreshToken.Invalid;
        
        var nowUtc = _clock.UtcNow;
        
        if (!request.DeviceId.Equals(_currentUser.DeviceId))
        {
            //Logout all sessions
            user.RevokeAllSession("User refresh token device id mismatch with device id from access token", nowUtc);
            
            await _revocationStore.RevokeAllDevicesAsync(
                user.Id,
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            return UserErrors.RefreshToken.Revoked;
        }
        
        var tokenHash = _tokenHasher.Hash(request.RefreshToken);

        UserRefreshToken? currentToken = null;
        UserSession? session = null;

        foreach (var f in user.Sessions)
        {
            var token = f.Tokens.FirstOrDefault(t => t.TokenHash == tokenHash);
            
            if (token is null) 
                continue;
            
            currentToken = token;
            session = f;
            break;
        }

        if (currentToken is null || session is null)
            return UserErrors.RefreshToken.Invalid;
        
        
        if (session.DeviceId != request.DeviceId)
        {
            // Device mismatch — potential token theft!
            // Revoke entire session for safety
            user.RevokeSession(session.Id, 
                $"Device mismatch: expected '{session.DeviceId}', got '{request.DeviceId}'", nowUtc);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return UserErrors.User.DeviceMismatch;
        }
        
        
        // Rotate token
        var rawRefreshToken = _tokenProvider.Generate();
        var hashedRefreshToken = _tokenHasher.Hash(rawRefreshToken);

        var (_,isRotateRefreshTokenFailed, refreshToken, rotateRefreshTokenError ) = user.RotateRefreshToken(
            sessionId: session.Id,
            currentToken: currentToken,
            newTokenHash: hashedRefreshToken,
            ipAddress: request.IpAddress,
            timeRefreshTokenExpiration: _expirationSettings.RefreshTokenExpiration,
            timeRefreshFamilySlidingExpiration: _expirationSettings.FamilySlidingExpiration,
            now: nowUtc);

        if (isRotateRefreshTokenFailed)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return rotateRefreshTokenError;
        }

        // Generate new access token
        var roles = user.Roles.Select(x => x.Role.Name).ToList();
        
        var accessToken = _tokenProvider.GenerateJwt(
            userId: user.Id,
            email: user.Email, 
            userName: user.UserName,
            deviceId: request.DeviceId,
            roles: roles,
            now: nowUtc);
        
        var accessTokenExpiresAt = nowUtc.Add(_expirationSettings.AccessTokenExpiration);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
}