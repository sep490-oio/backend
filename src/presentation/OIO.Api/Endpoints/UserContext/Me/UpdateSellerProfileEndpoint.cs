using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Commands.UpdateSellerProfile;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Me;

public class UpdateSellerProfileEndpoint : IEndpoint
{
    public sealed record Request(
        [Required] string StoreName,
        [Required] string StoreDescription);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiEndpoint.Url.Me.UpdateSellerProfile, async (
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new UpdateSellerProfileCommand(
                    request.StoreName, request.StoreDescription);

                var result = await sender.Send(command, ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Me.ManageSellerProfile)
            .WithName(ApiEndpoint.Names.Me.UpdateSellerProfile)
            .WithTags(ApiEndpoint.Tags.SellerProfiles);
    }
}
