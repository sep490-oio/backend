using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Commands.UpdateAddress;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Me;

public class UpdateAddressEndpoint : IEndpoint
{
    public sealed record Request(
        string? Type,
        string? RecipientName,
        string? Street,
        string? Ward,
        string? District,
        string? City,
        string? PhoneNumber,
        string? CountryCode,
        string? PostalCode);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiEndpoint.Url.Me.UpdateAddress, async (
                Guid addressId,
                Request request,
                ISender sender,
                CancellationToken cancellationToken = default) =>
                {
                    var command = new UpdateAddressCommand(
                        addressId,
                        request.Type,
                        request.RecipientName,
                        request.Street,
                        request.Ward,
                        request.District,
                        request.City,
                        request.PhoneNumber,
                        request.CountryCode,
                        request.PostalCode);

                    var result = await sender.Send(command, cancellationToken);

                    return result.ToOkHttpResult();
                })
            .RequireAuthorization(App.Permissions.Catalogs.Me.ManageAddress)
            .WithName(ApiEndpoint.Names.Me.UpdateMyAddress)
            .WithTags(ApiEndpoint.Tags.Me);
    }
}