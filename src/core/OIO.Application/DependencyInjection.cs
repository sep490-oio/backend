using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OIO.Application.Abstractions.Behaviors;
using OIO.Application.Abstractions.Settings;
using OIO.Application.Context.AuctionContext.Services;
using OIO.Application.Context.MediaContext.Services;
using OIO.Application.Context.ModerationContext.Services;
using OIO.Application.Context.NotificationContext.Services;
using OIO.Application.Context.OrderContext.Services;
using OIO.Application.Context.UserContext.Services;

namespace OIO.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.Configure<MediatrOption>(configuration.GetSection(MediatrOption.SectionName));
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

        services.AddScoped<INotificationRoutingService, NotificationRoutingService>();
        services.AddScoped<ContinueVerifiedAuctionService>();
        services.AddScoped<AuctionDraftCreationService>();
        services.AddScoped<IAuctionCollusionDetectionService, AuctionCollusionDetectionService>();
        services.AddScoped<ItemShippingSelectionService>();
        services.AddScoped<IMediaRelocationService, MediaRelocationService>();
        services.AddScoped<EscrowSettlementService>();
        services.AddScoped<ModerationAuditService>();
        services.AddScoped<VerificationDuplicateIdentityService>();
        services.AddScoped<DisputeAccessService>();

        return services;
    }
}
