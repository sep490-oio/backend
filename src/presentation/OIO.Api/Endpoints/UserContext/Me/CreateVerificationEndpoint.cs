using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Commands.CreateVerification;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Me;

public class CreateVerificationEndpoint : IEndpoint
{
    public sealed record Request(
        [Required] string VerificationType);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Me.CreateVerification, async (
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new CreateVerificationCommand(
                    VerificationType: request.VerificationType);

                var result = await sender.Send(command, ct);

                return result.ToCreatedHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Me.ManageVerification)
            .WithName(ApiEndpoint.Names.Me.CreateVerification)
            .WithTags(ApiEndpoint.Tags.Verifications);
    }
}
