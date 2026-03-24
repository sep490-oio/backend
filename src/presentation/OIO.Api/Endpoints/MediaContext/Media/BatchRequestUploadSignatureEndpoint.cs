using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.MediaContext.Commands.BatchRequestUploadSignature;
using OIO.Application.Context.MediaContext.Commands.RequestUploadSignature;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.MediaContext.Media;

public sealed class BatchRequestUploadSignatureEndpoint : IEndpoint
{
    public sealed record RequestItem(
        [Required] string Context,
        [Required] string FileName);

    public sealed record Request(
        [Required] List<RequestItem> Items);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Media.BatchRequestSignature, async (
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new BatchRequestUploadSignatureCommand(
                    request.Items.Select(i => new BatchUploadSignatureItem(
                        i.Context,
                        i.FileName)).ToList());

                var result = await sender.Send(command, ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Media.Upload)
            .WithName(ApiEndpoint.Names.Media.BatchRequestUploadSignature)
            .WithTags(ApiEndpoint.Tags.Media)
            .Produces<List<UploadSignatureResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
