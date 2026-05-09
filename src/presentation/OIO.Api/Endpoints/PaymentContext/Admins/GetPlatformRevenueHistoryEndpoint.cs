using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.PaymentContext.Queries.Admins.GetPlatformRevenueHistory;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.PaymentContext.Admins;

public sealed class GetPlatformRevenueHistoryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.AdminPayments.GetRevenueHistory, async (
                ISender sender,
                DateOnly? from,
                DateOnly? to,
                string? granularity,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new GetPlatformRevenueHistoryQuery(from, to, granularity ?? "day"), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ReadPayments)
            .WithName(ApiEndpoint.Names.AdminPayments.GetRevenueHistory)
            .WithTags(ApiEndpoint.Tags.Payments)
            .Produces<PlatformRevenueHistoryDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}
