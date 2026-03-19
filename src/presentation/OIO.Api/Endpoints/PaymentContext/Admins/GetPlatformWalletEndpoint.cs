using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.PaymentContext.DTOs;
using OIO.Application.Context.PaymentContext.Queries.Admins.GetPlatformWallet;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.PaymentContext.Admins;

public sealed class GetPlatformWalletEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.AdminPayments.GetPlatformWallet, async (
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new GetPlatformWalletQuery(), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ReadPayments)
            .WithName(ApiEndpoint.Names.AdminPayments.GetPlatformWallet)
            .WithTags(ApiEndpoint.Tags.Payments)
            .Produces<WalletSummaryDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}
