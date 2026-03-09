using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Infrastructure.Authorizations;

public sealed class CustomJwtBearerEvents : JwtBearerEvents
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ISessionRevocationStore _sessionRevocationStore;
    private readonly ILogger<CustomJwtBearerEvents> _logger;
    private readonly string _authErrorItemKey = "auth_error";

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
        var deviceIdClaim = context.Principal?.FindFirst(CustomClaimType.DeviceId);

        if (userIdClaim == null ||
            deviceIdClaim == null ||
            !Guid.TryParse(userIdClaim.Value, out var parsedUserId) ||
            !Guid.TryParse(deviceIdClaim.Value, out var parsedDeviceId))
        {
            context.HttpContext.Items[_authErrorItemKey] = nameof(UserErrors.Auth.AuthTokenInvalid);
            context.Fail(nameof(UserErrors.Auth.AuthTokenInvalid));
            return;
        }

        var userId = UserId.From(parsedUserId);

        if (await _sessionRevocationStore.IsUserRevokedAsync(userId, context.HttpContext.RequestAborted))
        {
            context.HttpContext.Items[_authErrorItemKey] = nameof(UserErrors.Auth.AuthTokenRevoked);
            context.Fail(nameof(UserErrors.Auth.AuthTokenRevoked));
            return;
        }

        if (await _sessionRevocationStore.IsDeviceRevokedAsync(userId, parsedDeviceId, context.HttpContext.RequestAborted))
        {
            context.HttpContext.Items[_authErrorItemKey] = nameof(UserErrors.Auth.AuthTokenRevoked);
            context.Fail(nameof(UserErrors.Auth.AuthTokenRevoked));
        }
    }

    public override Task AuthenticationFailed(AuthenticationFailedContext context)
    {
        _logger.LogError(context.Exception, "JWT auth failed");
        return Task.CompletedTask;
    }
    
    public override Task MessageReceived(MessageReceivedContext context)
    {
        var path = context.HttpContext.Request.Path;

        if (string.IsNullOrEmpty(context.Token) &&
            path.StartsWithSegments("/hubs"))
        {
            var token = context.Request.Query["access_token"].ToString();

            if (!string.IsNullOrWhiteSpace(token))
            {
                context.Token = token;
            }
        }

        return Task.CompletedTask;
    }

    public override async Task Challenge(JwtBearerChallengeContext context)
    {
        context.HandleResponse();

        var http = context.HttpContext;
        var authHeader = http.Request.Headers.Authorization.ToString();
        var queryToken = http.Request.Query["access_token"].ToString();
        var path = http.Request.Path;

        var isBearerWithoutToken =
            authHeader.Equals("Bearer", StringComparison.OrdinalIgnoreCase) ||
            authHeader.Equals("Bearer ", StringComparison.OrdinalIgnoreCase);

        var hasHeaderToken =
            !string.IsNullOrWhiteSpace(authHeader) && !isBearerWithoutToken;

        var hasQueryToken =
            path.StartsWithSegments("/hubs") &&
            !string.IsNullOrWhiteSpace(queryToken);

        var hasAnyToken = hasHeaderToken || hasQueryToken;
        var isMissingToken = !hasAnyToken;

        var customError = http.Items[_authErrorItemKey] as string;

        var error = customError switch
        {
            nameof(UserErrors.Auth.AuthTokenRevoked) => UserErrors.Auth.AuthTokenRevoked,
            nameof(UserErrors.Auth.AuthTokenInvalid) => UserErrors.Auth.AuthTokenInvalid,
            _ when isMissingToken => UserErrors.Auth.AuthTokenMissing,
            _ when context.AuthenticateFailure is SecurityTokenExpiredException => UserErrors.Auth.AuthTokenExpired,
            _ => UserErrors.Auth.AuthTokenInvalid
        };

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
