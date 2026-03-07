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
}