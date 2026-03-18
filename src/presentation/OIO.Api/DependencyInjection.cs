using System.Text.Json;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions.HttpResults;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.OpenApi;
using Npgsql;
using OIO.Api.Extensions;
using OIO.Api.Hubs;
using OIO.Api.Middleware;
using OIO.Api.Services;
using OIO.Application.Context.AuctionContext.Services;
using OIO.Application.Context.ModerationContext.Services;
using OIO.Application.Context.NotificationContext.Services;
using OIO.Infrastructure.Settings;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace OIO.Api;

public static class DependencyInjection
{
    public static void AddApi(this IServiceCollection services, IConfiguration configuration)
    {
       services.Configure<RouteHandlerOptions>(o =>
        {
            o.ThrowOnBadRequest = false;
        });
        services.AddValidation();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.CustomSchemaIds(type =>
            {
                return BuildId(type);

                string BuildId(Type t)
                {
                    if (t.IsGenericType)
                    {
                        var baseName = t.Name[..t.Name.IndexOf('`')];
                        var args = string.Join("", t.GetGenericArguments().Select(BuildId));
                        return $"{baseName}{args}";
                    }

                    if (!t.IsNested || t.DeclaringType is null) return t.Name;

                    var parent = t.DeclaringType.Name;
                    if (parent.EndsWith("Endpoint", StringComparison.Ordinal))
                        parent = parent[..^"Endpoint".Length];

                    return $"{parent}{t.Name}";
                }
            });
            
            options.SwaggerDoc("v1", new OpenApiInfo()
            {
                Title = "OIO Auction API",
                Version = "v1",
                Description = "Online auction platform API"
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter your JWT token"
            });

            options.AddSecurityRequirement(document => 
                new OpenApiSecurityRequirement()
                {
                    [
                        new OpenApiSecuritySchemeReference("Bearer", document)
                    ] = []
                });
            
            options.AddSignalRSwaggerGen(); 
        });

        services.AddControllerConfigure();
        services.AddErrorHandling();
        services.AddEndpoints(typeof(Program).Assembly);
        services.AddCorsPolicy(configuration);
        services.AddSingleton<IdempotencyCacheService>();
        services.AddSingleton<AuctionBidIdempotencyHubFilter>();
        services.AddScoped<IAuctionNotificationService, AuctionNotificationService>();
        services.AddScoped<INotificationProvider, SignalRNotificationProvider>();
        services.AddScoped<IDisputeRealtimeService, DisputeRealtimeService>();
        services.AddSignalR()
            .AddHubOptions<AuctionHub>(options => options.AddFilter<AuctionBidIdempotencyHubFilter>());
    }

    private static void AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        var cors = configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>()!;

        services.AddCors(options =>
        {
            options.AddPolicy(CorsOptions.PolicyName, policy =>
            {
                policy
                    .WithOrigins(cors.AllowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });
    }

    private static void AddControllerConfigure(this IServiceCollection services)
    {
        // services
        //     .AddControllers()
        //     .AddNewtonsoftJson(options =>
        //     {
        //         options.SerializerSettings.ContractResolver =
        //             new CamelCasePropertyNamesContractResolver();
        //         options.SerializerSettings.DefaultValueHandling = DefaultValueHandling.Ignore;
        //     });
        
        services.ConfigureHttpJsonOptions(o =>
        {
            o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            o.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase; 
            o.SerializerOptions.PropertyNameCaseInsensitive = true;
            o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });

    }
    
    private static void AddErrorHandling(this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
                options.CustomizeProblemDetails = ctx =>
                {
                    ctx.ProblemDetails.Instance = $"{ctx.HttpContext.Request.Method} {ctx.HttpContext.Request.Path}";
                    ctx.ProblemDetails.Extensions.TryAdd("requestId", ctx.HttpContext.TraceIdentifier);
                    var activity = ctx.HttpContext.Features.Get<IHttpActivityFeature>()?.Activity;
                    ctx.ProblemDetails.Extensions.TryAdd("traceId", activity?.Id);

                    if (ctx.ProblemDetails.Status == StatusCodes.Status400BadRequest)
                    {
                        var (title, type) = ProblemDetailsMappingProvider.FindMapping(StatusCodes.Status400BadRequest);
                        ctx.ProblemDetails.Type = type;
                        ctx.ProblemDetails.Title = title;
                        ctx.ProblemDetails.Extensions["code"] = "General.Validations";
                    }
                }
            );
        services.AddExceptionHandler<GlobalExceptionHandler>();
    }
    
    public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder)
    {
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(builder.Environment.ApplicationName))
            .WithTracing(tracing =>
                tracing
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddQuartzInstrumentation()
                    .AddNpgsql())
            .WithMetrics(metrics =>
                metrics
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddNpgsqlInstrumentation())
            .UseOtlpExporter();

        builder.Logging.AddOpenTelemetry(options =>
        {
            options.IncludeScopes = true;
            options.IncludeFormattedMessage = true;
        });

        return builder;
    }
}
