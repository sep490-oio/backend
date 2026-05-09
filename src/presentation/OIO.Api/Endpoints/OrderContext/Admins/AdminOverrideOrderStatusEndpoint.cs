using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.OrderContext.Commands.Admin.AdminOverrideOrderStatus;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.OrderContext.Admins;

public sealed class AdminOverrideOrderStatusEndpoint : IEndpoint
{
    public sealed record Request(string NewStatus, string Reason);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Admins.AdminOverrideOrderStatus, async (
                Guid orderId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new AdminOverrideOrderStatusCommand(orderId, request.NewStatus, request.Reason),
                    ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManagePayments)
            .WithName(ApiEndpoint.Names.Admins.AdminOverrideOrderStatus)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
