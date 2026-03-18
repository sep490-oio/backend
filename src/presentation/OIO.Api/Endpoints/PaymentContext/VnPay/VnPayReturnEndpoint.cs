using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.PaymentContext.Commands.ProcessVnPayCallback;
using OIO.Infrastructure.Payment.VnPay;

namespace OIO.Api.Endpoints.PaymentContext.VnPay;

/// <summary>
/// VNPay Return URL endpoint.
/// VNPay redirect user về endpoint này sau khi thanh toán.
/// Frontend có thể dùng kết quả để hiển thị trạng thái thanh toán.
/// </summary>
public sealed class VnPayReturnEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.VnPay.Return, async (
                ISender sender,
                HttpContext httpContext,
                CancellationToken ct) =>
            {
                var queryParams = VnPayHelper.ParseQueryString(httpContext.Request.QueryString.Value ?? "");

                var command = new ProcessVnPayCallbackCommand(queryParams);
                var result = await sender.Send(command, ct);

                if (result.IsFailure)
                {
                    return Results.BadRequest(new { success = false, message = result.Error.Message });
                }

                var response = result.Value;

                // Trả về kết quả cho frontend hiển thị
                return Results.Ok(new
                {
                    success = response.IsSuccess,
                    transactionRef = response.TransactionRef,
                    responseCode = response.ResponseCode,
                    message = response.Message,
                });
            })
            .WithName(ApiEndpoint.Names.VnPay.Return)
            .WithTags(ApiEndpoint.Tags.Payments)
            .AllowAnonymous() // Return URL — user đã được redirect từ VNPay
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
