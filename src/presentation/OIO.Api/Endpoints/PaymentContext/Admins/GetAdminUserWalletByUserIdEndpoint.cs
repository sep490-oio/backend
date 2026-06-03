using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.PaymentContext.DTOs;
using OIO.Application.Context.PaymentContext.Queries.Admins.GetAdminUserWalletByUserId;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.PaymentContext.Admins;

public class GetAdminUserWalletByUserIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.AdminPayments.GetAdminUserWalletByUserId, async (
                Guid userId,
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetAdminUserWalletByUserIdQuery(userId);
                var result = await sender.Send(query, ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ReadPayments)
            .WithName(ApiEndpoint.Names.AdminPayments.GetAdminUserWalletByUserId)
            .WithTags(ApiEndpoint.Tags.AdminPayments)
            .Produces<WalletSummaryDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound);
    }
}
