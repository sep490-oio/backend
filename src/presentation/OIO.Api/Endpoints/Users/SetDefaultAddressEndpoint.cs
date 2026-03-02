using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.UserContext.Commands.SetDefaultAddress;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.Users;

public class SetDefaultAddressEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch(ApiEndpoint.Url.Users.SetDefaultAddress, async (
                Guid addressId,
                ISender sender,
                CancellationToken cancellationToken = default) =>
                {
                    var command = new SetDefaultAddressCommand(addressId);
                    
                    var result = await sender.Send(command, cancellationToken);

                    return result.ToNoContentHttpResult();
                })
            .RequireAuthorization(App.Permissions.Catalogs.Users.SetDefaultAddresses)
            .WithName(ApiEndpoint.Names.Users.SetDefaultAddress)
            .WithTags(ApiEndpoint.Tags.Users);
    }
}