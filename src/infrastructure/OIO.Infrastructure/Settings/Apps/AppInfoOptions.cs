using OIO.Application.Abstractions.Commons;

namespace OIO.Infrastructure.Settings.Apps;

public sealed class AppInfoOptions 
{
    public const string SectionName = "AppInfo";

    public string AppName { get; set; } = "AppName";
    public string Version { get; set; } = "1.0.0";
    public string FeUrl { get; set; } = null!;
    public string BeUrl { get; set; } = null!;
    public string EmailVerifyPath { get; set; } = null!;
    public string ResetPasswordPath { get; set; } = null!;
    public Features Features { get; set; } = null!;
    
    public AuctionDefaults AuctionDefaults { get; set; } = new();
    public ItemDefaults ItemDefaults { get; set; } = new();
    public MediaDefaults MediaDefaults { get; set; } = new();
    public AuthDefaults AuthDefaults { get; set; } = new();

}

public sealed class Features : IFeatureConfigs
{
    public bool EnableScalar { get; set; } = false;
}

public sealed class AuctionDefaults
{
    public int MaxExtensionsPerAuction { get; set; } = 10;
    public TimeSpan ExtensionThresholdMinutes { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan MaxDuration { get; set; } = TimeSpan.FromDays(30);
    public TimeSpan MinDuration { get; set; } = TimeSpan.FromDays(1);
}


public sealed class ItemDefaults
{
    public int MaxQuestionsPerItem { get; set; } = 100;
}

public sealed class MediaDefaults
{
    public TimeSpan SignatureExpirationMinutes { get; set; } = TimeSpan.FromMinutes(30);
    public TimeSpan OrphanExpirationMinutes { get; set; } = TimeSpan.FromMinutes(60);
    public TimeSpan LinkedRecordRetentionDays { get; set; } = TimeSpan.FromDays(7);
    public TimeSpan CleanupIntervalMinutes { get; set; } = TimeSpan.FromMinutes(15);
    
    public List<UploadContextOption> UploadContexts { get; set; } = [];
    
}

public sealed class AuthDefaults
{
    public TimeSpan PasswordResetTokenExpirationMinutes { get; set; } = TimeSpan.FromMinutes(30);
    public TimeSpan ResendEmailCooldownSeconds { get; set; } = TimeSpan.FromSeconds(60);
    public TimeSpan EmailVerificationTokenExpirationMinutes { get; set; } = TimeSpan.FromMinutes(30);
    public TimeSpan TwoFactorSetupTokenExpirationMinutes { get; set; } = TimeSpan.FromMinutes(30);
    public TimeSpan PhoneVerificationTokenExpirationMinutes { get; set; } = TimeSpan.FromMinutes(30);
    public int MaxPasswordResetAttemptsPerHour { get; set; } = 5;
}