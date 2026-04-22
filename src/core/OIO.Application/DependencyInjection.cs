using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OIO.Application.Abstractions.Behaviors;
using OIO.Application.Abstractions.Caching;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Settings;
using OIO.Application.Context.AuctionContext.Services;
using OIO.Application.Context.MediaContext.Services;
using OIO.Application.Context.ModerationContext.Services;
using OIO.Application.Context.NotificationContext.Services;
using OIO.Application.Context.OrderContext.Services;
using OIO.Application.Context.PaymentContext.Services;
using OIO.Application.Context.UserContext.Services;
using OIO.Application.Context.WarehouseContext.Services;

namespace OIO.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.Configure<AppLoggingOptions>(configuration.GetSection(AppLoggingOptions.SectionName));
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
        services.AddScoped<AuctionActivationService>();
        services.AddScoped<AuctionStateSyncService>();
        services.AddScoped<IAuctionRealtimePublisher, AuctionRealtimePublisher>();
        services.AddScoped<IAutoBidRealtimePublisher, AutoBidRealtimePublisher>();
        services.AddScoped<IAuctionPositionPublisher, AuctionPositionPublisher>();
        services.AddScoped<IAuctionCollusionDetectionService, AuctionCollusionDetectionService>();
        services.AddScoped<ItemShippingSelectionService>();
        services.AddScoped<IMediaRelocationService, MediaRelocationService>();
        services.AddScoped<EscrowSettlementService>();
        services.AddScoped<IOrderReceiptService, OrderReceiptService>();
        services.AddScoped<IOrderDeliveryService, OrderDeliveryService>();
        services.AddScoped<IWinnerOrderProvisioner, WinnerOrderProvisioner>();
        services.AddScoped<BuyNowReservationFinalizer>();
        services.AddScoped<ModerationAuditService>();
        services.AddScoped<VerificationDuplicateIdentityService>();
        services.AddScoped<DisputeAccessService>();
        services.AddScoped<IDisputeResolutionService, DisputeResolutionService>();
        services.AddScoped<IDisputeIntakeService, DisputeIntakeService>();

        // Warehouse return-to-seller shipment factory — shared by the
        // inspection-rejected event handler and the admin retry command.
        services.AddScoped<IWarehouseReturnShipmentFactory, WarehouseReturnShipmentFactory>();

        // Forced re-acceptance (plan §3.6.4 / B6).
        services.AddSingleton<ICacheInvalidator, HybridCacheInvalidator>();
        services.AddScoped<IEnsureTermsAcceptedService, EnsureTermsAcceptedService>();

        return services;
    }
}
