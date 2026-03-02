#pragma warning disable ASPIREPIPELINES003
#pragma warning disable ASPIRECOMPUTE003
var builder = DistributedApplication.CreateBuilder(args);

var docker = builder.AddDockerComposeEnvironment("compose")
    .WithDashboard(enabled: false);

var registryEndpoint = builder.AddParameterFromConfiguration("registryEndpoint", "REGISTRY_ENDPOINT");
var registryRepository = builder.AddParameterFromConfiguration("registryRepository", "REGISTRY_REPOSITORY");

var registry = builder.AddContainerRegistry(
    "ghcr",
    registryEndpoint,
    registryRepository);

var db = builder.AddPostgres("database")
    .WithHostPort(15432)
    .WithDataVolume()
    .AddDatabase("oio-mcbc");


var api = builder.AddProject<Projects.OIO_Api>("oio-api")
    // .WithHttpHealthCheck("/health")
    .WithReference(db, connectionName: "Database")
    .WaitFor(db)
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = "oio-api";
        service.Ports.Add("8080:8080");

        // Lấy các biến môi trường đã được truyền từ GitHub Actions workflow
        var endpoint = Environment.GetEnvironmentVariable("REGISTRY_ENDPOINT") ?? "ghcr.io";
        // GHCR yêu cầu tên repo phải viết thường toàn bộ
        var repo = Environment.GetEnvironmentVariable("REGISTRY_REPOSITORY")?.ToLower();
        var version = Environment.GetEnvironmentVariable("APP_VERSION") ?? "latest";

        // Chỉ định rõ cấu trúc Image để ghi vào file docker-compose.yaml
        service.Image = $"{endpoint}/{repo}/oio-api:{version}";
    })
    .WithContainerRegistry(registry)
    .WithImagePushOptions(context =>
    {
        var version = Environment.GetEnvironmentVariable("APP_VERSION") ?? "latest";
        context.Options.RemoteImageTag = version;
    });

AddEnvIfNotNull("JWT_SECRET_KEY", "Jwt__SecretKey");
AddEnvIfNotNull("JWT_AUDIENCE", "Jwt__Audience");
AddEnvIfNotNull("JWT_ISSUER", "Jwt__Issuer");
AddEnvIfNotNull("JWT_ACCESS_TOKEN_EXPIRATION", "Jwt__AccessTokenExpiration");
AddEnvIfNotNull("JWT_REFRESH_TOKEN_EXPIRATION", "Jwt__RefreshTokenExpiration");
AddEnvIfNotNull("JWT_REFRESH_TOKEN_FAMILY_SLIDING_EXPIRATION", "Jwt__RefreshTokenFamilySlidingExpiration");
AddEnvIfNotNull("JWT_REFRESH_TOKEN_FAMILY_ABSOLUTE_EXPIRATION", "Jwt__RefreshTokenFamilyAbsoluteExpiration");
AddEnvIfNotNull("MEDIATR_LICENSE_KEY", "MediatR__LicenseKey");
AddEnvIfNotNull("EMAIL_HOST", "Email__Host");
AddEnvIfNotNull("EMAIL_PORT", "Email__Port");
AddEnvIfNotNull("EMAIL_USERNAME", "Email__Username");
AddEnvIfNotNull("EMAIL_PASSWORD", "Email__Password");
AddEnvIfNotNull("EMAIL_USE_START_TLS", "Email__UseStartTls");
AddEnvIfNotNull("EMAIL_FROM_ADDRESS", "Email__FromAddress");
AddEnvIfNotNull("EMAIL_FROM_NAME", "Email__FromName");
AddEnvIfNotNull("DEFAULT_ACCOUNT_EMAIL", "DefaultAccount__Email");
AddEnvIfNotNull("DEFAULT_ACCOUNT_USERNAME", "DefaultAccount__UserName");
AddEnvIfNotNull("DEFAULT_ACCOUNT_PASSWORD", "DefaultAccount__Password");
AddEnvIfNotNull("DEFAULT_ACCOUNT_FIRSTNAME", "DefaultAccount__FirstName");
AddEnvIfNotNull("DEFAULT_ACCOUNT_LASTNAME", "DefaultAccount__LastName");
AddEnvIfNotNull("DEFAULT_ACCOUNT_DISPLAYNAME", "DefaultAccount__DisplayName");

builder.Build().Run();

void AddEnvIfNotNull(string envName, string targetConfigKey)
{
    var value = Environment.GetEnvironmentVariable(envName);
    if (!string.IsNullOrEmpty(value))
    {
        api.WithEnvironment(targetConfigKey, value);
    }
}