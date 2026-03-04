using OIO.Api.Middleware;

namespace OIO.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseRequestContextLogging(this IApplicationBuilder app)
    {
        app.UseMiddleware<RequestContextLoggingMiddleware>();

        return app;
    }
}