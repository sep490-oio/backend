using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Commands.AddAddress;
using OIO.Domain.AppDefinitions;
using OIO.Domain.Context.UserContext.ValueObjects;

namespace OIO.Api.Endpoints.UserContext.Me;

public class AddAddressEndpoint : IEndpoint
{
    public sealed record Request(
        [Required] string Type,
        [Required] string RecipientName,
        [Required] string Street,
        [Required] string Ward,
        [Required] string District,
        [Required] string City,
        string? PostalCode,
        [Required] string PhoneNumber,
        [Required] string CountryCode = PhoneNumber.DefaultRegion,
        [Required] bool IsDefault = false);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Me.AddAddress, async (
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new AddAddressCommand(
                    Type: request.Type,
                    RecipientName: request.RecipientName,
                    Street: request.Street,
                    Ward: request.Ward,
                    District: request.District,
                    City: request.City,
                    PostalCode: request.PostalCode,
                    PhoneNumber: request.PhoneNumber,
                    CountryCode: request.CountryCode,
                    IsDefault: request.IsDefault);

                var result = await sender.Send(command, ct);

                return result.ToCreatedHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Me.ManageAddress)
            .WithName(ApiEndpoint.Names.Me.AddAddress)
            .WithTags(ApiEndpoint.Tags.Me);
    }
}