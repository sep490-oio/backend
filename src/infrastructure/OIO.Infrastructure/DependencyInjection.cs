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
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Mail;
using OIO.Application.Abstractions.Media;
using OIO.Application.Abstractions.Scheduling;
using OIO.Application.Abstractions.Security;
using OIO.Application.Abstractions.Settings;
using OIO.Application.Abstractions.Shipping;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.UserContext.Services;
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
            services.Configure<AppInfoOptions>(configuration.GetSection(AppInfoOptions.SectionName));
            services.AddScoped<IAppConfigs, AppConfig>();
            services.AddScoped<ISystemSettingsService, SystemSettingsService>();

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
                .AddShipping();

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
            services.AddSingleton<ISecureTokenGenerator, SecureTokenGenerator>();
            services.AddScoped<ISecureTokenStore, SecureTokenStore>();

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

                var scheduler = Guid.NewGuid();
                options.SchedulerId = $"default-id-{scheduler}";
                options.SchedulerName = $"default-name-{scheduler}";
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
            services.ConfigureOptions<AuctionJobSetup>();

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
        private IServiceCollection AddShipping()
        {
            services.AddHttpClient("GhnClient");
            services.AddTransient<IShippingProvider, GhnShippingProvider>();
            services.AddTransient<IShippingProviderSelector, ShippingProviderSelector>();
            services.AddScoped<IShippingService, ShippingService>();
            return services;
        }
    }
}