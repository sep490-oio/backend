using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Commands.SuggestItemDescription;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Items;

public sealed class SuggestItemDescriptionEndpoint : IEndpoint
{
    public sealed record Request(
        [Required] string Title,
        [Required] string Condition,
        [Required] List<Guid> ImageMediaUploadIds,
        string? Locale = "vi");

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Items.SuggestDescription, async (
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new SuggestItemDescriptionCommand(
                    request.Title,
                    request.Condition,
                    request.ImageMediaUploadIds,
                    request.Locale);

                var result = await sender.Send(command, ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Items.Create)
            .RequireRateLimiting("AiSuggestPerSeller")
            .WithName(ApiEndpoint.Names.Items.SuggestItemDescription)
            .WithTags(ApiEndpoint.Tags.Items)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }
}
