using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OIO.Application.Abstractions.Behaviors;
using OIO.Application.Abstractions.Settings;

namespace OIO.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.Configure<MediatrOption>(configuration.GetSection(MediatrOption.SectionName));
        // MediatR
        services.AddMediatR(cfg =>
        {
            var mediatrOption = new MediatrOption();
            configuration.Bind(MediatrOption.SectionName, mediatrOption);
            cfg.LicenseKey = mediatrOption.LicenseKey;
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });


        return services;
    }
}