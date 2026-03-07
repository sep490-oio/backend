using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Commands.AddMediaToItem;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Items;

public sealed class AddMediaToItemEndpoint : IEndpoint
{
    public sealed record Request(
        Guid MediaUploadId,
        bool IsPrimary = false,
        int? SortOrder = null);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Items.AddMedia, async (
                Guid itemId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new AddMediaToItemCommand(
                    itemId,
                    request.MediaUploadId,
                    request.IsPrimary,
                    request.SortOrder);

                var result = await sender.Send(command, ct);

                return result.ToCreatedHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Items.ManageMedia)
            .WithName(ApiEndpoint.Names.Items.AddItemMedia)
            .WithTags(ApiEndpoint.Tags.Items)
            .Produces(StatusCodes.Status201Created)
            .ProducesValidationProblem();
    }
}