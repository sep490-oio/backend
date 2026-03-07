using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Commands.RemoveAddress;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Me;

public class RemoveMyAddressEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete(ApiEndpoint.Url.Me.RemoveMyAddress, async (
                    Guid addressId,
                    ISender sender,
                    CancellationToken ct) =>
                {
                    var command = new RemoveAddressCommand(addressId);
                    
                    var result = await sender.Send(command, ct);

                    return result.ToNoContentHttpResult();
                })
            .RequireAuthorization(App.Permissions.Catalogs.Me.ManageAddress)
            .WithName(ApiEndpoint.Names.Me.RemoveMyAddress)
            .WithTags(ApiEndpoint.Tags.Me);
    }
}