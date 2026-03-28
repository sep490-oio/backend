using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.WarehouseContext.Queries.CalculateExpectedDeliveryTime;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext;

public sealed class CalculateExpectedDeliveryTimeEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Warehouse.CalculateLeadTime, async (
                CalculateExpectedDeliveryTimeQuery query,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(query, ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.CalculateLeadTime)
            .WithName(ApiEndpoint.Names.Warehouse.CalculateLeadTime)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces<DateTime?>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
