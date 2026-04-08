using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.WarehouseContext.Commands.SelfShipOrder;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext.Shipments;

public sealed class SelfShipOrderEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Warehouse.SelfShipOutbound, async (
                SelfShipOrderRequest req,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new SelfShipOrderCommand(
                    OrderId:               req.OrderId,
                    ExternalCarrierName:   req.ExternalCarrierName,
                    CarrierTrackingNumber: req.CarrierTrackingNumber,
                    WeightGrams:           req.WeightGrams,
                    InsuranceValue:        req.InsuranceValue,
                    ShippingMethod:        req.ShippingMethod
                );

                var result = await sender.Send(command, ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.SelfShipOutbound)
            .WithName(ApiEndpoint.Names.Warehouse.SelfShipOrder)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}

public sealed record SelfShipOrderRequest(
    Guid    OrderId,
    string  ExternalCarrierName,
    string  CarrierTrackingNumber,
    int     WeightGrams,
    decimal InsuranceValue = 0,
    string? ShippingMethod = null);
