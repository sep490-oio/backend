using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.UserContext.Commands.ConfirmPhoneNumber;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.Users;

public class ConfirmPhoneNumberEndpoint : IEndpoint
{
    public sealed record Request(
        [Required] string VerificationCode);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Users.ConfirmPhoneNumber, async (
                Request request, 
                ISender sender, 
                CancellationToken ct) =>
            {
                var command = new ConfirmPhoneNumberCommand(request.VerificationCode);

                var result = await sender.Send(command, ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Users.ConfirmPhone)
            .WithName(ApiEndpoint.Names.Users.ConfirmPhoneNumber)
            .WithTags(ApiEndpoint.Tags.Users);
    }
}