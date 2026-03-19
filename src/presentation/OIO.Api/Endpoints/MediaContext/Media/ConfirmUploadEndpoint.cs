using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Api.Filters;
using OIO.Application.Context.MediaContext.Commands.ConfirmUpload;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.MediaContext.Media;

public sealed class ConfirmUploadEndpoint : IEndpoint
{
    public sealed record Request(
        [Required] Guid MediaUploadId,
        [Required] string PublicId,
        [Required] string SecureUrl,
        [Required] long Bytes,
        [Required] string Format,
        string? FileName,
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
                    request.FileName,
                    request.Width,
                    request.Height,
                    request.DurationSeconds);
                return await sender.Send(command, ct);
            })
            .AddEndpointFilter(new IdempotencyFilter<ConfirmUploadResponse>(
                IdempotencyHttpPolicies.ConfirmUpload()))
            .RequireAuthorization(App.Permissions.Catalogs.Media.ConfirmUpload)
            .WithName(ApiEndpoint.Names.Media.ConfirmUpload)
            .WithTags(ApiEndpoint.Tags.Media)
            .Produces(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
