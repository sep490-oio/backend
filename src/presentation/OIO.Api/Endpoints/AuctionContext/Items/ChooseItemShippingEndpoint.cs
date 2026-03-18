using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Commands.ChooseItemShipping;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Items;

public sealed class ChooseItemShippingEndpoint : IEndpoint
{
    public sealed record Request(
        string SenderName,
        string SenderPhone,
        string SenderAddress,
        string SenderWard,
        string SenderDistrict,
        string SenderProvince,
        int WeightGrams,
        decimal InsuranceValue,
        string? ProviderCode = null,
        string? SenderCarrierAddressDataJson = null,
        int? LengthCm = null,
        int? WidthCm = null,
        int? HeightCm = null,
        string? ExternalTrackingNumber = null,
        string? ExternalCarrierName = null,
        string? Notes = null);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Items.Shipping, async (
                Guid itemId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new ChooseItemShippingCommand(
                    ItemId: itemId,
                    SenderName: request.SenderName,
                    SenderPhone: request.SenderPhone,
                    SenderAddress: request.SenderAddress,
                    SenderWard: request.SenderWard,
                    SenderDistrict: request.SenderDistrict,
                    SenderProvince: request.SenderProvince,
                    WeightGrams: request.WeightGrams,
                    InsuranceValue: request.InsuranceValue,
                    ProviderCode: request.ProviderCode,
                    SenderCarrierAddressDataJson: request.SenderCarrierAddressDataJson,
                    LengthCm: request.LengthCm,
                    WidthCm: request.WidthCm,
                    HeightCm: request.HeightCm,
                    ExternalTrackingNumber: request.ExternalTrackingNumber,
                    ExternalCarrierName: request.ExternalCarrierName,
                    Notes: request.Notes);

                var result = await sender.Send(command, ct);
                return result.ToCreatedHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Items.Create)
            .WithName(ApiEndpoint.Names.Items.ChooseItemShipping)
            .WithTags(ApiEndpoint.Tags.Items)
            .Produces(StatusCodes.Status201Created)
            .ProducesValidationProblem();
    }
}
