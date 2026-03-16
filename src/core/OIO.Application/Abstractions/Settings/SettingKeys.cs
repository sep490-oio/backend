namespace OIO.Application.Abstractions.Settings;

public static class SettingKeys
{
    // Auction
    public const string AuctionMaxExtensions = "auction:max_extensions";
    public const string AuctionExtensionThreshold = "auction:extension_threshold";
    public const string AuctionMaxDuration = "auction:max_duration";
    public const string AuctionMinDuration = "auction:min_duration";
    public const string AuctionRunnerUpOfferExpirationHours = "auction:runner_up_offer_expiration_hours";

    // Items
    public const string ItemMaxQuestions = "item:max_questions";

    // Orders
    public const string OrderReturnDecisionWindowDays = "order:return_decision_window_days";

    // Ops
    public const string OpsAutoSuspendOnEmergency = "ops:auto_suspend_on_emergency";
    public const string OpsAutoSuspendAfterNonPaymentCount = "ops:auto_suspend_after_non_payment_count";

    // Monitoring
    public const string MonitoringInvalidBidBurstThreshold = "monitoring:invalid_bid_burst_threshold";
    public const string MonitoringBidBurstThreshold = "monitoring:bid_burst_threshold";

    // Media
    public const string MediaSignatureExpiration = "media:signature_expiration_minutes";
    public const string MediaOrphanExpiration = "media:orphan_expiration_minutes";
    public const string MediaLinkedRetention = "media:linked_retention_days";
    public const string MediaCleanupInterval = "media:cleanup_interval_minutes";
    public const string MediaUploadContexts = "media:upload_contexts";

    // Auth
    public const string AuthPasswordResetExpiration = "auth:password_reset_expiration_minutes";
    public const string EmailVerificationTokenExpiration = "auth:email_verification_token_expiration_minutes";
    public const string PhoneVerificationTokenExpiration = "auth:phone_verification_token_expiration_minutes";
    public const string TwoFactorSetupTokenExpiration = "auth:two_factor_setup_token_expiration_minutes";
    public const string AuthResendEmailCooldown = "auth:resend_email_cooldown_seconds";
    public const string AuthMaxPasswordResetAttempts = "auth:max_password_reset_attempts_per_hour";
    
}
