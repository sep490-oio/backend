using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Commands.RejectSellerProfile;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Admins;

public class RejectSellerProfileEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Admins.RejectSellerProfile, async (
                Guid id,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new RejectSellerProfileCommand(id);

                var result = await sender.Send(command, ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManageSellerProfiles)
            .WithName(ApiEndpoint.Names.Admins.RejectSellerProfile)
            .WithTags(ApiEndpoint.Tags.Admins);
    }
}
