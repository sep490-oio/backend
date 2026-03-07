using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Commands.ChangePassword;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Me;

public class ChangePasswordEndpoint : IEndpoint
{
    public sealed record Request(
        [Required] string CurrentPassword,
        [Required] string NewPassword);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiEndpoint.Url.Me.ChangePassword, async (
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new ChangePasswordCommand(
                    request.CurrentPassword,
                    request.NewPassword);

                var result = await sender.Send(command, ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Me.ChangePassword)
            .WithName(ApiEndpoint.Names.Me.ChangePassword)
            .WithTags(ApiEndpoint.Tags.Me);
    }
}