using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.WarehouseContext.Queries.CalculateShippingFee;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext;

public sealed class CalculateShippingFeeEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Warehouse.CalculateShippingFee, async (
                CalculateShippingFeeQuery query,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(query, ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.CalculateShippingFee)
            .WithName(ApiEndpoint.Names.Warehouse.CalculateShippingFee)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces<decimal>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
