using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Commands.BatchAddMediaToItem;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Items;

public sealed class BatchAddMediaToItemEndpoint : IEndpoint
{
    public sealed record RequestItem(
        [Required] Guid MediaUploadId,
        [Required] bool IsPrimary = false,
        int? SortOrder = null);

    public sealed record Request(
        [Required] List<RequestItem> Items);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Items.BatchAddMedia, async (
                Guid itemId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new BatchAddMediaToItemCommand(
                    itemId,
                    request.Items.Select(i => new BatchMediaItem(
                        i.MediaUploadId,
                        i.IsPrimary,
                        i.SortOrder)).ToList());

                var result = await sender.Send(command, ct);

                return result.ToCreatedHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Items.ManageMedia)
            .WithName(ApiEndpoint.Names.Items.BatchAddItemMedia)
            .WithTags(ApiEndpoint.Tags.Items)
            .Produces<List<ItemMediaDto>>(StatusCodes.Status201Created)
            .ProducesValidationProblem();
    }
}
