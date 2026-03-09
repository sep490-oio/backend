using MediatR;
using Microsoft.Extensions.Logging;
using OIO.Application.Context.AuctionContext.Commands.EndAuction;
using Quartz;

namespace OIO.Infrastructure.Scheduling.Jobs.Auctions;

[DisallowConcurrentExecution]
public sealed class EndAuctionJob : IJob
{
    public static JobKey BuildJobKey(Guid auctionId) =>
        new($"end-{auctionId}", JobConstants.AuctionLifecycleGroup);

    public static TriggerKey BuildTriggerKey(Guid auctionId) =>
        new($"end-trigger-{auctionId}", JobConstants.AuctionLifecycleGroup);

    private readonly ISender _sender;
    private readonly ILogger<EndAuctionJob> _logger;

    public EndAuctionJob(
        ISender sender,
        ILogger<EndAuctionJob> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var auctionId = context.MergedJobDataMap.GetGuidValue("AuctionId");

        _logger.LogInformation(
            "⏰ EndAuctionJob fired for {AuctionId}.", auctionId);

        var result = await _sender.Send(new EndAuctionCommand(auctionId));

        if (result.IsFailure)
        {
            _logger.LogError(
                "❌ EndAuctionJob failed for {AuctionId}: {Error}",
                auctionId, result.Error.Message);
        }
    }
}