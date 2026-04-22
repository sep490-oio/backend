using System.Diagnostics;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.OrderContext.Commands.CreateAdminRefundRetryTicket;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseToSellerShipments;
using OIO.Domain.Context.WarehouseContext.Enums;
using Quartz;

namespace OIO.Infrastructure.Scheduling.Jobs.Warehouses;

/// <summary>
/// Daily sweep (03:00 UTC) that auto-confirms a
/// <see cref="WarehouseToSellerShipment"/> if the seller did not confirm
/// receipt within 7 days of delivery. Keeps the aggregate as the single
/// transition authority — we call <c>ConfirmBySeller(now)</c> just like the
/// manual command does.
/// </summary>
[DisallowConcurrentExecution]
internal sealed class WarehouseReturnAutoConfirmJob : IJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IClock _clock;
    private readonly IOptionsMonitor<AppLoggingOptions> _loggingOptions;
    private readonly ILogger<WarehouseReturnAutoConfirmJob> _logger;

    public WarehouseReturnAutoConfirmJob(
        IServiceScopeFactory scopeFactory,
        IClock clock,
        IOptionsMonitor<AppLoggingOptions> loggingOptions,
        ILogger<WarehouseReturnAutoConfirmJob> logger)
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
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var sender     = scope.ServiceProvider.GetRequiredService<ISender>();

        var ct         = context.CancellationToken;
        var now        = _clock.UtcNow;
        var deliveredCutoff = now.AddDays(-7);

        var eligible = await dbContext.Set<WarehouseToSellerShipment>()
            .Include(s => s.Evidence)
            .Where(s => s.Status == WarehouseToSellerShipmentStatus.Delivered
                     && s.SellerConfirmedAt == null
                     && s.DeliveredAt != null
                     && s.DeliveredAt < deliveredCutoff)
            .OrderBy(s => s.DeliveredAt)
            .Take(200)
            .ToListAsync(ct);

        var confirmedCount       = 0;
        var skippedMissingCount  = 0;
        var failedCount          = 0;

        foreach (var shipment in eligible)
        {
            try
            {
                // D2 evidence guard: skip when the seller never uploaded receipt
                // evidence. ConfirmBySeller() would reject anyway; fail fast with
                // a clear admin ticket so the stuck shipment surfaces in the
                // operator queue.
                if (!shipment.HasReceiptEvidence)
                {
                    skippedMissingCount++;
                    _logger.LogWarning(
                        "WarehouseReturnAutoConfirmJob: SKIPPED shipment {ShipmentId} — " +
                        "seller did not upload receipt evidence. Creating admin ticket.",
                        shipment.Id.Value);

                    // Reuse the OrderContext admin-ticket command. WarehouseReturn
                    // has no deferred-refund concept, so we pass Intent="none".
                    // OrderId field is the shipment id (ticket schema is shared;
                    // admin tooling discriminates by Intent + log line).
                    await sender.Send(new CreateAdminRefundRetryTicketCommand(
                        OrderId:       shipment.Id.Value,
                        OrderReturnId: shipment.Id.Value,
                        Intent:        "warehouse_return_no_refund",
                        Amount:        null,
                        FailureReason: "auto-confirm blocked — seller did not upload receipt evidence"), ct);
                    continue;
                }

                var confirmResult = shipment.ConfirmBySeller(now);
                if (confirmResult.IsFailure)
                {
                    failedCount++;
                    _logger.LogWarning(
                        "WarehouseReturnAutoConfirmJob: ConfirmBySeller failed for shipment {ShipmentId}: {Error}",
                        shipment.Id.Value, confirmResult.Error.Message);
                    continue;
                }

                dbContext.Update(shipment);
                await unitOfWork.SaveChangesAsync(ct);
                confirmedCount++;

                _logger.LogInformation(
                    "WarehouseToSellerShipment {ShipmentId} auto-confirmed after 7-day seller-confirmation window.",
                    shipment.Id.Value);
            }
            catch (Exception ex)
            {
                failedCount++;
                _logger.LogError(ex,
                    "WarehouseReturnAutoConfirmJob: error confirming shipment {ShipmentId}",
                    shipment.Id.Value);
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
                "WarehouseReturnAutoConfirmJob completed in {DurationMs}ms. Eligible={Eligible}, Confirmed={Confirmed}, SkippedMissingEvidence={Skipped}, Failed={Failed}",
                stopwatch.ElapsedMilliseconds, eligible.Count, confirmedCount, skippedMissingCount, failedCount);
        }
        else if (logging.LogNoopRuns)
        {
            _logger.LogDebug(
                "WarehouseReturnAutoConfirmJob found no eligible shipments in {DurationMs}ms.",
                stopwatch.ElapsedMilliseconds);
        }
    }
}
