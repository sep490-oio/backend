using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Commands.ConfirmInspectedCondition;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Items;

public sealed class ConfirmInspectedConditionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Items.ConfirmInspectedCondition, async (
                Guid itemId,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new ConfirmInspectedConditionCommand(itemId), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Items.Resubmit)
            .WithName(ApiEndpoint.Names.Items.ConfirmInspectedCondition)
            .WithTags(ApiEndpoint.Tags.Items)
            .Produces<ItemDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
