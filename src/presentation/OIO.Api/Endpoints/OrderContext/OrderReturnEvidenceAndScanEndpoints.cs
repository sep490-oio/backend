using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.OrderContext.Commands.AddOrderReturnEvidence;
using OIO.Application.Context.OrderContext.Commands.RetryDeferredRefund;
using OIO.Application.Context.OrderContext.Commands.ScanOrderReturn;
using OIO.Application.Context.OrderContext.DTOs;

namespace OIO.Api.Endpoints.OrderContext;

/// <summary>
/// POST /api/me/orders/{orderId}/returns/{returnId}/evidence — buyer uploads a
/// pickup photo. Auth is buyer-scoped — handler asserts caller == order.BuyerId
/// AND asserts <see cref="Application.Context.OrderContext.Commands.AddOrderReturnEvidence.AddOrderReturnEvidenceCommand.Category"/>
/// is <c>pickup_by_buyer</c> when the buyer calls (enforced in the handler's
/// category switch).
/// </summary>
public sealed class AddBuyerOrderReturnEvidenceEndpoint : IEndpoint
{
    public sealed record Request(
        [Required] Guid MediaUploadId,
        [Required] string Category);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Orders.AddBuyerReturnEvidence, async (
                Guid orderId,
                Guid returnId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new AddOrderReturnEvidenceCommand(orderId, returnId, request.MediaUploadId, request.Category),
                    ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Orders.AddBuyerOrderReturnEvidence)
            .WithTags(ApiEndpoint.Tags.Orders)
            .Produces<OrderReturnEvidenceDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}

/// <summary>
/// POST /api/seller/orders/{orderId}/returns/{returnId}/evidence — seller
/// uploads a receipt photo. Auth is seller-scoped — handler asserts caller
/// == order.SellerId AND asserts category is <c>receipt_by_seller</c>.
/// </summary>
public sealed class AddSellerOrderReturnEvidenceEndpoint : IEndpoint
{
    public sealed record Request(
        [Required] Guid MediaUploadId,
        [Required] string Category);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Orders.AddSellerReturnEvidence, async (
                Guid orderId,
                Guid returnId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new AddOrderReturnEvidenceCommand(orderId, returnId, request.MediaUploadId, request.Category),
                    ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Orders.AddSellerOrderReturnEvidence)
            .WithTags(ApiEndpoint.Tags.Orders)
            .Produces<OrderReturnEvidenceDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}

/// <summary>
/// POST /api/seller/orders/{orderId}/returns/{returnId}/scan — seller scans
/// the buyer-issued QR on parcel arrival. Flips the return to
/// <c>SellerReceived</c> (D4 — must be <c>ReturnInTransit</c> to scan).
/// </summary>
public sealed class ScanOrderReturnEndpoint : IEndpoint
{
    public sealed record Request([Required] string QrToken);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Orders.ScanReturn, async (
                Guid orderId,
                Guid returnId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new ScanOrderReturnCommand(orderId, returnId, request.QrToken),
                    ct);
                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Orders.ScanOrderReturn)
            .WithTags(ApiEndpoint.Tags.Orders)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}

/// <summary>
/// POST /api/admin/orders/{orderId}/returns/{returnId}/retry-refund — admin
/// retries a deferred refund that previously failed (D1 compensating path).
/// </summary>
public sealed class RetryDeferredRefundEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Orders.RetryDeferredRefund, async (
                Guid orderId,
                Guid returnId,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new RetryDeferredRefundCommand(orderId, returnId), ct);
                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Orders.RetryDeferredRefund)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
