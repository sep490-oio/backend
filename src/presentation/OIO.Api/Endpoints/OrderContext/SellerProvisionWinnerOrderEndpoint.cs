using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using OIO.Api.Common;
using OIO.Application.Context.OrderContext.Commands.ProvisionWinnerOrder;
using OIO.Application.Context.UserContext.Services;

namespace OIO.Api.Endpoints.OrderContext;

internal sealed class SellerProvisionWinnerOrderEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Orders.SellerProvisionWinnerOrder,
                async (Guid auctionId, ICurrentUser currentUser, ISender sender, CancellationToken ct) =>
                {
                    var cmd = new ProvisionWinnerOrderCommand(auctionId, CurrentUserId: currentUser.UserId.Value, IsAdmin: false);
                    var result = await sender.Send(cmd, ct);
                    return result.ToOkHttpResult();
                })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Orders.SellerProvisionWinnerOrder)
            .WithTags(ApiEndpoint.Tags.Orders)
            .WithSummary("Manually provision an order for a completed auction (Seller).")
            .WithDescription("Creates a new order if it wasn't successfully created automatically after auction completion.")
            .Produces<Guid>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);
    }
}
