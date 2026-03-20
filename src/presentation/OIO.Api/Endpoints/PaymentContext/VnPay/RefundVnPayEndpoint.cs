using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.Context.PaymentContext.Commands.RefundVnPayTransaction;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.PaymentContext.VnPay;

/// <summary>
/// Hoàn tiền VNPay. Chỉ Admin mới có quyền.
/// </summary>
public sealed class RefundVnPayEndpoint : IEndpoint
{
    public sealed record Request(
        [Required] string OriginalTransactionRef,
        [Required] string OriginalVnPayTransactionNo,
        [Required] decimal Amount,
        [Required] string Reason);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.VnPay.Refund, async (
                Request request,
                ISender sender,
                HttpContext httpContext,
                CancellationToken ct) =>
            {
                var command = new RefundVnPayTransactionCommand(
                    OriginalTransactionRef: request.OriginalTransactionRef,
                    OriginalVnPayTransactionNo: request.OriginalVnPayTransactionNo,
                    Amount: request.Amount,
                    Reason: request.Reason,
                    IpAddress: httpContext.GetIpAddress());

                var result = await sender.Send(command, ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManagePayments)
            .WithName(ApiEndpoint.Names.VnPay.Refund)
            .WithTags(ApiEndpoint.Tags.Payments)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
