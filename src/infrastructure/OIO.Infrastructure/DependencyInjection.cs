using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Npgsql;
using OIO.Application.Abstractions.Address;
using OIO.Application.Abstractions.Auth;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Mail;
using OIO.Application.Abstractions.Media;
using OIO.Application.Abstractions.Scheduling;
using OIO.Application.Abstractions.Security;
using OIO.Application.Abstractions.Shipping;
using OIO.Application.Abstractions.Search;
using OIO.Infrastructure.Elasticsearch;
using OIO.Application.Context.AuctionContext.Services;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.UserContext.Services;
using OIO.Application.Abstractions.Ekyc;
using OIO.Application.Abstractions.Payment;
using OIO.Application.Context.NotificationContext.Services;
using OIO.Infrastructure.Authorizations;
using OIO.Infrastructure.Clock;
using OIO.Infrastructure.Persistence;
using OIO.Infrastructure.Persistence.Interceptors;
using OIO.Infrastructure.Persistence.Repositories;
using OIO.Infrastructure.Services;
using OIO.Infrastructure.Settings;
using OIO.Domain.AppDefinitions;
using OIO.Infrastructure.Mail;
using OIO.Infrastructure.Mail.RazorEmails.Rendering;
using OIO.Infrastructure.Media;
using OIO.Infrastructure.Outbox;
using OIO.Infrastructure.Scheduling;
using OIO.Infrastructure.Scheduling.Jobs;
using OIO.Infrastructure.Scheduling.JobSetup;
using OIO.Infrastructure.Security;
using OIO.Infrastructure.Settings.Apps;
using Quartz;
using Microsoft.AspNetCore.DataProtection;
using StackExchange.Redis;
using OIO.Infrastructure.Auth;
using OIO.Infrastructure.Ekyc;
using OIO.Infrastructure.Elasticsearch.Jobs;
using OIO.Infrastructure.Notification.BackgroundJobs;
using OIO.Infrastructure.Notification.Providers;
using OIO.Infrastructure.Payment.Reconciliation;
using OIO.Infrastructure.Payment.VnPay;
using OIO.Infrastructure.Payment.Webhooks;
using OIO.Infrastructure.Scheduling.Jobs.Auctions;
using OIO.Infrastructure.Scheduling.Jobs.Orders;
using OIO.Infrastructure.Shipping;
using OIO.Infrastructure.Shipping.Ghn;

