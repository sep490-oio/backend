using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Commands.CreateSellerProfile;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Me;

public class CreateSellerProfileEndpoint : IEndpoint
{
    public sealed record Request(
        [Required] string StoreName,
        [Required] string StoreDescription);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Me.CreateSellerProfile, async (
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new CreateSellerProfileCommand(
                    request.StoreName, request.StoreDescription);

                var result = await sender.Send(command, ct);

                return result.ToCreatedHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Me.ManageSellerProfile)
            .WithName(ApiEndpoint.Names.Me.CreateSellerProfile)
            .WithTags(ApiEndpoint.Tags.SellerProfiles);
    }
}
