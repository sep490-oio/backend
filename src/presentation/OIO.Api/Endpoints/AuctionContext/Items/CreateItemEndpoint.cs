using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Commands.CreateItem;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Items;

public sealed class CreateItemEndpoint : IEndpoint
{
    public sealed record MediaAttachmentRequest(
        Guid MediaUploadId,
        string PublicId,
        bool IsPrimary = false,
        int SortOrder = 0);

    public sealed record Request(
        string Title,
        string Condition,
        Guid? CategoryId = null,
        string? Description = null,
        int Quantity = 1,
        string? Attributes = null,
        List<MediaAttachmentRequest>? Images = null);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Items.Create, async (
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var images = request.Images?
                    .Select(i => new MediaAttachment(
                        i.MediaUploadId, i.IsPrimary, i.SortOrder))
                    .ToList();

                var command = new CreateItemCommand(
                    request.Title,
                    request.Condition,
                    request.CategoryId,
                    request.Description,
                    request.Quantity,
                    request.Attributes,
                    images);

                var result = await sender.Send(command, ct);

                return result.ToCreatedHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Items.CreateItem)
            .WithTags(ApiEndpoint.Tags.Items)
            .Produces(StatusCodes.Status201Created)
            .ProducesValidationProblem();
    }
}