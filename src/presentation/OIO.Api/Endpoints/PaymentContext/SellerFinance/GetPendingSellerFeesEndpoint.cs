using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using OIO.Api.Common;
using OIO.Application.Context.PaymentContext.DTOs;
using OIO.Application.Context.PaymentContext.Queries.GetPendingSellerFees;

using CSharpFunctionalExtensions.HttpResults;

namespace OIO.Api.Endpoints.PaymentContext.SellerFinance;

internal sealed class GetPendingSellerFeesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.SellerFinance.PendingFees,
                async (ISender sender, CancellationToken ct) =>
                    (await sender.Send(new GetPendingSellerFeesQuery(), ct)).ToOkHttpResult())
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.SellerFinance.GetPendingSellerFees)
            .WithTags(ApiEndpoint.Tags.SellerFinance)
            .WithSummary("Get pending fees for the current seller.")
            .Produces<IReadOnlyList<PendingSellerFeeDto>>(StatusCodes.Status200OK);
    }
}
