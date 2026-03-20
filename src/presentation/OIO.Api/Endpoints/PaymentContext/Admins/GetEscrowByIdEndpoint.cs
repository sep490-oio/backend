using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.PaymentContext.DTOs;
using OIO.Application.Context.PaymentContext.Queries.Admins.GetAdminEscrowById;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.PaymentContext.Admins;

public sealed class GetEscrowByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.AdminPayments.GetEscrowById, async (
                Guid escrowId,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new GetAdminEscrowByIdQuery(escrowId),
                    ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ReadPayments)
            .WithName(ApiEndpoint.Names.AdminPayments.GetEscrowById)
            .WithTags(ApiEndpoint.Tags.Payments)
            .Produces<EscrowDetailDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
