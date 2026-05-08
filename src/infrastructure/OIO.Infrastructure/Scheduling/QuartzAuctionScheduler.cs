using System.Collections.Concurrent;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Scheduling;
using OIO.Application.Context.AuctionContext.Commands.ActivateAuction;
using OIO.Application.Context.AuctionContext.Commands.EndAuction;
using OIO.Infrastructure.Scheduling.Jobs;
using OIO.Infrastructure.Scheduling.Jobs.Auctions;
using Quartz;

namespace OIO.Infrastructure.Scheduling;

internal sealed class QuartzAuctionScheduler : IAuctionScheduler
{
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly IClock _clock;
    private readonly ILogger<QuartzAuctionScheduler> _logger;

    public QuartzAuctionScheduler(
        ISchedulerFactory schedulerFactory,
        IClock clock,
        ILogger<QuartzAuctionScheduler> logger)
    {
        _schedulerFactory = schedulerFactory;
        _clock = clock;
        _logger = logger;
    }

    public async Task ScheduleStartAsync(
        Guid auctionId, 
        DateTime startTime,
        CancellationToken ct = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(ct);

        var job = JobBuilder.Create<ActivateAuctionJob>()
            .WithIdentity(ActivateAuctionJob.BuildJobKey(auctionId))
            .UsingJobData("AuctionId", auctionId.ToString())
            .StoreDurably(false)
            .Build();

        var fireAt = startTime <= _clock.UtcNow
            ? _clock.UtcNow.AddSeconds(1)
            : startTime;

        var trigger = TriggerBuilder.Create()
            .WithIdentity(ActivateAuctionJob.BuildTriggerKey(auctionId))
            .StartAt(fireAt)
            .WithSimpleSchedule(x => x.WithMisfireHandlingInstructionFireNow())
            .Build();

        // Replace if already exists (idempotent)
        if (await scheduler.CheckExists(job.Key, ct))
        {
            await scheduler.DeleteJob(job.Key, ct);
        }

        await scheduler.ScheduleJob(job, trigger, ct);

        _logger.LogInformation(
            "⏰ Scheduled START for auction {AuctionId} at {FireAt}.",
            auctionId, fireAt);
    }

    public async Task ScheduleEndAsync(
        Guid auctionId, DateTime endTime, CancellationToken ct = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(ct);

        var job = JobBuilder.Create<EndAuctionJob>()
            .WithIdentity(EndAuctionJob.BuildJobKey(auctionId))
            .UsingJobData("AuctionId", auctionId.ToString())
            .StoreDurably(false)
            .Build();

        var fireAt = endTime <= _clock.UtcNow
            ? _clock.UtcNow.AddSeconds(1)
            : endTime;

        var trigger = TriggerBuilder.Create()
            .WithIdentity(EndAuctionJob.BuildTriggerKey(auctionId))
            .StartAt(fireAt)
            .WithSimpleSchedule(x => x.WithMisfireHandlingInstructionFireNow())
            .Build();

        if (await scheduler.CheckExists(job.Key, ct))
        {
            await scheduler.DeleteJob(job.Key, ct);
        }

        await scheduler.ScheduleJob(job, trigger, ct);

        _logger.LogInformation(
            "⏰ Scheduled END for auction {AuctionId} at {FireAt}.",
            auctionId, fireAt);
    }

    public async Task ScheduleQualificationCloseAsync(
        Guid auctionId, DateTime qualificationEndTime, CancellationToken ct = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(ct);

        var job = JobBuilder.Create<CloseQualificationJob>()
            .WithIdentity(CloseQualificationJob.BuildJobKey(auctionId))
            .UsingJobData("AuctionId", auctionId.ToString())
            .StoreDurably(false)
            .Build();

        var fireAt = qualificationEndTime <= _clock.UtcNow
            ? _clock.UtcNow.AddSeconds(1)
            : qualificationEndTime;

        var trigger = TriggerBuilder.Create()
            .WithIdentity(CloseQualificationJob.BuildTriggerKey(auctionId))
            .StartAt(fireAt)
            .WithSimpleSchedule(x => x.WithMisfireHandlingInstructionFireNow())
            .Build();

        if (await scheduler.CheckExists(job.Key, ct))
        {
            await scheduler.DeleteJob(job.Key, ct);
        }

        await scheduler.ScheduleJob(job, trigger, ct);

        _logger.LogInformation(
            "⏰ Scheduled QUALIFICATION CLOSE for auction {AuctionId} at {FireAt}.",
            auctionId, fireAt);
    }

    public async Task RescheduleEndAsync(
        Guid auctionId, DateTime newEndTime, CancellationToken ct = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(ct);
        var triggerKey = EndAuctionJob.BuildTriggerKey(auctionId);

        if (await scheduler.CheckExists(triggerKey, ct))
        {
            var newTrigger = TriggerBuilder.Create()
                .WithIdentity(triggerKey)
                .StartAt(newEndTime)
                .WithSimpleSchedule(x => x.WithMisfireHandlingInstructionFireNow())
                .Build();

            await scheduler.RescheduleJob(triggerKey, newTrigger, ct);

            _logger.LogInformation(
                "⏰ Rescheduled END for auction {AuctionId} to {NewEndTime}.",
                auctionId, newEndTime);
        }
        else
        {
            // Trigger doesn't exist, schedule fresh
            await ScheduleEndAsync(auctionId, newEndTime, ct);
        }
    }

    public async Task CancelAsync(Guid auctionId, CancellationToken ct = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(ct);

        var qualCloseDeleted = await scheduler.DeleteJob(
            CloseQualificationJob.BuildJobKey(auctionId), ct);
        var startDeleted = await scheduler.DeleteJob(
            ActivateAuctionJob.BuildJobKey(auctionId), ct);
        var endDeleted = await scheduler.DeleteJob(
            EndAuctionJob.BuildJobKey(auctionId), ct);

        _logger.LogInformation(
            "❌ Cancelled timers for auction {AuctionId}. QualClose={QualClose}, Start={Start}, End={End}.",
            auctionId, qualCloseDeleted, startDeleted, endDeleted);
    }
}