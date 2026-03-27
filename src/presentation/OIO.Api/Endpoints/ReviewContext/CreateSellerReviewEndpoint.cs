using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.ReviewContext.Commands.CreateSellerReview;
using OIO.Application.Context.ReviewContext.DTOs;

namespace OIO.Api.Endpoints.ReviewContext;

public sealed class CreateSellerReviewEndpoint : IEndpoint
{
    public sealed record Request(
        [Required] Guid OrderId,
        [Required] int OverallRating,
        int? CommunicationRating = null,
        int? ShippingSpeedRating = null,
        int? ItemAccuracyRating = null,
        string? Title = null,
        string? Comment = null);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Reviews.Create, async (
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new CreateSellerReviewCommand(
                    request.OrderId,
                    request.OverallRating,
                    request.CommunicationRating,
                    request.ShippingSpeedRating,
                    request.ItemAccuracyRating,
                    request.Title,
                    request.Comment);

                var result = await sender.Send(command, ct);

                return result.ToCreatedHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Reviews.CreateSellerReview)
            .WithTags(ApiEndpoint.Tags.Reviews)
            .Produces<SellerReviewDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