namespace OIO.Infrastructure;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddInfrastructure(
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("Database")
                                   ?? throw new InvalidOperationException(
                                       "Database connection string is not configured.");
            
            services.AddSingleton(TimeProvider.System);
            services.AddSingleton<IClock, DatetimeProvider>();
            services.Configure<AppOptions>(configuration.GetSection(AppOptions.SectionName));
            services.Configure<FeaturesOptions>(configuration.GetSection(FeaturesOptions.SectionName));
            services.Configure<AuctionOptions>(configuration.GetSection(AuctionOptions.SectionName));
            services.Configure<ItemOptions>(configuration.GetSection(ItemOptions.SectionName));
            services.Configure<MediaOptions>(configuration.GetSection(MediaOptions.SectionName));
            services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));
            services.Configure<MonitoringOptions>(configuration.GetSection(MonitoringOptions.SectionName));
            services.Configure<OpsOptions>(configuration.GetSection(OpsOptions.SectionName));
            services.Configure<OrderOptions>(configuration.GetSection(OrderOptions.SectionName));
            services.AddSingleton<AppConfig>();
            services.AddSingleton<IAppInfo>(serviceProvider => serviceProvider.GetRequiredService<AppConfig>());
            services.AddSingleton<IRuntimeSettings>(serviceProvider => serviceProvider.GetRequiredService<AppConfig>());

            services
                .AddPersistence(configuration, connectionString)
                .AddAuthenticationServices(configuration)
                .AddAuthorizationService()
                .AddCachingService(configuration)
                .AddHealthCheckService(configuration)
                .AddEmail(configuration)
                .AddBackgroundJobs()
                .AddSchedulingServices(configuration, connectionString)
                .AddOutbox(configuration)
                .AddMedia(configuration)
                .AddSecurityServices(configuration)
                .AddShipping(configuration)
                .AddEkyc(configuration)
                .AddPayment(configuration)
                .AddElasticsearch(configuration);

            services.AddMediatR(cfg =>
            {
                // Exclude IdempotentDomainEventHandler<> from auto-registration. MediatR's assembly
                // scanner would otherwise register it as an OPEN-GENERIC INotificationHandler<>,
                // applying it to ALL events including handler-less ones (e.g., RefreshTokenRotatedEvent),
                // causing a circular dependency when DI tries to resolve the decorator's inner handler
                // (which would be itself).
                cfg.TypeEvaluator = type =>
                    type != typeof(IdempotentDomainEventHandler<>);
                cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            });

            // Decorate only INotificationHandler<T> types that have real (non-decorator) registrations.
            // This MUST run after all AddMediatR calls so all handlers from both assemblies are registered.
            DecorateRegisteredNotificationHandlers(services);

            return services;
        }
        
        private IServiceCollection AddPersistence(
            IConfiguration configuration, string connectionString)
        {
            // Interceptors
            services.AddSingleton<AuditableEntityInterceptor>();
            services.AddSingleton<SoftDeleteInterceptor>();
            services.AddSingleton<ConcurrencyInterceptor>();
            services.AddSingleton<InsertOutboxMessagesInterceptor>();
            
            services.AddNpgsqlDataSource(connectionString);
            // DbContext
            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                var dataSource = sp.GetRequiredService<NpgsqlDataSource>();
                options.UseNpgsql(dataSource, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history");

                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null);

                    npgsqlOptions.CommandTimeout(30);
                }).UseSnakeCaseNamingConvention();

                options.AddInterceptors(
                    sp.GetRequiredService<AuditableEntityInterceptor>(),
                    sp.GetRequiredService<SoftDeleteInterceptor>(),
                    sp.GetRequiredService<ConcurrencyInterceptor>(),
                    sp.GetRequiredService<InsertOutboxMessagesInterceptor>());

                if (sp.GetRequiredService<IHostEnvironment>().IsDevelopment())
                {
                    options.EnableSensitiveDataLogging()
                        .EnableDetailedErrors();
                }
            });


            // Unit of Work
            services.AddScoped<IDbContext>(serviceProvider =>
                serviceProvider.GetRequiredService<ApplicationDbContext>());

            services.AddScoped<IUnitOfWork>(serviceProvider =>
                serviceProvider.GetRequiredService<ApplicationDbContext>());

            // Repositories
            services.AddScoped<IAuctionLockRepository, AuctionLockRepository>();

            return services;
        }

        private IServiceCollection AddAuthenticationServices(IConfiguration configuration)
        {
            services.AddScoped<CustomJwtBearerEvents>();

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options => { options.EventsType = typeof(CustomJwtBearerEvents); })
                .AddJwtBearer(App.Policy.ExpiredTokenAllowed,
                    options => { options.EventsType = typeof(CustomJwtBearerEvents); });

            services.AddAuthorizationBuilder()
                .AddPolicy(App.Policy.ExpiredTokenAllowed, policy =>
                {
                    policy.AddAuthenticationSchemes(App.Policy.ExpiredTokenAllowed);
                    policy.RequireAuthenticatedUser();
                });

            // Settings
            services.Configure<JwtOptions>(
                configuration.GetSection(JwtOptions.SectionName));
            services.Configure<DefaultAccountOptions>(configuration.GetSection(DefaultAccountOptions.SectionName));
            services.Configure<CorsOptions>(configuration.GetSection(CorsOptions.SectionName));
            services.Configure<HashingOptions>(configuration.GetSection(HashingOptions.SectionName));

            services.ConfigureOptions<JwtBearerOptionsSetup>();
            services.ConfigureOptions<JwtBearerOptionsForExpiredTokenSetup>();

            // Domain Services
            services.AddSingleton<IPasswordHasher, PasswordHasher>();
            services.AddSingleton<ITokenHasher, TokenHasher>();

            // Application Services
            services.AddSingleton<ITokenExpirationSettings, TokenExpirationSettings>();
            services.AddScoped<IJwtTokenProvider, JwtTokenProvider>();
            services.AddSingleton<ITotpService, TotpService>();
            services.AddScoped<ISessionRevocationStore, SessionRevocationStore>();
            services.AddScoped<ICurrentUser, CurrentUser>();

            // HttpContextAccessor
            services.AddHttpContextAccessor();

            return services;
        }

        private IServiceCollection AddAuthorizationService()
        {
            services.AddAuthorization();
            services.AddScoped<IPermissionService, PermissionService>();
            services.AddTransient<IAuthorizationHandler, PermissionAuthorizationHandler>();
            services.AddSingleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();

            return services;
        }

        private IServiceCollection AddCachingService(IConfiguration configuration)
        {
            var redisConnection = configuration.GetConnectionString("Cache");

            if (!string.IsNullOrWhiteSpace(redisConnection))
            {
                IConnectionMultiplexer multiplexer = ConnectionMultiplexer.Connect(redisConnection);
                services.AddSingleton(multiplexer);
                services.AddStackExchangeRedisCache(options =>
                {
                    options.ConnectionMultiplexerFactory = () => Task.FromResult(multiplexer);
                    options.Configuration = redisConnection;
                    options.InstanceName = "oio:cache";
                });
            }
            else
            {
                services.AddDistributedMemoryCache();
            }

            services.AddHybridCache(options =>
            {
                // Maximum size of cached items
                options.MaximumPayloadBytes = 1024 * 1024 * 10; // 10MB
                options.MaximumKeyLength = 512; // 512 characters

                // Default timeouts
                options.DefaultEntryOptions = new HybridCacheEntryOptions()
                {
                    Expiration = TimeSpan.FromMinutes(30),
                    LocalCacheExpiration = TimeSpan.FromMinutes(5)
                };
            });

            return services;
        }

        private IServiceCollection AddSecurityServices(IConfiguration configuration)
        {
            var redisConnection = configuration.GetConnectionString("Cache");

            var dpBuilder = services.AddDataProtection();

            if (!string.IsNullOrWhiteSpace(redisConnection))
            {
                var dpMultiplexer = ConnectionMultiplexer.Connect(redisConnection);
                dpBuilder
                    .PersistKeysToStackExchangeRedis(dpMultiplexer, "DataProtection-Keys:OIO")
                    .SetApplicationName("oio-api");
            }

            services.AddSingleton<ISecureTokenGenerator, SecureTokenGenerator>();
            services.AddScoped<ISecureTokenStore, SecureTokenStore>();
            services.AddScoped<ISealedBidEncryptionService, SealedBidEncryptionService>();
            services.AddScoped<OIO.Application.Context.OrderContext.Services.ISellerDirectShipmentTokenService, SellerDirectShipmentTokenService>();
            services.AddScoped<OIO.Application.Context.WarehouseContext.Services.IOutboundShipmentQrTokenService, OutboundShipmentQrTokenService>();

            return services;
        }

        private IServiceCollection AddEmail(IConfiguration configuration)
        {
            services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
            services.AddTransient<IMailSender, MailSender>();
            services.AddSingleton<RazorViewRenderer>();
            services.AddScoped<IUserMailNotifier, UserMailNotifier>();

            return services;
        }

        public IServiceCollection AddMedia(IConfiguration configuration)
        {
            services.Configure<CloudinaryOptions>(configuration.GetSection(CloudinaryOptions.SectionName));
            services.AddScoped<UploadContextRegistry>();
            services.AddScoped<IMediaSignatureService, CloudinarySignatureService>();
            services.AddScoped<IMediaSignatureService, CloudinarySignatureService>();
services.AddScoped<IMediaDirectUploadService, CloudinaryDirectUploadService>();  
            return services;
        }


        private IServiceCollection AddHealthCheckService(IConfiguration configuration)
        {
            // Health Checks
            services.AddHealthChecks()
                .AddRedis(
                    configuration.GetConnectionString("Cache")!,
                    name: "redis",
                    failureStatus: HealthStatus.Unhealthy,
                    tags: ["ready", "cache"])
                .AddNpgSql(
                    name: "database",
                    failureStatus: HealthStatus.Unhealthy,
                    tags: ["ready", "db"])
                .AddCheck<OutboxHealthCheck>(
                    "outbox",
                    failureStatus: HealthStatus.Degraded,
                    tags: ["ready", "outbox"]);

            return services;
        }

        private IServiceCollection AddBackgroundJobs()
        {
            services.AddHostedService<ExpiredSessionCleanupJob>();
            services.AddHostedService<CancelExpiredOrdersJob>();
            services.AddHostedService<ScanOverdueSelfShipOrdersJob>();
            services.AddHostedService<BackfillDirectShipmentQrTokensJob>();
            services.AddHostedService<BackfillOutboundShipmentQrTokensJob>();
            services.AddHostedService<ExpireRunnerUpOffersJob>();
            services.AddHostedService<ExpireBuyNowReservationsJob>();
            services.AddHostedService<ScanActiveAuctionsForCollusionJob>();
            services.AddHostedService<BackfillScheduledAuctionStartsJob>();

            return services;
        }

        private IServiceCollection AddSchedulingServices(
            IConfiguration configuration,
            string connectionString)
        {
            services.AddQuartz(options =>
            {
                // Use DB persistence (survives restart)
                options.UsePersistentStore(store =>
                {
                    store.UsePostgres(pg =>
                    {
                        pg.ConnectionString = connectionString;
                        pg.TablePrefix = "quartz.qrtz_";
                    });
                    store.UseSystemTextJsonSerializer();
                });

                options.SchedulerId = "oio-scheduler";
                options.SchedulerName = "oio-scheduler";
            });

            services.AddQuartzHostedService(options =>
            {
                // Graceful shutdown: wait for running jobs to complete
                options.WaitForJobsToComplete = true;
            });

            services.AddScoped<IAuctionScheduler, QuartzAuctionScheduler>();

            // Job Setups
            services.ConfigureOptions<OutboxMessagesProcessorJobSetup>();
            services.ConfigureOptions<MediaUploadCleanupJobSetup>();
            services.ConfigureOptions<PendingUploadRelocationJobSetup>();
            services.ConfigureOptions<AuctionJobSetup>();
            services.ConfigureOptions<AuctionAutoCompleteJobSetup>();
            services.ConfigureOptions<RecalculateSellerTrustScoresJobSetup>();
            services.ConfigureOptions<SyncGhnAddressJobSetup>();
            services.AddScoped<SellerTrustScoreCalculator>();

            // Notification Delivery Job
            services.ConfigureOptions<ProcessNotificationDeliveriesJobSetup>();
            services.AddScoped<INotificationProvider, EmailNotificationProvider>();

            return services;
        }

        public IServiceCollection AddOutbox(IConfiguration configuration)
        {
            services.AddOptions<OutboxSettings>()
                .Bind(configuration.GetSection(OutboxSettings.SectionName))
                .ValidateDataAnnotations()
                .Validate(
                    validation: outboxSettings =>
                        outboxSettings.Interval > TimeSpan.Zero &&
                        outboxSettings.CleanupRetention > TimeSpan.Zero &&
                        outboxSettings.PoisonMessageRetention > TimeSpan.Zero,
                    failureMessage: "Outbox Interval, CleanupRetention, and PoisonMessageRetention must be greater than zero.")
                .Validate(
                    validation: outboxSettings =>
                        outboxSettings.MaxAttempts == OutboxConstants.MaxAttemptsIndexFilter,
                    failureMessage:
                        $"Outbox MaxAttempts must be {OutboxConstants.MaxAttemptsIndexFilter} " +
                        "to match the partial index idx_outbox_messages_unprocessed. " +
                        "Changing MaxAttempts requires a database migration to update the index filter.")
                .Validate(
                    validation: outboxSettings =>
                        outboxSettings.HealthyThreshold < outboxSettings.UnhealthyThreshold,
                    failureMessage: "Outbox HealthyThreshold must be less than UnhealthyThreshold.")
                .ValidateOnStart();
            services.AddTransient<IOutboxMessageResolver, OutboxMessageResolver>();
            services.ConfigureOptions<OutboxMessagesProcessorJobSetup>();
            services.AddScoped<OutboxProcessor>();

            return services;
        }

        private IServiceCollection AddEkyc(IConfiguration configuration)
        {
            services.Configure<VnptEkycOptions>(configuration.GetSection(VnptEkycOptions.SectionName));
            services.AddHttpClient<IEkycProvider, VnptEkycProvider>();

            return services;
        }

        private IServiceCollection AddShipping(IConfiguration configuration)
        {
            services.Configure<GhnAddressOptions>(
                configuration.GetSection(Settings.GhnAddressOptions.SectionName));
            services.AddHttpClient("GhnClient");
            services.AddHttpClient("GhnAddressClient");
            services.AddTransient<IShippingProvider, GhnShippingProvider>();
            services.AddTransient<IShippingProviderSelector, ShippingProviderSelector>();
            services.AddScoped<IShippingService, ShippingService>();
            services.AddScoped<IGhnAddressService, GhnAddressService>();
            return services;
        }

        private IServiceCollection AddPayment(IConfiguration configuration)
        {
            services.Configure<VnPayConfig>(
                configuration.GetSection(VnPayConfig.SectionName));
            services.AddHttpClient<IPaymentGatewayService, VnPayGateway>();

            services.ConfigureOptions<ProcessGatewayWebhooksJobSetup>();
            services.AddScoped<GatewayWebhookProcessor>();

            services.ConfigureOptions<GatewayReconciliationJobSetup>();

            return services;
        }

        private IServiceCollection AddElasticsearch(IConfiguration configuration)
        {
            services.Configure<ElasticsearchSettings>(
                configuration.GetSection(ElasticsearchSettings.SectionName));

            services.AddSingleton<IElasticsearchService, ElasticsearchService>();
            services.AddScoped<IElasticsearchSyncService, ElasticsearchSyncService>();

            // Jobs
            services.ConfigureOptions<ElasticsearchReconciliationJobSetup>();

            return services;
        }
        
    }
    
    private static void DecorateRegisteredNotificationHandlers(IServiceCollection services)
    {
        // Only decorate handlers for event types that satisfy IDomainEvent constraint,
        // since IdempotentDomainEventHandler<T> requires `where TDomainEvent : IDomainEvent`.
        // This excludes integration events (e.g., OrderPaidIntegrationEvent) that implement
        // INotification directly without implementing IDomainEvent.
        var domainEventInterface = typeof(Domain.SeedWork.DomainEvents.IDomainEvent);

        var handlerServiceTypes = services
            .Where(sd => sd.ServiceType.IsGenericType
                         && sd.ServiceType.GetGenericTypeDefinition() == typeof(INotificationHandler<>)
                         && domainEventInterface.IsAssignableFrom(sd.ServiceType.GetGenericArguments()[0]))
            .Select(sd => sd.ServiceType)
            .Distinct()
            .ToList();

        foreach (var serviceType in handlerServiceTypes)
        {
            var eventType = serviceType.GetGenericArguments()[0];
            var decoratorType = typeof(IdempotentDomainEventHandler<>).MakeGenericType(eventType);
            services.Decorate(serviceType, decoratorType);
        }
    }
}
