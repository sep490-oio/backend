using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Api.Filters;
using OIO.Application.Context.MediaContext.Commands.RequestUploadSignature;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.MediaContext.Media;

public sealed class RequestUploadSignatureEndpoint : IEndpoint
{
    public sealed record Request(
        [Required] string Context,
        [Required] string FileName);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Media.RequestSignature, async (
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new RequestUploadSignatureCommand(
                    request.Context,
                    request.FileName);

                return await sender.Send(command, ct);
            })
            .AddEndpointFilter(new IdempotencyFilter<UploadSignatureResponse>(
                IdempotencyHttpPolicies.RequestUploadSignature()))
            .RequireAuthorization(App.Permissions.Catalogs.Media.Upload)
            .WithName(ApiEndpoint.Names.Media.RequestUploadSignature)
            .WithTags(ApiEndpoint.Tags.Media)
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
