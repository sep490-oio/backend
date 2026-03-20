using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Commands.ChooseAuctionShipping;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Auctions;

public sealed class ChooseAuctionShippingEndpoint : IEndpoint
{
    public sealed record Request(
        [Required] string SenderName,
        [Required] string SenderPhone,
        [Required] string SenderAddress,
        [Required] string SenderWard,
        [Required] string SenderDistrict,
        [Required] string SenderProvince,
        [Required] int WeightGrams,
        [Required] decimal InsuranceValue,
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
        app.MapPost(ApiEndpoint.Url.Auctions.Shipping, async (
                Guid auctionId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new ChooseAuctionShippingCommand(
                    AuctionId: auctionId,
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
            .RequireAuthorization(App.Permissions.Catalogs.Auctions.Submit)
            .WithName(ApiEndpoint.Names.Auctions.ChooseAuctionShipping)
            .WithTags(ApiEndpoint.Tags.Auctions)
            .Produces(StatusCodes.Status201Created)
            .ProducesValidationProblem();
    }
}
