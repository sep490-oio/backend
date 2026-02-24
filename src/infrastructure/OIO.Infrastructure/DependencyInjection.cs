using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.UserContext.Services;
using OIO.Domain.Constants.AppPermissions;
using OIO.Domain.Context.UserContext.Repositories;
using OIO.Domain.Context.UserContext.Services;
using OIO.Infrastructure.Authorizations;
using OIO.Infrastructure.BackgroundJobs.CleanUpJobs;
using OIO.Infrastructure.Clock;
using OIO.Infrastructure.HealthChecks;
using OIO.Infrastructure.Persistence;
using OIO.Infrastructure.Persistence.Interceptors;
using OIO.Infrastructure.Persistence.Repositories;
using OIO.Infrastructure.Services;
using OIO.Infrastructure.Settings;

namespace OIO.Infrastructure;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddInfrastructure(
            IConfiguration configuration)
        {
            services.AddSingleton(TimeProvider.System);
            services.AddSingleton<IClock, DatetimeProvider>();
            services
                .AddPersistence(configuration)
                .AddAuthenticationServices(configuration)
                .AddAuthorizationService()
                .AddCachingService(configuration)
                .AddHealthCheckService()
                .AddBackgroundJobs();

            return services;
        }
        
        private IServiceCollection AddPersistence(
            IConfiguration configuration)
        {
            // Interceptors
            services.AddSingleton<AuditableEntityInterceptor>();
            services.AddSingleton<SoftDeleteInterceptor>();
            services.AddSingleton<ConcurrencyInterceptor>();
            services.AddSingleton<InsertOutboxMessagesInterceptor>();

            // DbContext
            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                var connectionString = configuration.GetConnectionString("Database")
                                       ?? throw new InvalidOperationException("Database connection string is not configured.");

                options.UseNpgsql(connectionString, npgsqlOptions =>
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

            // Repositories
            services.AddScoped<IUserRepository, UserRepository>();

            // Unit of Work
            services.AddScoped<IDbContext>(serviceProvider => serviceProvider.GetRequiredService<ApplicationDbContext>());

            services.AddScoped<IUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<ApplicationDbContext>());

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
                .AddJwtBearer(options =>
                {
                    options.EventsType =  typeof(CustomJwtBearerEvents);
                })
                .AddJwtBearer(AppPermission.ExpiredTokenAllowed,options =>
                {
                    options.EventsType = typeof(CustomJwtBearerEvents);
                });
            
            services.AddAuthorizationBuilder()
                .AddPolicy(AppPermission.ExpiredTokenAllowed, policy =>
                {
                    policy.AddAuthenticationSchemes(AppPermission.ExpiredTokenAllowed);
                    policy.RequireAuthenticatedUser();
                });
            
            // Settings
            services.Configure<JwtOptions>(
                configuration.GetSection(JwtOptions.SectionName));
            services.ConfigureOptions<JwtBearerOptionsSetup>();
            services.ConfigureOptions<JwtBearerOptionsForExpiredTokenSetup>();

            // Domain Services
            services.AddSingleton<IPasswordHasher, PasswordHasher>();
            services.AddSingleton<ITokenHasher, TokenHasher>();

            // Application Services
            services.AddSingleton<ITokenExpirationSettings, TokenExpirationSettings>();
            services.AddScoped<ITokenProvider, TokenProvider>();
            services.AddScoped<ICurrentUser, CurrentUser>();
            services.AddScoped<IEmailConfirmationService, EmailConfirmationService>();
            services.AddScoped<IPhoneVerificationService, PhoneVerificationService>();

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
            var redisConnection = configuration.GetConnectionString("Redis");

            if (!string.IsNullOrEmpty(redisConnection))
            {
                // services.AddStackExchangeRedisCache(options =>
                // {
                //     options.Configuration = redisConnection;
                //     options.InstanceName = "oio:";
                // });
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

        private IServiceCollection AddHealthCheckService()
        {
            // Health Checks
            services.AddHealthChecks()
                .AddCheck<DatabaseHealthCheck>("database");

            return services;
        }
        
        private IServiceCollection AddBackgroundJobs()
        {
            services.AddHostedService<ExpiredSessionCleanupJob>();
            return services;
        }
    }
}