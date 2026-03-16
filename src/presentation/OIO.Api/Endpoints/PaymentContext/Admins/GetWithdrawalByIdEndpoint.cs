using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.PaymentContext.DTOs;
using OIO.Application.Context.PaymentContext.Queries.Admins.GetAdminWithdrawalById;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.PaymentContext.Admins;

public sealed class GetWithdrawalByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.AdminPayments.GetWithdrawalById, async (
                Guid withdrawalId,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new GetAdminWithdrawalByIdQuery(withdrawalId),
                    ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ReadPayments)
            .WithName(ApiEndpoint.Names.AdminPayments.GetWithdrawalById)
            .WithTags(ApiEndpoint.Tags.Payments)
            .Produces<AdminWithdrawalRequestDetailDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
