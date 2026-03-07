using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.MediaContext.Commands.RequestUploadSignature;

namespace OIO.Api.Endpoints.MediaContext.Media;

public sealed class RequestUploadSignatureEndpoint : IEndpoint
{
    public sealed record Request(
        string Context,
        string FileName,
        Guid? EntityId = null);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Media.RequestSignature, async (
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new RequestUploadSignatureCommand(
                    request.Context,
                    request.EntityId,
                    request.FileName);

                var result = await sender.Send(command, ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Media.RequestUploadSignature)
            .WithTags(ApiEndpoint.Tags.Media)
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem();
    }
}