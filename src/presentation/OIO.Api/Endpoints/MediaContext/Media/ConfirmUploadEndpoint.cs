using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.MediaContext.Commands.ConfirmUpload;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.MediaContext.Media;

public sealed class ConfirmUploadEndpoint : IEndpoint
{
    public sealed record Request(
        Guid MediaUploadId,
        string PublicId,
        string SecureUrl,
        long Bytes,
        string Format,
        int? Width,
        int? Height,
        double? DurationSeconds);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Media.Confirm, async (
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new ConfirmUploadCommand(
                    request.MediaUploadId,
                    request.PublicId,
                    request.SecureUrl,
                    request.Bytes,
                    request.Format,
                    request.Width,
                    request.Height,
                    request.DurationSeconds);

                var result = await sender.Send(command, ct);

                return result.ToCreatedHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Media.ConfirmUpload)
            .WithName(ApiEndpoint.Names.Media.ConfirmUpload)
            .WithTags(ApiEndpoint.Tags.Media)
            .Produces(StatusCodes.Status201Created)
            .ProducesValidationProblem();
    }
}