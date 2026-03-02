using OIO.Api;
using OIO.Api.Extensions;
using OIO.Application;
using OIO.Infrastructure;
using OIO.Infrastructure.Persistence.Extensions;
using OIO.Infrastructure.Settings;
using OIO.ServiceDefaults;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddApi(builder.Configuration);
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.MapDefaultEndpoints();
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
            .SortTagsAlphabetically()
            .SortOperationsByMethod();
       
    });
    
    app.MapGet("/", () => Results.Redirect("/docs"))
        .ExcludeFromDescription();
    
    await app.ApplyMigrationsAsync();
    await DatabaseSeeder.SeedAsync(app.Services);
}

app.UseExceptionHandler();
app.UseHttpsRedirection();

app.UseCors(CorsOptions.PolicyName);

app.UseAuthentication();
app.UseAuthorization();
app.MapEndpoints();
// app.MapHealthChecks("/health");


app.Run();
