using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.MediaContext.Commands.BatchConfirmUpload;
using OIO.Application.Context.MediaContext.Commands.ConfirmUpload;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.MediaContext.Media;

public sealed class BatchConfirmUploadEndpoint : IEndpoint
{
    public sealed record RequestItem(
        [Required] Guid MediaUploadId,
        [Required] string PublicId,
        [Required] string SecureUrl,
        [Required] long Bytes,
        [Required] string Format,
        string? FileName,
        int? Width,
        int? Height,
        double? DurationSeconds);

    public sealed record Request(
        [Required] List<RequestItem> Items);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Media.BatchConfirm, async (
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new BatchConfirmUploadCommand(
                    request.Items.Select(i => new BatchConfirmUploadItem(
                        i.MediaUploadId,
                        i.PublicId,
                        i.SecureUrl,
                        i.Bytes,
                        i.Format,
                        i.FileName,
                        i.Width,
                        i.Height,
                        i.DurationSeconds)).ToList());

                var result = await sender.Send(command, ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Media.ConfirmUpload)
            .WithName(ApiEndpoint.Names.Media.BatchConfirmUpload)
            .WithTags(ApiEndpoint.Tags.Media)
            .Produces<List<ConfirmUploadResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
