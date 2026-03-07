using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Commands.ActivateItem;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Items;

public sealed class ActivateItemEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Items.Activate, async (
                Guid itemId,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new ActivateItemCommand(itemId);

                var result = await sender.Send(command, ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Items.ActivateItem)
            .WithTags(ApiEndpoint.Tags.Items)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}