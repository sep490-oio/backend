using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.UserContext.Commands.RemoveAddress;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.Users;

public class RemoveAddressEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete(ApiEndpoint.Url.Users.RemoveAddress, async (
                    Guid addressId,
                    ISender sender,
                    CancellationToken ct) =>
                {
                    var command = new RemoveAddressCommand(addressId);
                    
                    var result = await sender.Send(command, ct);

                    return result.ToNoContentHttpResult();
                })
            .RequireAuthorization(App.Permissions.Catalogs.Users.RemoveAddresses)
            .WithName(ApiEndpoint.Names.Users.RemoveAddress)
            .WithTags(ApiEndpoint.Tags.Users);
    }
}