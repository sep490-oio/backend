using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.PaymentContext.DTOs;
using OIO.Application.Context.PaymentContext.Queries.Admins.GetAdminTransactionById;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.PaymentContext.Admins;

public sealed class GetTransactionByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.AdminPayments.GetTransactionById, async (
                Guid transactionId,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new GetAdminTransactionByIdQuery(transactionId),
                    ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ReadPayments)
            .WithName(ApiEndpoint.Names.AdminPayments.GetTransactionById)
            .WithTags(ApiEndpoint.Tags.Payments)
            .Produces<PaymentTransactionDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
