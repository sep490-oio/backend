using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.OrderContext.Services;
using OIO.Domain.Context.OrderContext.Aggregates.SellerDirectShipments;
using OIO.Domain.Context.OrderContext.Enums;
using OIO.Infrastructure.Persistence;

namespace OIO.Infrastructure.Scheduling.Jobs.Orders;

/// <summary>
/// One-shot startup repair job. Ensures every in-flight <see cref="SellerDirectShipment"/>
/// (pre-accepted / pre-disputed / pre-completed) has a signed QR token embedded
/// in its <c>QrPayload</c>. Legacy shipments from before the signed-token rollout
/// stored a plain id payload; this job reissues a version-1 token, rewrites the
/// payload as <c>{FeUrl}/me/shipments/scan?token=...</c>, and stamps the version
/// / issued-at audit fields on the aggregate.
///
/// Idempotent — re-running is a no-op once every eligible row carries the
/// canonical <c>/me/shipments/{id}/receive?token=</c> payload. Also re-issues
/// rows still using the legacy <c>/me/shipments/scan?token=</c> format.
/// Runs once at app start, in batches of 100.
/// </summary>
public sealed class BackfillDirectShipmentQrTokensJob : IHostedService
{
    private const int BatchSize = 100;
    private const int InitialVersion = 1;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackfillDirectShipmentQrTokensJob> _logger;

    public BackfillDirectShipmentQrTokensJob(
        IServiceScopeFactory scopeFactory,
        ILogger<BackfillDirectShipmentQrTokensJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("BackfillDirectShipmentQrTokensJob started.");

        try
        {
            var total = await RunAsync(cancellationToken);
            _logger.LogInformation(
                "BackfillDirectShipmentQrTokensJob completed. Repaired {Count} shipments.",
                total);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BackfillDirectShipmentQrTokensJob failed.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task<int> RunAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tokenService = scope.ServiceProvider.GetRequiredService<ISellerDirectShipmentTokenService>();
        var appInfo = scope.ServiceProvider.GetRequiredService<IAppInfo>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        var feBase = (appInfo.FeUrl ?? string.Empty).TrimEnd('/');
        var totalRepaired = 0;

        while (!ct.IsCancellationRequested)
        {
            // Candidates: in-flight shipments (not accepted/disputed/completed)
            // that either have no token issued yet, OR still carry the legacy
            // /me/shipments/scan?token= URL (needs rewrite to canonical format).
            var batch = await dbContext.Set<SellerDirectShipment>()
                .Where(s => s.Status != SellerDirectShipmentStatus.Accepted
                            && s.Status != SellerDirectShipmentStatus.Disputed
                            && s.Status != SellerDirectShipmentStatus.Completed
                            && (s.QrTokenIssuedAt == null
                                || s.QrPayload.Contains("/me/shipments/scan?token=")))
                .OrderBy(s => s.CreatedAt)
                .Take(BatchSize)
                .ToListAsync(ct);

            if (batch.Count == 0) break;

            var now = clock.UtcNow;
            foreach (var shipment in batch)
            {
                // Pull the buyer id from the Order so the token binding matches
                // the runtime Issue() contract used in the create command.
                var order = await dbContext.Set<OIO.Domain.Context.OrderContext.Aggregates.Orders.Order>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.Id == shipment.OrderId, ct);

                if (order is null)
                {
                    _logger.LogWarning(
                        "BackfillDirectShipmentQrTokensJob: skipping shipment {ShipmentId} — order not found.",
                        shipment.Id.Value);
                    continue;
                }

                var token = tokenService.Issue(
                    shipment.Id,
                    order.Id.Value,
                    order.BuyerId.Value,
                    InitialVersion,
                    now);

                var qrCodeUrl = $"{feBase}/me/shipments/{shipment.Id.Value}/receive?token={token}";
                shipment.OverwriteQrPayload(qrCodeUrl, qrCodeUrl, now);
                shipment.RecordQrTokenIssued(InitialVersion, now, now);
                totalRepaired++;
            }

            await dbContext.SaveChangesAsync(ct);

            // Defensive exit: if the batch is smaller than BatchSize, there is
            // nothing left. Avoids an extra empty query round-trip.
            if (batch.Count < BatchSize) break;
        }

        return totalRepaired;
    }
}
