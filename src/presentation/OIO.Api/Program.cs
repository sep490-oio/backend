using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OIO.Api;
using OIO.Api.Extensions;
using OIO.Api.Hubs;
using OIO.Application;
using OIO.Application.Abstractions.Commons;
using OIO.Infrastructure;
using OIO.Infrastructure.Persistence.Extensions;
using OIO.Infrastructure.Persistence.Seed;
using OIO.Infrastructure.Settings;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, loggerConfig) =>
    loggerConfig.ReadFrom.Configuration(context.Configuration));
builder.AddObservability();


builder.Services.AddApi(builder.Configuration);
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

builder.Host.UseOrleans(siloBuilder =>
{
    siloBuilder.UseLocalhostClustering();
    siloBuilder.AddMemoryGrainStorageAsDefault();
});

var app = builder.Build();

app.UseStaticFiles();

// Configure the HTTP request pipeline.
var features = builder.Configuration
    .GetSection(FeaturesOptions.SectionName)
    .Get<FeaturesOptions>() ?? new FeaturesOptions();

if (app.Environment.IsDevelopment() || features.EnableScalar)
{
    app.MapSwagger("/openapi/{documentName}.json");
    app.MapScalarApiReference("/docs", options =>
    {
        //css config wrap line response for scalar
        options.HeadContent = """
                              <link rel="stylesheet" href="/css/scalar-wrap-line.css" />
                              """;
        options.WithTitle("OIO API")
            .ShowOperationId()
            .ExpandAllTags()
            .WithClassicLayout()
            .SortTagsAlphabetically()
            .SortOperationsByMethod();
       
    });
    
    app.MapGet("/", () => Results.Redirect("/docs"))
        .ExcludeFromDescription();
    
    await app.ApplyMigrationsAsync();
    await DatabaseSeeder.SeedAsync(app.Services);
    
    using var scope = app.Services.CreateScope();
    var sender = scope.ServiceProvider.GetRequiredService<MediatR.ISender>();
    await sender.Send(new OIO.Application.Context.SearchContext.Commands.BootstrapSync.BootstrapSearchCommand());
}

app.UseHttpsRedirection();
app.UseExceptionHandler();
app.UseRequestContextLogging();
app.UseAppRequestLogging();
app.UseCors(CorsOptions.PolicyName);

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapEndpoints();

app.MapHub<AuctionHub>("/hubs/auction");
app.MapHub<DisputeHub>("/hubs/disputes");
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHub<UserHub>("/hubs/user");

app.MapHealthChecks("health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapGet("test", (IAppInfo appInfo) => appInfo.BeUrl);

app.Run();
