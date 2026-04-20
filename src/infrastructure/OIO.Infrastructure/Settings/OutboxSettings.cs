using System.ComponentModel.DataAnnotations;

namespace OIO.Infrastructure.Settings;

internal sealed record OutboxSettings
{
    public const string SectionName = "Outbox";

    public required TimeSpan Interval { get; init; }

    [Range(minimum: 1, maximum: int.MaxValue, ErrorMessage = "BatchSize must be greater than or equal to 1.")]
    public required int BatchSize { get; init; }

    [Range(minimum: 1, maximum: 100, ErrorMessage = "MaxAttempts must be between 1 and 100.")]
    public required int MaxAttempts { get; init; }

    public required TimeSpan CleanupRetention { get; init; }

    [Range(minimum: 1, maximum: 10_000, ErrorMessage = "CleanupBatchSize must be between 1 and 10_000.")]
    public required int CleanupBatchSize { get; init; }

    [Range(minimum: 1, maximum: 1000, ErrorMessage = "MaxCleanupBatchesPerRun must be between 1 and 1000.")]
    public required int MaxCleanupBatchesPerRun { get; init; }

    public required TimeSpan CleanupDelay { get; init; }

    /// <summary>
    /// Retention period for poison messages (processed_at IS NULL, attempt_count >= MaxAttempts)
    /// before they are eligible for cleanup. Default: 7 days.
    /// </summary>
    public TimeSpan PoisonMessageRetention { get; init; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Threshold for the outbox health check: queue depth below this is Healthy.
    /// </summary>
    [Range(minimum: 1, maximum: 100_000)]
    public int HealthyThreshold { get; init; } = 100;

    /// <summary>
    /// Threshold for the outbox health check: queue depth at or above this is Unhealthy.
    /// </summary>
    [Range(minimum: 1, maximum: 100_000)]
    public int UnhealthyThreshold { get; init; } = 1000;

    /// <summary>
    /// Threshold for the outbox health check: poison count at or above this triggers Degraded status.
    /// </summary>
    [Range(minimum: 1, maximum: 100_000)]
    public int PoisonThreshold { get; init; } = 50;
}