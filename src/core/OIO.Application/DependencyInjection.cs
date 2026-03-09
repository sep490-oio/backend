using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OIO.Application.Abstractions.Behaviors;
using OIO.Application.Abstractions.Settings;
using OIO.Application.Abstractions.Sorting;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Application.Context.UserContext.Mappings;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.UserContext.Aggregates.Users;

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
            cfg.AddOpenBehavior(typeof(ConcurrencyRetryBehavior<,>));
        });

        services.AddSorting();

        return services;
    }

    public static IServiceCollection AddSorting(this IServiceCollection services)
    {
        
        
        return services;
    }
}