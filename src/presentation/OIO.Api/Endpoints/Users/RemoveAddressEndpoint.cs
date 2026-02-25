using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.UserContext.Commands.RemoveAddress;

namespace OIO.Api.Endpoints.Users;

public class RemoveAddressEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("api/users/me/addresses/{addressId:guid}",
                async (Guid addressId, ISender sender, CancellationToken ct) =>
                {
                    var command = new RemoveAddressCommand(addressId);
                    
                    var result = await sender.Send(command, ct);

                    return result.IsFailure ? result.Error.ProcessError() : Results.NoContent();
                })
            .RequireAuthorization()
            .WithTags(Tags.Users);
    }
}