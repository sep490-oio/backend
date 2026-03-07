using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Commands.SetDefaultAddress;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Me;

public class SetDefaultAddressEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch(ApiEndpoint.Url.Me.SetDefaultAddress, async (
                Guid addressId,
                ISender sender,
                CancellationToken cancellationToken = default) =>
                {
                    var command = new SetDefaultAddressCommand(addressId);
                    
                    var result = await sender.Send(command, cancellationToken);

                    return result.ToNoContentHttpResult();
                })
            .RequireAuthorization(App.Permissions.Catalogs.Me.ManageAddress)
            .WithName(ApiEndpoint.Names.Me.SetDefaultAddress)
            .WithTags(ApiEndpoint.Tags.Me);
    }
}