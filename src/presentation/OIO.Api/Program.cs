using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OIO.Api;
using OIO.Api.Extensions;
using OIO.Application;
using OIO.Infrastructure;
using OIO.Infrastructure.Persistence.Extensions;
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

var app = builder.Build();

app.UseStaticFiles();

// Configure the HTTP request pipeline.
var enableScalar = builder.Configuration.GetValue<bool>("Features:EnableScalar");
if (app.Environment.IsDevelopment() || enableScalar)
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
}

app.UseHttpsRedirection();
app.UseExceptionHandler();
app.UseRequestContextLogging();
app.UseSerilogRequestLogging();
app.UseCors(CorsOptions.PolicyName);

app.UseAuthentication();
app.UseAuthorization();
app.MapEndpoints();

app.MapHealthChecks("health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();
