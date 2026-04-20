using Dapper;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Npgsql;
using OIO.Infrastructure.Settings;

namespace OIO.Infrastructure.Outbox;

internal sealed class OutboxHealthCheck(
    NpgsqlDataSource dataSource,
    IOptionsMonitor<OutboxSettings> outboxSettingsOptions
) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var settings = outboxSettingsOptions.CurrentValue;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        const string sql = """
                           SELECT
                               COUNT(*) FILTER (WHERE processed_at IS NULL AND attempt_count < @MaxAttempts) AS unprocessed,
                               COUNT(*) FILTER (WHERE processed_at IS NULL AND attempt_count >= @MaxAttempts) AS poison
                           FROM outbox_messages
                           WHERE processed_at IS NULL;
                           """;

        var result = await connection.QuerySingleAsync<(long Unprocessed, long Poison)>(
            new CommandDefinition(
                commandText: sql,
                parameters: new { settings.MaxAttempts },
                cancellationToken: cancellationToken));

        var data = new Dictionary<string, object>
        {
            ["unprocessed"] = result.Unprocessed,
            ["poison"] = result.Poison,
            ["healthyThreshold"] = settings.HealthyThreshold,
            ["unhealthyThreshold"] = settings.UnhealthyThreshold,
            ["poisonThreshold"] = settings.PoisonThreshold
        };

        if (result.Unprocessed >= settings.UnhealthyThreshold)
        {
            return HealthCheckResult.Unhealthy(
                $"Outbox queue depth critically high: {result.Unprocessed} unprocessed messages.",
                data: data);
        }

        if (result.Unprocessed >= settings.HealthyThreshold || result.Poison >= settings.PoisonThreshold)
        {
            return HealthCheckResult.Degraded(
                $"Outbox degraded: {result.Unprocessed} unprocessed, {result.Poison} poison messages.",
                data: data);
        }

        return HealthCheckResult.Healthy(
            $"Outbox healthy: {result.Unprocessed} unprocessed, {result.Poison} poison messages.",
            data: data);
    }
}
