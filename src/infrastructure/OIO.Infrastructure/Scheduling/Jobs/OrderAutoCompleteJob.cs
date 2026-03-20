using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Enums;
using Quartz;

namespace OIO.Infrastructure.Scheduling.Jobs;

[DisallowConcurrentExecution]
internal sealed class OrderAutoCompleteJob : IJob
{
    private readonly IDbContext _dbContext;
    // private readonly ISender _sender;
    private readonly IClock _clock;
    private readonly ILogger<OrderAutoCompleteJob> _logger;

    public OrderAutoCompleteJob(IDbContext dbContext, IClock clock, ILogger<OrderAutoCompleteJob> logger)
    {
        _dbContext = dbContext;
        _clock = clock;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Starting OrderAutoCompleteJob...");

        var now = _clock.UtcNow;
        var thresholdDate = now.AddDays(-3);

        // Find all orders that are Delivered and have been past the 3-day window
        var eligibleOrders = await _dbContext.Set<Order>()
            .Where(o => o.Status == OrderStatus.Delivered && o.DeliveredAt <= thresholdDate)
            .Select(o => o.Id)
            .ToListAsync(context.CancellationToken);

        _logger.LogInformation("Found {Count} orders eligible for auto-completion.", eligibleOrders.Count);

        foreach (var orderId in eligibleOrders)
        {
            // TODO: Because we are only working on the Warehouse scope, the actual transition
            // logic and payment integration to sellers will need to be implemented within OrderContext.
            
            // Expected Implementation:
            // var command = new CompleteOrderCommand(orderId);
            // await _sender.Send(command, context.CancellationToken);
            
            _logger.LogInformation("TODO: Trigger auto-completion for Order {OrderId}", orderId);
        }

        _logger.LogInformation("OrderAutoCompleteJob finished.");
    }
}
