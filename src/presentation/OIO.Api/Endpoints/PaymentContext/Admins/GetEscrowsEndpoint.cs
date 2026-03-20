using MediatR;
using Microsoft.AspNetCore.Mvc;
using OIO.Api.Common;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.PaymentContext.DTOs;
using OIO.Application.Context.PaymentContext.Queries.Admins.GetAdminEscrows;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.PaymentContext.Admins;

public sealed class GetEscrowsEndpoint : IEndpoint
{
    public sealed record Parameters : AdminEscrowFilterParameters;

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.AdminPayments.GetEscrows, async (
                [AsParameters] Parameters parameters,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new GetAdminEscrowsQuery(parameters),
                    ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ReadPayments)
            .WithName(ApiEndpoint.Names.AdminPayments.GetEscrows)
            .WithTags(ApiEndpoint.Tags.Payments)
            .Produces<PagedList<EscrowDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}
