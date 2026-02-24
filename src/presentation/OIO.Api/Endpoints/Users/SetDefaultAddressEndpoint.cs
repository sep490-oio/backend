using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.UserContext.Commands.SetDefaultAddress;

namespace OIO.Api.Endpoints.Users;

public class SetDefaultAddressEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch("api/users/me/addresses/{addressId:guid}/default",
                async (Guid addressId, ISender sender, CancellationToken ct) =>
                {
                    var result = await sender.Send(new SetDefaultAddressCommand(addressId), ct);

                    return result.IsFailure ? result.Error.ProcessError() : Results.NoContent();
                })
            .RequireAuthorization()
            .WithTags(Tags.Users);
    }
}