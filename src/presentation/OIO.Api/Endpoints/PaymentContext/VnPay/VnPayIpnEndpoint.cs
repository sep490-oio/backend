using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.PaymentContext.Commands.SaveVnPayWebhook;
using OIO.Infrastructure.Payment.VnPay;

namespace OIO.Api.Endpoints.PaymentContext.VnPay;

/// <summary>
/// VNPay IPN callback endpoint (server-to-server).
/// VNPay gọi endpoint này để thông báo kết quả thanh toán.
/// KHÔNG yêu cầu authentication — VNPay gọi trực tiếp.
/// </summary>
public sealed class VnPayIpnEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.VnPay.Ipn, async (
                ISender sender,
                HttpContext httpContext,
                CancellationToken ct) =>
            {
                // Parse query string từ VNPay
                var queryParams = VnPayHelper.ParseQueryString(httpContext.Request.QueryString.Value ?? "");

                var command = new SaveVnPayWebhookCommand(queryParams);
                var result = await sender.Send(command, ct);

                if (result.IsFailure)
                {
                    // VNPay yêu cầu trả về JSON với RspCode
                    return Results.Json(new { RspCode = "97", Message = result.Error.Message });
                }

                // Trả về 00 ngay lập tức cho VNPay, việc xử lý ví/transaction sẽ làm trong background
                return Results.Json(new
                {
                    RspCode = "00",
                    Message = "Confirm Success",
                });
            })
            .WithName(ApiEndpoint.Names.VnPay.Ipn)
            .WithTags(ApiEndpoint.Tags.Payments)
            .AllowAnonymous() // VNPay IPN — no auth required
            .Produces(StatusCodes.Status200OK);
    }
}
