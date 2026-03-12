using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Commands.VerifySellerProfile;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Admins;

public class VerifySellerProfileEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Admins.VerifySellerProfile, async (
                Guid id,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new VerifySellerProfileCommand(id);

                var result = await sender.Send(command, ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManageSellerProfiles)
            .WithName(ApiEndpoint.Names.Admins.VerifySellerProfile)
            .WithTags(ApiEndpoint.Tags.Admins);
    }
}
