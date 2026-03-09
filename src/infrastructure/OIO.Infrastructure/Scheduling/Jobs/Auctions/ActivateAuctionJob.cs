using MediatR;
using Microsoft.Extensions.Logging;
using OIO.Application.Context.AuctionContext.Commands.ActivateAuction;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using Quartz;

namespace OIO.Infrastructure.Scheduling.Jobs.Auctions;

[DisallowConcurrentExecution]
public sealed class ActivateAuctionJob : IJob
{
    public static JobKey BuildJobKey(Guid auctionId) =>
        new($"activate-{auctionId}", JobConstants.AuctionLifecycleGroup);

    public static TriggerKey BuildTriggerKey(Guid auctionId) =>
        new($"activate-trigger-{auctionId}", JobConstants.AuctionLifecycleGroup);

    private readonly ISender _sender;
    private readonly ILogger<ActivateAuctionJob> _logger;

    public ActivateAuctionJob(
        ISender sender,
        ILogger<ActivateAuctionJob> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var auctionId = context.MergedJobDataMap.GetGuidValue(nameof(AuctionId));

        _logger.LogInformation(
            "⏰ ActivateAuctionJob fired for {AuctionId}.", auctionId);

        var result = await _sender.Send(new ActivateAuctionCommand(auctionId));

        if (result.IsFailure)
        {
            _logger.LogError(
                "❌ ActivateAuctionJob failed for {AuctionId}: {Error}",
                auctionId, result.Error.Message);
        }
    }
}