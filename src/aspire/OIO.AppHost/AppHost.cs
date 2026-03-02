#pragma warning disable ASPIREPIPELINES003
#pragma warning disable ASPIRECOMPUTE003
var builder = DistributedApplication.CreateBuilder(args);

// =========================
// Parameters
// =========================
// Secret
var mediatrLicenseKey = builder.AddParameter("mediatr-license-key", secret: true);

// Non-secret
var jwtSecretKey = builder.AddParameter("jwt-secret-key", secret: true);
var jwtAudience = builder.AddParameter("jwt-audience");
var jwtIssuer = builder.AddParameter("jwt-issuer");
var jwtAccessTokenExpiration = builder.AddParameter("jwt-access-token-expiration");
var jwtRefreshTokenExpiration = builder.AddParameter("jwt-refresh-token-expiration");
var jwtRefreshTokenFamilySlidingExpiration = builder.AddParameter("jwt-refresh-token-family-sliding-expiration");
var jwtRefreshTokenFamilyAbsoluteExpiration = builder.AddParameter("jwt-refresh-token-family-absolute-expiration");

var emailHost = builder.AddParameter("email-host");
var emailPassword = builder.AddParameter("email-password", secret: true);
var emailPort = builder.AddParameter("email-port");
var emailUsername = builder.AddParameter("email-username");
var emailUseStartTls = builder.AddParameter("email-use-start-tls");
var emailFromAddress = builder.AddParameter("email-from-address");
var emailFromName = builder.AddParameter("email-from-name");

var defaulAccountEmail = builder.AddParameter("default-account-email");
var defaulAccountUsername = builder.AddParameter("default-account-user-name");
var defaulAccountPassword = builder.AddParameter("default-account-password");
var defaulAccountFirstName = builder.AddParameter("default-account-first-name");
var defaulAccountLastName = builder.AddParameter("default-account-last-name");
var defaulAccountDisplayName = builder.AddParameter("default-account-display-name");


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

builder.AddProject<Projects.OIO_Api>("oio-api")
    // .WithHttpHealthCheck("/health")
    .WithReference(db, connectionName: "Database")
    .WaitFor(db)
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = "oio-api";
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
    })

    // Jwt
    .WithEnvironment("Jwt__SecretKey", jwtSecretKey)
    .WithEnvironment("Jwt__Audience", jwtAudience)
    .WithEnvironment("Jwt__Issuer", jwtIssuer)
    .WithEnvironment("Jwt__AccessTokenExpiration", jwtAccessTokenExpiration)
    .WithEnvironment("Jwt__RefreshTokenExpiration", jwtRefreshTokenExpiration)
    .WithEnvironment("Jwt__RefreshTokenFamilySlidingExpiration", jwtRefreshTokenFamilySlidingExpiration)
    .WithEnvironment("Jwt__RefreshTokenFamilyAbsoluteExpiration", jwtRefreshTokenFamilyAbsoluteExpiration)

    // MediatR
    .WithEnvironment("MediatR__LicenseKey", mediatrLicenseKey)

    // Email
    .WithEnvironment("Email__Host", emailHost)
    .WithEnvironment("Email__Port", emailPort)
    .WithEnvironment("Email__Username", emailUsername)
    .WithEnvironment("Email__Password", emailPassword)
    .WithEnvironment("Email__UseStartTls", emailUseStartTls)
    .WithEnvironment("Email__FromAddress", emailFromAddress)
    .WithEnvironment("Email__FromName", emailFromName)
    
    //DefaultAccount
    .WithEnvironment("DefaultAccount__Email", defaulAccountEmail)
    .WithEnvironment("DefaultAccount__UserName", defaulAccountUsername)
    .WithEnvironment("DefaultAccount__Password", defaulAccountPassword)
    .WithEnvironment("DefaultAccount__FirstName", defaulAccountFirstName)
    .WithEnvironment("DefaultAccount__LastName", defaulAccountLastName)
    .WithEnvironment("DefaultAccount__DisplayName", defaulAccountDisplayName);
builder.Build().Run();
