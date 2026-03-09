namespace OIO.Application.Abstractions.Scheduling;

public interface IAuctionScheduler
{
    Task ScheduleStartAsync(Guid auctionId, DateTime startTime, CancellationToken ct = default);
    Task ScheduleEndAsync(Guid auctionId, DateTime endTime, CancellationToken ct = default);
    Task RescheduleEndAsync(Guid auctionId, DateTime newEndTime, CancellationToken ct = default);
    Task CancelAsync(Guid auctionId, CancellationToken ct = default);
}