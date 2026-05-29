using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using OIO.Api.Common;
using OIO.Application.Context.OrderContext.Commands.ProvisionWinnerOrder;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.OrderContext.Admins;

internal sealed class AdminProvisionWinnerOrderEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Admins.AdminProvisionWinnerOrder,
                async (Guid auctionId, ISender sender, CancellationToken ct) =>
                {
                    var cmd = new ProvisionWinnerOrderCommand(auctionId, IsAdmin: true);
                    var result = await sender.Send(cmd, ct);
                    return result.ToOkHttpResult();
                })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManagePayments)
            .WithName(ApiEndpoint.Names.Admins.AdminProvisionWinnerOrder)
            .WithTags(ApiEndpoint.Tags.Orders)
            .WithSummary("Manually provision an order for a completed auction (Admin).")
            .WithDescription("Creates a new order if it wasn't successfully created automatically after auction completion.")
            .Produces<Guid>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status409Conflict);
    }
}
