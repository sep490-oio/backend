using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.WarehouseContext.Commands.UpdateShippingProviderConfig;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext;

public sealed class UpdateShippingProviderConfigEndpoint : IEndpoint
{
    public sealed record Request(
        string  DisplayName,
        string  ApiBaseUrl,
        string  PickName,
        string  PickPhone,
        string  PickAddress,
        string  PickWard,
        string  PickDistrict,
        string  PickProvince,
        string? PickCarrierAddressDataJson,
        string? WebhookSecret,
        string? CredentialsJson);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiEndpoint.Url.Warehouse.ShippingProviderConfigById, async (
                Guid configId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new UpdateShippingProviderConfigCommand(
                    configId,
                    request.DisplayName,
                    request.ApiBaseUrl,
                    request.PickName,
                    request.PickPhone,
                    request.PickAddress,
                    request.PickWard,
                    request.PickDistrict,
                    request.PickProvince,
                    request.PickCarrierAddressDataJson,
                    request.WebhookSecret,
                    request.CredentialsJson), ct);
                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.ManageLocations)
            .WithName(ApiEndpoint.Names.Warehouse.UpdateShippingProviderConfig)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}