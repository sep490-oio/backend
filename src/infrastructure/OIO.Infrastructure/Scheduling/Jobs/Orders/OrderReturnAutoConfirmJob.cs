using System.Diagnostics;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.OrderContext.Commands.ConfirmOrderReturnReceived;
using OIO.Application.Context.OrderContext.Commands.CreateAdminRefundRetryTicket;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Enums;
using Quartz;

namespace OIO.Infrastructure.Scheduling.Jobs.Orders;

/// <summary>
/// Daily sweep (03:15 UTC) that auto-confirms an <see cref="OrderReturn"/>
/// if the seller did not confirm receipt within 14 days of the shipment
/// shipping back. Mirrors <c>WarehouseReturnAutoConfirmJob</c> — keeps the
/// aggregate as the single transition authority via
/// <c>ConfirmOrderReturnReceivedCommandHandler</c>.
/// </summary>
/// <remarks>
/// Offset 15 minutes past the <c>WarehouseReturnAutoConfirmJob</c> (03:00)
/// and 1h 15m past <c>OrderReturnDeadlineWatcherJob</c> (02:00) to avoid
/// simultaneous DB-contention spikes. Cron: <c>0 15 3 * * ?</c>.
/// <para>
/// D1 compensating-path interaction: if the seller never uploaded receipt
/// evidence, the <see cref="OrderReturn.Resolve"/> guard (D3) rejects and
/// the row is SKIPPED, logged at Warning, and an admin retry ticket is
/// created so operators can chase the missing evidence.
/// </para>
/// </remarks>
[DisallowConcurrentExecution]
internal sealed class OrderReturnAutoConfirmJob : IJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IClock _clock;
    private readonly IOptionsMonitor<AppLoggingOptions> _loggingOptions;
    private readonly ILogger<OrderReturnAutoConfirmJob> _logger;

    public OrderReturnAutoConfirmJob(
        IServiceScopeFactory scopeFactory,
        IClock clock,
        IOptionsMonitor<AppLoggingOptions> loggingOptions,
        ILogger<OrderReturnAutoConfirmJob> logger)
    {
        _scopeFactory   = scopeFactory;
        _clock          = clock;
        _loggingOptions = loggingOptions;
        _logger         = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        using var scope = _scopeFactory.CreateScope();
        var dbContext  = scope.ServiceProvider.GetRequiredService<IDbContext>();
        var sender     = scope.ServiceProvider.GetRequiredService<ISender>();

        var ct  = context.CancellationToken;
        var now = _clock.UtcNow;
        var shippedCutoff = now.AddDays(-14);

        // SellerReceived && SellerConfirmedReceivedAt != null means the scan
        // flipped status, but the seller has not confirmed/resolved yet. We
        // query on ShippedAt so the 14-day window is anchored to the buyer's
        // actual ship-back date (SellerReceivedAt is only set after scan).
        // We include both ReturnInTransit (seller never scanned) and
        // SellerReceived (scanned but not confirmed) — per plan D1.
        var eligible = await dbContext.Set<OrderReturn>()
            .Where(r => (r.Status == OrderReturnStatus.ReturnInTransit
                         || r.Status == OrderReturnStatus.SellerReceived)
                     && r.ShippedAt != null
                     && r.ShippedAt < shippedCutoff)
            .OrderBy(r => r.ShippedAt)
            .Take(200)
            .ToListAsync(ct);

        var confirmedCount       = 0;
        var skippedMissingCount  = 0;
        var failedCount          = 0;

        foreach (var orderReturn in eligible)
        {
            try
            {
                // D1 evidence guard: auto-confirm MUST NOT succeed if the seller
                // never uploaded receipt evidence. Resolve() will reject anyway,
                // but the handler would then log ERROR. Fail fast with a clear
                // admin ticket instead so the stuck return surfaces.
                if (!orderReturn.HasReceiptEvidence)
                {
                    skippedMissingCount++;
                    _logger.LogWarning(
                        "OrderReturnAutoConfirmJob: SKIPPED OrderReturn {OrderReturnId} — " +
                        "no seller receipt evidence. Creating admin ticket.",
                        orderReturn.Id.Value);

                    await sender.Send(new CreateAdminRefundRetryTicketCommand(
                        OrderId:       orderReturn.OrderId.Value,
                        OrderReturnId: orderReturn.Id.Value,
                        Intent:        orderReturn.DeferredRefundIntent?.Id ?? "unknown",
                        Amount:        orderReturn.DeferredRefundAmount,
                        FailureReason: "auto-confirm blocked — no seller receipt evidence"), ct);
                    continue;
                }

                // Run the full manual-confirm path via MediatR so the handler's
                // D1/D3/D6 guarantees (Resolve-before-refund, refund via policy,
                // compensating admin ticket on RefundBuyerAsync failure) apply.
                var result = await sender.Send(
                    new ConfirmOrderReturnReceivedCommand(
                        OrderId:  orderReturn.OrderId.Value,
                        ReturnId: orderReturn.Id.Value),
                    ct);

                if (result.IsFailure)
                {
                    failedCount++;
                    _logger.LogWarning(
                        "OrderReturnAutoConfirmJob: ConfirmOrderReturnReceived failed for OrderReturn {OrderReturnId}: {Error}",
                        orderReturn.Id.Value, result.Error.Message);
                    continue;
                }

                confirmedCount++;
                _logger.LogInformation(
                    "OrderReturn {OrderReturnId} auto-confirmed after 14-day seller-confirm window.",
                    orderReturn.Id.Value);
            }
            catch (Exception ex)
            {
                failedCount++;
                _logger.LogError(ex,
                    "OrderReturnAutoConfirmJob: error confirming OrderReturn {OrderReturnId}",
                    orderReturn.Id.Value);
            }
        }

        stopwatch.Stop();
        var logging = _loggingOptions.CurrentValue.Jobs;

        if (eligible.Count > 0 || failedCount > 0)
        {
            var level = failedCount > 0 || stopwatch.ElapsedMilliseconds >= logging.SlowJobThresholdMs
                ? LogLevel.Warning
                : LogLevel.Information;

            _logger.Log(
                level,
                "OrderReturnAutoConfirmJob completed in {DurationMs}ms. Eligible={Eligible}, Confirmed={Confirmed}, SkippedMissingEvidence={Skipped}, Failed={Failed}",
                stopwatch.ElapsedMilliseconds, eligible.Count, confirmedCount, skippedMissingCount, failedCount);
        }
        else if (logging.LogNoopRuns)
        {
            _logger.LogDebug(
                "OrderReturnAutoConfirmJob found no eligible returns in {DurationMs}ms.",
                stopwatch.ElapsedMilliseconds);
        }
    }
}
