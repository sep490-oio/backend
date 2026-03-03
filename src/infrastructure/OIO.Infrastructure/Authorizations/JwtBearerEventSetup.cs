using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using OIO.Application.UserContext.Services;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Infrastructure.Authorizations;

public sealed class CustomJwtBearerEvents : JwtBearerEvents
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ISessionRevocationStore _sessionRevocationStore;
    private readonly ILogger<CustomJwtBearerEvents> _logger;

    public CustomJwtBearerEvents(
        IProblemDetailsService problemDetailsService,
        ISessionRevocationStore sessionRevocationStore,
        ILogger<CustomJwtBearerEvents> logger)
    {
        _problemDetailsService = problemDetailsService;
        _sessionRevocationStore = sessionRevocationStore;
        _logger = logger;
    }

    public override async Task TokenValidated(TokenValidatedContext context)
    {
        await base.TokenValidated(context);

        var userIdClaim = context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub);
        
        var deviceId = context.Principal?.FindFirst(CustomClaimType.DeviceId);
    
        if (userIdClaim == null || deviceId == null || 
            !Guid.TryParse(userIdClaim.Value, out var parsedUserId)|| 
            !Guid.TryParse(deviceId.Value, out var parsedDeviceId))
        {
            context.Fail(nameof(UserErrors.Auth.AuthTokenInvalid));
            return;
        }

        var userId = UserId.From(parsedUserId);
        var isUserRevokedAsync = await _sessionRevocationStore.IsUserRevokedAsync(userId, context.HttpContext.RequestAborted);

        if (isUserRevokedAsync)
        {
            context.Fail(nameof(UserErrors.Auth.AuthTokenRevoked));
            return;
        }
        
        var isTokenExistInBlackList = await _sessionRevocationStore.IsDeviceRevokedAsync(
            UserId.From(parsedUserId), 
            parsedDeviceId, 
            context.HttpContext.RequestAborted);

        if (isTokenExistInBlackList)
        {
            context.Fail(nameof(UserErrors.Auth.AuthTokenRevoked));
        }
        
    }

    public override Task AuthenticationFailed(AuthenticationFailedContext context)
    {
        _logger.LogError(context.Exception, "JWT auth failed");
        return Task.CompletedTask;
    }

    public override async Task Challenge(JwtBearerChallengeContext context)
    {
        context.HandleResponse();
        var http = context.HttpContext;
        var authHeader = http.Request.Headers.Authorization.ToString();

        Error error;
        var isMissingHeader = string.IsNullOrWhiteSpace(authHeader);
        var isBearerWithoutToken =
            authHeader.Equals("Bearer", StringComparison.OrdinalIgnoreCase) ||
            authHeader.Equals("Bearer ", StringComparison.OrdinalIgnoreCase);

        if (isMissingHeader || isBearerWithoutToken)
        {
            error = UserErrors.Auth.AuthTokenMissing;
        }
        else
        {
            error = context.AuthenticateFailure switch
            {
                SecurityTokenExpiredException => UserErrors.Auth.AuthTokenExpired,
                { Message: nameof(UserErrors.Auth.AuthTokenRevoked) } => 
                    UserErrors.Auth.AuthTokenRevoked,
                _ => UserErrors.Auth.AuthTokenInvalid
            };
        }

        await WriteErrorAsync(http, StatusCodes.Status401Unauthorized, error);
    }

    public override async Task Forbidden(ForbiddenContext context)
    {
        await WriteErrorAsync(
            context.HttpContext,
            StatusCodes.Status403Forbidden,
            UserErrors.Auth.InsufficientPermissions);
    }
    
    private async Task WriteErrorAsync(HttpContext httpContext, int statusCode, Error error)
    {
        if (httpContext.Response.HasStarted)
            return;

        var problemDetails = new ProblemDetails()
        {
            Title = error.Message,
            Detail = error.Code,
            Status = statusCode,
        };

        var ok = await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails
        });
        
    }
}