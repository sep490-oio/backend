using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Commands.ConfirmPhoneNumber;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Me;

public class ConfirmPhoneNumberEndpoint : IEndpoint
{
    public sealed record Request(
        [Required] string VerificationCode);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Me.ConfirmPhoneNumber, async (
                Request request, 
                ISender sender, 
                CancellationToken ct) =>
            {
                var command = new ConfirmPhoneNumberCommand(request.VerificationCode);

                var result = await sender.Send(command, ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Me.ManagePhone)
            .WithName(ApiEndpoint.Names.Me.ConfirmPhoneNumber)
            .WithTags(ApiEndpoint.Tags.Me);
    }
}