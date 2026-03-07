namespace OIO.Application.Abstractions.Commons;

public interface IAppConfigs
{
    string AppName { get; } 
    string Version { get; } 
    string FeUrl { get; } 
    string BeUrl  { get; } 
    string EmailVerifyPath  { get; } 
    string ResetPasswordPath { get; } 
    IFeatureConfigs Features  { get; } 
    
    IAuctionConfigs Auctions { get; }
    IItemConfigs Items { get; }
    IMediaConfigs Media { get; }
    IAuthConfigs Auth { get; }
}

public interface IFeatureConfigs
{
    bool EnableScalar { get; }
}

public interface IAuctionConfigs
{
    Task<int> GetMaxExtensionsPerAuctionAsync(CancellationToken cancellationToken = default);
    Task<TimeSpan> GetExtensionThresholdMinutesAsync(CancellationToken cancellationToken = default);
    Task<TimeSpan> GetMaxDurationAsync(CancellationToken cancellationToken = default);
    Task<TimeSpan> GetMinDurationAsync(CancellationToken cancellationToken = default);
}
public interface IItemConfigs
{
    Task<int> GetMaxQuestionsPerItemAsync(CancellationToken cancellationToken = default);
}

public interface IMediaConfigs
{
    Task<TimeSpan> GetSignatureExpirationMinutesAsync(CancellationToken cancellationToken = default);

    Task<TimeSpan> GetOrphanExpirationMinutesAsync(CancellationToken cancellationToken = default);

    Task<TimeSpan> GetLinkedRecordRetentionDaysAsync(CancellationToken cancellationToken = default);

    Task<TimeSpan> GetCleanupIntervalMinutesAsync(CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<UploadContextOption>> GetUploadContextsAsync(CancellationToken cancellationToken = default);
}

public interface IAuthConfigs
{
    Task<TimeSpan> GetPasswordResetTokenExpirationMinutesAsync(CancellationToken cancellationToken = default);
    Task<TimeSpan> GetEmailVerificationTokenExpirationMinutesAsync(CancellationToken cancellationToken = default);
    Task<TimeSpan> GetPhoneVerificationTokenExpirationMinutesAsync(CancellationToken cancellationToken = default);
    Task<TimeSpan> GetTwoFactorSetupTokenExpirationMinutesAsync(CancellationToken cancellationToken = default);
    Task<TimeSpan> GetResendEmailCooldownSecondsAsync(CancellationToken cancellationToken = default);
    Task<int> GetMaxPasswordResetAttemptsPerHourAsync(CancellationToken cancellationToken = default);
}

public class UploadContextOption
{
    public string Name { get; set; } = null!;
    public string ResourceType { get; set; } = null!;
    public string Folder { get; set; } = null!;
    public long MaxFileSizeBytes { get; set; }
    public string[] AllowedFormats { get; set; } = [];
    public string? Eager { get; set; }
    public int MaxUploadsPerEntity { get; set; }
}

