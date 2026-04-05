using System.Diagnostics;
using Microsoft.IdentityModel.JsonWebTokens;
using Serilog.Context;

namespace OIO.Api.Middleware;

public class RequestContextLoggingMiddleware(RequestDelegate next)
{
    public const string CorrelationIdHeaderName = "X-Correlation-Id";
    public const string CorrelationIdItemKey = "__CorrelationId";

    public async Task Invoke(HttpContext context)
    {
        var correlationId = GetCorrelationId(context);
        var activity = Activity.Current;
        var traceId = activity?.TraceId.ToString();
        var spanId = activity?.SpanId.ToString();
        var requestPath = context.Request.Path.Value ?? string.Empty;
        var httpMethod = context.Request.Method;
        var endpointName = context.GetEndpoint()?.DisplayName;
        var userId = context.User.Identity?.IsAuthenticated == true
            ? context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            : null;

        context.Items[CorrelationIdItemKey] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeaderName] = correlationId;
            return Task.CompletedTask;
        });

        activity?.SetTag("correlation.id", correlationId);
        activity?.SetTag("http.request.method", httpMethod);
        activity?.SetTag("url.path", requestPath);

        if (!string.IsNullOrWhiteSpace(userId))
        {
            activity?.SetTag("enduser.id", userId);
        }

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("TraceId", traceId ?? string.Empty))
        using (LogContext.PushProperty("SpanId", spanId ?? string.Empty))
        using (LogContext.PushProperty("RequestPath", requestPath))
        using (LogContext.PushProperty("HttpMethod", httpMethod))
        using (LogContext.PushProperty("EndpointName", endpointName ?? string.Empty))
        using (LogContext.PushProperty("UserId", userId ?? string.Empty))
        {
            await next.Invoke(context);
        }
    }

    private static string GetCorrelationId(HttpContext context)
    {
        context.Request.Headers.TryGetValue(
            CorrelationIdHeaderName,
            out var correlationId);

        return correlationId.FirstOrDefault() ?? context.TraceIdentifier;
    }
}
