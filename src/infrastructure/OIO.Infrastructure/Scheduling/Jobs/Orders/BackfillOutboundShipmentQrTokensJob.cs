using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.WarehouseContext.Services;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Infrastructure.Persistence;

namespace OIO.Infrastructure.Scheduling.Jobs.Orders;

/// <summary>
/// One-shot startup repair job. Reissues QR tokens for legacy external-carrier
/// <see cref="OutboundShipment"/> rows that either have no token yet or carry a
/// relative-path payload from before the absolute deep-link rollout. Skips
/// terminal states (failed / returned / cancelled). Delivered shipments are
/// still backfilled so the buyer scan remains valid post-handover.
/// </summary>
public sealed class BackfillOutboundShipmentQrTokensJob : IHostedService
{
    private const int BatchSize = 100;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackfillOutboundShipmentQrTokensJob> _logger;

    public BackfillOutboundShipmentQrTokensJob(
        IServiceScopeFactory scopeFactory,
        ILogger<BackfillOutboundShipmentQrTokensJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("BackfillOutboundShipmentQrTokensJob started.");

        try
        {
            var total = await RunAsync(cancellationToken);
            _logger.LogInformation(
                "BackfillOutboundShipmentQrTokensJob completed. Repaired {Count} shipments.",
                total);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BackfillOutboundShipmentQrTokensJob failed.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task<int> RunAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tokenService = scope.ServiceProvider.GetRequiredService<IOutboundShipmentQrTokenService>();
        var appInfo = scope.ServiceProvider.GetRequiredService<IAppInfo>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        var feBase = (appInfo.FeUrl ?? string.Empty).TrimEnd('/');
        var totalRepaired = 0;

        while (!ct.IsCancellationRequested)
        {
            var batch = await dbContext.Set<OutboundShipment>()
                .Where(s => s.ShipmentMode == OutboundShipmentMode.ExternalCarrier
                            && s.Status != OutboundShipmentStatus.Failed
                            && s.Status != OutboundShipmentStatus.Returned
                            && s.Status != OutboundShipmentStatus.Cancelled
                            && (s.QrTokenIssuedAt == null
                                || s.QrPayload == null
                                || !s.QrPayload.StartsWith("http")))
                .OrderBy(s => s.CreatedAt)
                .Take(BatchSize)
                .ToListAsync(ct);

            if (batch.Count == 0) break;

            var now = clock.UtcNow;
            foreach (var shipment in batch)
            {
                try
                {
                    var order = await dbContext.Set<Order>()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(o => o.Id == shipment.OrderId, ct);

                    if (order is null)
                    {
                        _logger.LogWarning(
                            "BackfillOutboundShipmentQrTokensJob: skipping shipment {ShipmentId} — order not found.",
                            shipment.Id.Value);
                        continue;
                    }

                    var nextVersion = shipment.QrTokenVersion + 1;
                    var tokenString = tokenService.Issue(
                        shipmentId: shipment.Id,
                        orderId: order.Id.Value,
                        buyerId: order.BuyerId.Value,
                        version: nextVersion,
                        issuedAt: now);

                    var deepLink = $"{feBase}/orders/{order.Id.Value}/outbound-shipment/receive?token={tokenString}";

                    var issueResult = shipment.IssueQr(qrPayload: deepLink, qrCodeUrl: null, now);
                    if (issueResult.IsFailure)
                    {
                        _logger.LogWarning(
                            "BackfillOutboundShipmentQrTokensJob: IssueQr failed for {ShipmentId}: {Error}",
                            shipment.Id.Value, issueResult.Error.Code);
                        continue;
                    }

                    totalRepaired++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "BackfillOutboundShipmentQrTokensJob: row-level failure for shipment {ShipmentId}.",
                        shipment.Id.Value);
                }
            }

            await dbContext.SaveChangesAsync(ct);

            if (batch.Count < BatchSize) break;
        }

        return totalRepaired;
    }
}
