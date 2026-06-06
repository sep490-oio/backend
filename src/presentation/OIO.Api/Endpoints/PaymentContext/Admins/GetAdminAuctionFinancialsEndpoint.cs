using CSharpFunctionalExtensions.HttpResults;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.PaymentContext.Queries.Admins.GetAdminAuctionFinancials;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.PaymentContext.Admins;

internal sealed class GetAdminAuctionFinancialsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Admins.GetAdminAuctionFinancials,
                async ([AsParameters] Parameters parameters, ISender sender, CancellationToken ct) =>
                {
                    var query = new GetAdminAuctionFinancialsQuery(parameters.AuctionId, parameters.Type);
                    var result = await sender.Send(query, ct);
                    return result.ToOkHttpResult();
                })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ReadPayments)
            .WithName(ApiEndpoint.Names.Admins.GetAdminAuctionFinancials)
            .WithTags(ApiEndpoint.Tags.AdminPayments)
            .WithSummary("Get combined auction financials")
            .Produces<IReadOnlyList<AdminAuctionFinancialDto>>(StatusCodes.Status200OK)
            .ProducesValidationProblem();
    }

    public sealed record Parameters
    {
        public Guid AuctionId { get; init; }
        public string? Type { get; init; }
    }
}
