using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.PaymentContext.Commands.Escrows.RefundEscrow;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.PaymentContext.Admins;

public sealed class ForceRefundEscrowEndpoint : IEndpoint
{
    public record Request(string Reason);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.AdminPayments.ForceRefundEscrow, async (
                Guid escrowId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new RefundEscrowCommand(escrowId, request.Reason),
                    ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManagePayments)
            .WithName(ApiEndpoint.Names.AdminPayments.ForceRefundEscrow)
            .WithTags(ApiEndpoint.Tags.Payments)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
