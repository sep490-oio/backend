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
using OIO.Infrastructure.Authorizations;
using OIO.Infrastructure.Clock;
using OIO.Infrastructure.Persistence;
using OIO.Infrastructure.Persistence.Interceptors;
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
using StackExchange.Redis;
using OIO.Infrastructure.Auth;
using OIO.Infrastructure.Ekyc;
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
                .AddSecurityServices()
                .AddShipping(configuration)
                .AddEkyc(configuration)
                .AddPayment(configuration)
                .AddElasticsearch(configuration);

            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            });

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

        private IServiceCollection AddSecurityServices()
        {
            services.AddDataProtection();
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
                    tags: ["ready", "db"]);

            return services;
        }

        private IServiceCollection AddBackgroundJobs()
        {
            services.AddHostedService<ExpiredSessionCleanupJob>();
            services.AddHostedService<OIO.Infrastructure.Scheduling.Jobs.Orders.CancelExpiredOrdersJob>();
            services.AddHostedService<OIO.Infrastructure.Scheduling.Jobs.Orders.ScanOverdueSelfShipOrdersJob>();
            services.AddHostedService<OIO.Infrastructure.Scheduling.Jobs.Orders.BackfillDirectShipmentQrTokensJob>();
            services.AddHostedService<OIO.Infrastructure.Scheduling.Jobs.Orders.BackfillOutboundShipmentQrTokensJob>();
            services.AddHostedService<OIO.Infrastructure.Scheduling.Jobs.Auctions.ExpireRunnerUpOffersJob>();
            services.AddHostedService<OIO.Infrastructure.Scheduling.Jobs.Auctions.ExpireBuyNowReservationsJob>();
            services.AddHostedService<OIO.Infrastructure.Scheduling.Jobs.Auctions.ScanActiveAuctionsForCollusionJob>();
            services.AddHostedService<OIO.Infrastructure.Scheduling.Jobs.Auctions.BackfillScheduledAuctionStartsJob>();

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
            services.ConfigureOptions<OIO.Infrastructure.Notification.BackgroundJobs.ProcessNotificationDeliveriesJobSetup>();
            services.AddScoped<OIO.Application.Context.NotificationContext.Services.INotificationProvider, OIO.Infrastructure.Notification.Providers.EmailNotificationProvider>();

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
                        outboxSettings.CleanupRetention > TimeSpan.Zero,
                    failureMessage: "Outbox Interval and CleanupRetention must be greater than zero.")
                .ValidateOnStart();
            services.AddTransient<IOutboxMessageResolver, OutboxMessageResolver>();
            services.ConfigureOptions<OutboxMessagesProcessorJobSetup>();
            services.AddScoped<OutboxProcessor>();
            //For idempotent notification
            services.Decorate(typeof(INotificationHandler<>), typeof(IdempotentDomainEventHandler<>));

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
            services.Configure<Settings.GhnAddressOptions>(
                configuration.GetSection(Settings.GhnAddressOptions.SectionName));
            services.AddHttpClient("GhnClient");
            services.AddHttpClient("GhnAddressClient");
            services.AddTransient<IShippingProvider, GhnShippingProvider>();
            services.AddTransient<IShippingProviderSelector, ShippingProviderSelector>();
            services.AddScoped<IShippingService, ShippingService>();
            services.AddScoped<OIO.Application.Abstractions.Address.IGhnAddressService, GhnAddressService>();
            return services;
        }

        private IServiceCollection AddPayment(IConfiguration configuration)
        {
            services.Configure<Payment.VnPay.VnPayConfig>(
                configuration.GetSection(Payment.VnPay.VnPayConfig.SectionName));
            services.AddHttpClient<Application.Abstractions.Payment.IPaymentGatewayService,
                Payment.VnPay.VnPayGateway>();

            services.ConfigureOptions<Payment.Webhooks.ProcessGatewayWebhooksJobSetup>();
            services.AddScoped<Payment.Webhooks.GatewayWebhookProcessor>();

            services.ConfigureOptions<Payment.Reconciliation.GatewayReconciliationJobSetup>();

            return services;
        }

        private IServiceCollection AddElasticsearch(IConfiguration configuration)
        {
            services.Configure<ElasticsearchSettings>(
                configuration.GetSection(ElasticsearchSettings.SectionName));

            services.AddSingleton<IElasticsearchService, ElasticsearchService>();
            services.AddScoped<IElasticsearchSyncService, ElasticsearchSyncService>();

            // Jobs
            services.ConfigureOptions<Elasticsearch.Jobs.ElasticsearchReconciliationJobSetup>();

            return services;
        }
    }
}
