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
using OIO.Infrastructure.Settings.Apps;
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
var appInfo = new AppInfoOptions();
builder.Configuration.Bind(AppInfoOptions.SectionName, appInfo);

if (app.Environment.IsDevelopment() || appInfo.Features.EnableScalar)
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
    await FakeDataSeeder.SeedAsync(app.Services);
    
}

app.UseHttpsRedirection();
app.UseExceptionHandler();
app.UseRequestContextLogging();
app.UseSerilogRequestLogging();
app.UseCors(CorsOptions.PolicyName);

app.UseAuthentication();
app.UseAuthorization();
app.MapEndpoints();

app.MapHub<AuctionHub>("/hubs/auction");

app.MapHealthChecks("health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapGet("test", (IAppConfigs appInfos) => appInfos.BeUrl);

app.Run();
