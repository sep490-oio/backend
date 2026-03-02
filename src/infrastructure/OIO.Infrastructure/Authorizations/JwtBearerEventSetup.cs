using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Infrastructure.Authorizations;

public sealed class CustomJwtBearerEvents : JwtBearerEvents
{
    private const string JwtAuthErrorKey = "__jwt_auth_error";
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<CustomJwtBearerEvents> _logger;

    public CustomJwtBearerEvents(
        IProblemDetailsService problemDetailsService,
        ILogger<CustomJwtBearerEvents> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public override Task AuthenticationFailed(AuthenticationFailedContext context)
    {
        _logger.LogError(context.Exception, "JWT auth failed");
        context.HttpContext.Items[JwtAuthErrorKey] = context.Exception;
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
            var ex = http.Items.TryGetValue(JwtAuthErrorKey, out var value)
                ? value as Exception
                : null;

            error = ex is SecurityTokenExpiredException
                ? UserErrors.Auth.AuthTokenExpired
                : UserErrors.Auth.AuthTokenInvalid;
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