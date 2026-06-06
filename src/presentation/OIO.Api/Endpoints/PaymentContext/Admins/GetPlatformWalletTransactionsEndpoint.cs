using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.PaymentContext.Queries.Admins.GetPlatformWalletTransactions;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.PaymentContext.Admins;

public sealed class GetPlatformWalletTransactionsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.AdminPayments.GetPlatformWalletTransactions, async (
                ISender sender,
                int? pageNumber,
                int? pageSize,
                string? type,
                Guid? auctionId,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new GetPlatformWalletTransactionsQuery(
                        PageNumber: pageNumber ?? 1,
                        PageSize: pageSize ?? 20,
                        Type: type,
                        AuctionId: auctionId), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ReadPayments)
            .WithName(ApiEndpoint.Names.AdminPayments.GetPlatformWalletTransactions)
            .WithTags(ApiEndpoint.Tags.Payments)
            .Produces<PlatformWalletTransactionsResultDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}
