namespace OIO.Application.Abstractions.Commons;

public interface IAppInfo
{
    string AppName { get; }
    string Version { get; }
    string FeUrl { get; }
    string BeUrl { get; }
    string EmailVerifyPath { get; }
    string ResetPasswordPath { get; }
    FeaturesOptions Features { get; }
}

public interface IRuntimeSettings
{
    AuctionOptions Auction { get; }
    ItemOptions Item { get; }
    MediaOptions Media { get; }
    AuthOptions Auth { get; }
    MonitoringOptions Monitoring { get; }
    OpsOptions Ops { get; }
    OrderOptions Order { get; }
}

public sealed class AppOptions
{
    public const string SectionName = "App";

    public string AppName { get; set; } = "OIO Auction";
    public string Version { get; set; } = "1.0.0";
    public string FeUrl { get; set; } = null!;
    public string BeUrl { get; set; } = null!;
    public string EmailVerifyPath { get; set; } = null!;
    public string ResetPasswordPath { get; set; } = null!;
}

public sealed class FeaturesOptions
{
    public const string SectionName = "Features";

    public bool EnableScalar { get; set; }
}

public sealed class AuctionOptions
{
    public const string SectionName = "Auction";

    public int MaxExtensionsPerAuction { get; set; } = 10;
    public TimeSpan ExtensionThreshold { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan MaxDuration { get; set; } = TimeSpan.FromDays(30);
    public TimeSpan MinDuration { get; set; } = TimeSpan.FromDays(1);
    public int RunnerUpOfferExpirationHours { get; set; } = 24;
}

public sealed class ItemOptions
{
    public const string SectionName = "Item";

    public int MaxQuestionsPerItem { get; set; } = 100;
}

public sealed class MediaOptions
{
    public const string SectionName = "Media";

    public TimeSpan SignatureExpiration { get; set; } = TimeSpan.FromMinutes(30);
    public TimeSpan OrphanExpiration { get; set; } = TimeSpan.FromMinutes(60);
    public TimeSpan LinkedRecordRetention { get; set; } = TimeSpan.FromDays(7);
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(15);
    public List<UploadContextOption> UploadContexts { get; set; } = [];
}

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public TimeSpan PasswordResetTokenExpiration { get; set; } = TimeSpan.FromMinutes(30);
    public TimeSpan ResendEmailCooldown { get; set; } = TimeSpan.FromSeconds(60);
    public TimeSpan EmailVerificationTokenExpiration { get; set; } = TimeSpan.FromMinutes(30);
    public TimeSpan TwoFactorSetupTokenExpiration { get; set; } = TimeSpan.FromMinutes(30);
    public TimeSpan PhoneVerificationTokenExpiration { get; set; } = TimeSpan.FromMinutes(30);
    public int MaxPasswordResetAttemptsPerHour { get; set; } = 5;
}

public sealed class MonitoringOptions
{
    public const string SectionName = "Monitoring";

    public int InvalidBidBurstThreshold { get; set; } = 5;
    public int BidBurstThreshold { get; set; } = 10;
    public int AuctionCollusionSessionDeviceWindowDays { get; set; } = 90;
    public int AuctionCollusionSessionIpWindowDays { get; set; } = 30;
    public int AuctionCollusionPingPongWindowMinutes { get; set; } = 10;
    public int AuctionCollusionPingPongMinimumBids { get; set; } = 6;
    public int AuctionCollusionPingPongDominanceThresholdPercent { get; set; } = 80;
    public int AuctionCollusionRepeatedPairWindowDays { get; set; } = 30;
    public int AuctionCollusionRepeatedPairThreshold { get; set; } = 3;
}

public sealed class OpsOptions
{
    public const string SectionName = "Ops";

    public bool AutoSuspendOnEmergency { get; set; }
    public int AutoSuspendAfterNonPaymentCount { get; set; }
}

public sealed class OrderOptions
{
    public const string SectionName = "Order";

    public int ReturnDecisionWindowDays { get; set; } = 7;
    public int PaymentDeadlineHours { get; set; } = 48;
}

public sealed class UploadContextOption
{
    public string Name { get; set; } = null!;
    public string ResourceType { get; set; } = null!;
    public string Folder { get; set; } = null!;
    public long MaxFileSizeBytes { get; set; }
    public string[] AllowedFormats { get; set; } = [];
    public string? Eager { get; set; }
    public int MaxUploadsPerEntity { get; set; }
}
