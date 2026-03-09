using MediatR;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Media;
using OIO.Domain.Context.AuctionContext.Aggregates.Items.Events;

namespace OIO.Application.Context.AuctionContext.EventHandlers;

internal sealed class MediaRemovedFromItemEventHandler
    : INotificationHandler<MediaRemovedFromItemEvent>
{
    private readonly IMediaSignatureService _mediaService;
    private readonly ILogger<MediaRemovedFromItemEventHandler> _logger;

    public MediaRemovedFromItemEventHandler(
        IMediaSignatureService mediaService,
        ILogger<MediaRemovedFromItemEventHandler> logger)
    {
        _mediaService = mediaService;
        _logger = logger;
    }

    public async Task Handle(
        MediaRemovedFromItemEvent notification, CancellationToken ct)
    {
        var resourceType = UploadContextRegistry.ParseResourceType(
            notification.ResourceType);

        var deleted = await _mediaService.DeleteResourceAsync(
            notification.PublicId, resourceType, ct);

        if (deleted)
        {
            _logger.LogInformation(
                "Deleted {Type} resource from storage: {PublicId} (Item={ItemId})",
                notification.ResourceType, notification.PublicId, notification.ItemId);
        }
        else
        {
            _logger.LogWarning(
                "Failed to delete {Type} resource from storage: {PublicId} (Item={ItemId}). " +
                "May need manual cleanup.",
                notification.ResourceType, notification.PublicId, notification.ItemId);
        }
    }
}