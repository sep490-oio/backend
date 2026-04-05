using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using OIO.Api.Middleware;
using OIO.Application.Abstractions.Commons;
using Serilog;
using Serilog.Events;

namespace OIO.Api.Extensions;

public static class SerilogRequestLoggingExtensions
{
    public static IApplicationBuilder UseAppRequestLogging(this IApplicationBuilder app)
    {
        var optionsMonitor = app.ApplicationServices.GetRequiredService<IOptionsMonitor<AppLoggingOptions>>();

        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {HttpMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                var activity = Activity.Current;
                var endpoint = httpContext.GetEndpoint();
                var correlationId = httpContext.Items.TryGetValue(RequestContextLoggingMiddleware.CorrelationIdItemKey, out var correlationIdValue)
                    ? correlationIdValue?.ToString()
                    : httpContext.TraceIdentifier;
                var userId = httpContext.User.Identity?.IsAuthenticated == true
                    ? httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                    : null;

                diagnosticContext.Set("CorrelationId", correlationId ?? httpContext.TraceIdentifier);
                diagnosticContext.Set("TraceId", activity?.TraceId.ToString() ?? string.Empty);
                diagnosticContext.Set("SpanId", activity?.SpanId.ToString() ?? string.Empty);
                diagnosticContext.Set("RequestPath", httpContext.Request.Path.Value ?? string.Empty);
                diagnosticContext.Set("HttpMethod", httpContext.Request.Method);
                diagnosticContext.Set("EndpointName", endpoint?.DisplayName ?? string.Empty);

                if (!string.IsNullOrWhiteSpace(userId))
                {
                    diagnosticContext.Set("UserId", userId);
                }
            };
            options.GetLevel = (httpContext, elapsed, exception) =>
                DetermineRequestLogLevel(httpContext, elapsed, exception, optionsMonitor.CurrentValue.Request);
        });

        return app;
    }

    private static LogEventLevel DetermineRequestLogLevel(
        HttpContext httpContext,
        double elapsedMs,
        Exception? exception,
        AppRequestLoggingOptions options)
    {
        if (ShouldExclude(httpContext, options))
        {
            return LogEventLevel.Verbose;
        }

        var statusCode = httpContext.Response.StatusCode;
        var method = httpContext.Request.Method;

        if (exception is not null || statusCode >= StatusCodes.Status500InternalServerError)
        {
            return LogEventLevel.Error;
        }

        if (statusCode >= StatusCodes.Status400BadRequest || elapsedMs >= options.SlowRequestThresholdMs)
        {
            return LogEventLevel.Warning;
        }

        if (!HttpMethods.IsGet(method) && !HttpMethods.IsHead(method) && !HttpMethods.IsOptions(method))
        {
            return LogEventLevel.Information;
        }

        return options.LogReadSuccessAsDebug
            ? LogEventLevel.Debug
            : LogEventLevel.Information;
    }

    private static bool ShouldExclude(HttpContext httpContext, AppRequestLoggingOptions options)
    {
        var path = httpContext.Request.Path.Value ?? string.Empty;

        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        if (Path.HasExtension(path))
        {
            return true;
        }

        return options.ExcludedPathPrefixes.Any(prefix =>
            path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
}
